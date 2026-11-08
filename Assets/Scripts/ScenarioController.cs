using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenarioController : MonoBehaviour
{
    [SerializeField] private DayDefinition dayDefinition;
    [SerializeField] private DayObjectivesHUD objectivesHUD;
    public DayDefinition DayDefinition => dayDefinition;
    public DayProgress Progress { get; private set; }
    public bool IsInterviewPhase => interviewPhaseStarted && !InterviewsCompleted;
    public event System.Action StateChanged;
    [SerializeField] private DayBriefingUI briefingUI;
    [SerializeField] private DayExitDoor exitDoor;
    private CompanyMetrics companyAtStart;
    private bool leavingDay;
    public bool HasStartedDay { get; private set; }
    public bool IsBriefingOpen => briefingUI != null && briefingUI.IsOpen;
    public bool CanLeaveDay => HasStartedDay && InterviewsCompleted && !EndsCampaign && !leavingDay && !IsBriefingOpen;
    public DayReport CompletedReport { get; private set; }
    public bool EndsCampaign => CompletedReport != null && CompletedReport.EndsCampaign;

    [SerializeField]
    private CVPile cvPile;
    [SerializeField] private CVReview cvReview;
    [SerializeField] private MotivationLettersReview letterReview;
    [SerializeField] private AcceptedCandidates acceptedCandidates;
    [SerializeField] private Transform candidateParent;
    [SerializeField] private GameObject[] reviewObjects = new GameObject[0];
    [SerializeField, Min(0f)] private float fadeDuration = 0.6f;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerRayCast playerRayCast;
    [SerializeField] private Transform playerHead;
    [SerializeField] private Vector3 interviewHeadEulerAngles = new Vector3(0.600457788f, 0f, 0f);
    [SerializeField] private Vector3 interviewBodyEulerAngles = new Vector3(0f, 180f, 0f);
    [SerializeField] private Chair chair;
    [SerializeField] private UnityEngine.Events.UnityEvent onInterviewsCompleted = new UnityEngine.Events.UnityEvent();

    private CanvasGroup fadeOverlay;
    private bool interviewPhaseStarted;
    private InterviewUI interviewUI;
    private bool ownsPlayerFocus;
    private bool previousRayCastEnabled;
    private InterviewSession activeSession;
    private int interviewIndex;
    private bool autoSaveRequested;
    private bool fading;
    private Trash saveTrash;
    public bool CanSave => Progress != null && !GameSession.IsChangingScene && !fading && (!leavingDay || IsBriefingOpen) &&
        (IsBriefingOpen || (HasStartedDay && (!IsInterviewPhase ||
        (activeSession != null && !activeSession.IsComplete && interviewUI != null && interviewUI.gameObject.activeSelf))));
    private Quaternion originalHeadRotation;
    private bool hasSavedHeadRotation;
    [SerializeField] private Transform playerBody;
    private Quaternion originalBodyRotation;
    private bool hasSavedBodyRotation;
    private readonly Dictionary<Transform, int> chairLayers = new Dictionary<Transform, int>();
    public bool InterviewsCompleted { get; private set; }
    public Candidate CurrentInterviewCandidate { get; private set; }
    public GameObject CurrentInterviewInstance { get; private set; }

    private IEnumerator Start()
    {
        if (dayDefinition == null || cvPile == null || cvReview == null || letterReview == null || acceptedCandidates == null)
        {
            Debug.LogError("Assigne un SO de journée, la pile, les deux reviews et les candidats acceptés au scénario.", this);
            enabled = false;
            yield break;
        }
        var restored = GameSession.Instance.TakeSnapshot(dayDefinition);
        var catalog = GameSession.Instance.Catalog;
        var startingEmployees = restored == null ? new List<Candidate>(InterviewResults.Instance.RetainedCandidates)
            : catalog.Resolve(restored.employeesAtStart);
        companyAtStart = new CompanyMetrics(startingEmployees);
        Progress = new DayProgress(dayDefinition, startingEmployees);
        acceptedCandidates.ConfigureCapacity(dayDefinition.interviewCapacity);
        cvPile.ReceiveCVs(restored == null ? dayDefinition.candidates ?? new List<Candidate>() : catalog.Resolve(restored.remaining));
        if (restored != null)
        {
            acceptedCandidates.RestoreSelection(catalog.Resolve(restored.selected));
            foreach (var decision in restored.decisions) Progress.Record(catalog.Candidate(decision.candidateId), decision.retained);
        }
        if (objectivesHUD == null)
            objectivesHUD = GetComponentInChildren<DayObjectivesHUD>(true);
        if (objectivesHUD != null)
            objectivesHUD.Bind(this, acceptedCandidates, InterviewResults.Instance);
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        if (playerRayCast == null)
            playerRayCast = FindAnyObjectByType<PlayerRayCast>();
        if (playerHead == null && playerController != null)
            playerHead = playerController.transform.Find("Head");
        if (chair == null)
            chair = GetComponentInChildren<Chair>(true);
        if (playerBody == null && playerController != null)
            playerBody = playerController.transform;
        if (exitDoor == null)
            exitDoor = GetComponentInChildren<DayExitDoor>(true);
        if (exitDoor != null) exitDoor.Configure(this);
        if (briefingUI == null) briefingUI = GetComponentInChildren<DayBriefingUI>(true);
        if (briefingUI == null) briefingUI = DayBriefingUI.CreateView(transform);

        acceptedCandidates.Changed += RequestAutoSave;
        cvPile.Changed += RequestAutoSave;
        Progress.Changed += RequestAutoSave;
        saveTrash = GetComponentInChildren<Trash>(true);
        if (saveTrash != null) saveTrash.Changed += RequestAutoSave;
        if (restored != null)
        {
            playerController.RestorePose(restored.playerPosition, restored.playerRotation, restored.headRotation, restored.seated);
            if (chair != null) chair.RestoreSeatedState(restored.seated);
            cvReview.RestoreComputerPage(catalog.Document(restored.computerDocument));
            if (restored.phase != SavedDayPhase.Briefing)
            {
                yield return RestoreDay(restored, catalog);
                yield break;
            }
        }
        AcquirePlayerFocus();
        briefingUI.Show(DayObjectiveText.Briefing(dayDefinition, InterviewResults.Instance.LastReport),
            playerRayCast == null ? null : playerRayCast.InteractionAction);
        GameSession.Instance.NotifyReady();
        if (restored == null) RequestAutoSave();
        while (briefingUI.IsOpen) yield return null;
        // Consume the dismissal frame before allowing world interactions again.
        yield return null;
        ReleasePlayerFocus();
        HasStartedDay = true;
        StateChanged?.Invoke();
        RequestAutoSave();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameSession.InputBlocked || !HasStartedDay || interviewPhaseStarted)
            return;

        bool reviewsEmpty = cvReview.CurrentCandidate == null && letterReview.CurrentCandidate == null;
        if (acceptedCandidates.IsFull || (cvPile.IsEmpty && reviewsEmpty))
        {
            interviewPhaseStarted = true;
            StateChanged?.Invoke();
            StartCoroutine(BeginInterviews());
        }
    }

    private IEnumerator BeginInterviews(int startIndex = 0, DaySnapshot restored = null)
    {
        var interviewCandidates = new List<Candidate>(acceptedCandidates.Candidates);
        DisableReviewInteractions();

        AcquirePlayerFocus();

        CreateFadeOverlay();
        yield return FadeTo(1f);

        cvReview.Clear();
        letterReview.Clear();
        if (playerHead != null && interviewCandidates.Count > 0)
        {
            originalHeadRotation = restored == null ? playerHead.localRotation : restored.originalHead;
            hasSavedHeadRotation = restored == null || restored.savedHead;
            if (playerBody != null)
            {
                originalBodyRotation = restored == null ? playerBody.localRotation : restored.originalBody;
                hasSavedBodyRotation = restored == null || restored.savedBody;
                playerBody.localEulerAngles = interviewBodyEulerAngles;
            }
            playerHead.localEulerAngles = interviewHeadEulerAngles;
        }

        if (candidateParent == null)
            candidateParent = transform.Find("Candidate");

        var uiObject = new GameObject("Interview Dialogue", typeof(RectTransform));
        uiObject.transform.SetParent(transform, false);
        interviewUI = uiObject.AddComponent<InterviewUI>();
        interviewUI.Initialize();

        for (interviewIndex = startIndex; interviewIndex < interviewCandidates.Count; interviewIndex++)
        {
            var candidate = interviewCandidates[interviewIndex];
            if (candidate == null)
                continue;
            CurrentInterviewCandidate = candidate;

            if (candidateParent == null || candidate.mesh == null)
                Debug.LogError("Impossible d'afficher l'entretien : assigne l'empty Candidate et le prefab mesh du candidat.", this);
            else
                CurrentInterviewInstance = Instantiate(candidate.mesh, candidateParent.position,
                    candidateParent.rotation, candidateParent);

            if (candidate.interviewDialogue == null)
                Debug.LogWarning($"Aucun dialogue d'entretien assigné à {candidate.name} : passage au choix final.", candidate);

            var session = restored != null && interviewIndex == startIndex
                ? InterviewSession.Restore(candidate.interviewDialogue, restored.dialogueStep, restored.question)
                : new InterviewSession(candidate.interviewDialogue);
            activeSession = session;
            session.Changed += RequestAutoSave;
            yield return FadeTo(0f);
            interviewUI.Show(candidate, session);
            GameSession.Instance.NotifyReady();
            RequestAutoSave();
            yield return new WaitUntil(() => session.IsComplete);
            session.Changed -= RequestAutoSave;

            InterviewResults.Instance.Record(candidate, session.Retained);
            Progress.Record(candidate, session.Retained);
            interviewUI.Hide();
            yield return FadeTo(1f);
            ClearInterviewInstance();
            // Destroy is deferred; remove the previous model before the next fade-in.
            yield return null;
        }

        if (interviewCandidates.Count == 0)
            Debug.Log("Aucun candidat accepté pour les entretiens.", this);

        RestoreHeadAndChair();
        yield return FadeTo(0f);
        Destroy(fadeOverlay.gameObject);
        fadeOverlay = null;
        Destroy(interviewUI.gameObject);
        interviewUI = null;
        ReleasePlayerFocus();
        InterviewsCompleted = true;
        CompletedReport = DayReport.Capture(dayDefinition, Progress, cvPile.RemainingCount,
            companyAtStart, InterviewResults.Instance.Metrics, InterviewResults.Instance.DayReports);
        InterviewResults.Instance.StoreReport(CompletedReport);
        if (exitDoor != null) exitDoor.SetAvailable(!EndsCampaign);
        StateChanged?.Invoke();
        GameSession.Instance.NotifyReady();
        RequestAutoSave();
        onInterviewsCompleted.Invoke();
        if (EndsCampaign)
        {
            GameSession.Instance.SaveAutomatic(this);
            yield return ShowCampaignEnding();
        }
    }

    private void DisableReviewInteractions()
    {
        if (chair != null)
            foreach (var child in chair.GetComponentsInChildren<Transform>(true))
                chairLayers[child] = child.gameObject.layer;
        // Disable hover and interaction immediately, including collider children.
        foreach (var reviewObject in reviewObjects)
        {
            if (reviewObject == null)
                continue;
            foreach (var interactible in reviewObject.GetComponentsInChildren<IInteractible>(true))
            {
                interactible.OnStopLook();
                interactible.enabled = false;
            }
            foreach (var child in reviewObject.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 0;
                var outline = child.GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = false;
            }
        }

    }

    private IEnumerator RestoreDay(DaySnapshot state, GameContentCatalog catalog)
    {
        HasStartedDay = true;
        if (state.phase == SavedDayPhase.Interview)
        {
            interviewPhaseStarted = true;
            StateChanged?.Invoke();
            yield return BeginInterviews(state.interviewIndex, state);
            yield break;
        }
        if (state.phase == SavedDayPhase.Completed)
        {
            interviewPhaseStarted = true;
            InterviewsCompleted = true;
            CompletedReport = state.completedReport;
            DisableReviewInteractions();
            RestoreHeadAndChair();
            if (exitDoor != null) exitDoor.SetAvailable(!EndsCampaign);
        }
        else
        {
            var current = catalog.Candidate(state.currentReview);
            if (current != null)
            {
                cvReview.ReceiveCV(ref current);
                letterReview.ReceiveMotivationLetter(ref current);
            }
            yield return null;
            if (state.focus == SavedFocus.CV) playerRayCast.RestoreFocus(cvReview);
            else if (state.focus == SavedFocus.Letter) playerRayCast.RestoreFocus(letterReview);
            else if (state.focus == SavedFocus.Computer) playerRayCast.RestoreFocus(GetComponentInChildren<PersonalComputer>(true));
            yield return null;
            cvReview.ScrollOffset = state.cvScroll;
            letterReview.ScrollOffset = state.letterScroll;
        }
        GameSession.Instance.NotifyReady();
        StateChanged?.Invoke();
        if (EndsCampaign) yield return ShowCampaignEnding();
    }

    private IEnumerator ShowCampaignEnding()
    {
        AcquirePlayerFocus();
        CreateFadeOverlay();
        yield return FadeTo(1f);
        GameSession.Instance.Menus.ShowEnding(CompletedReport);
        yield return FadeTo(0f);
        Destroy(fadeOverlay.gameObject);
        fadeOverlay = null;
    }

    public SaveGameData CaptureSave()
    {
        if (!CanSave) throw new System.InvalidOperationException("Attendez la fin de la transition avant de sauvegarder.");
        var catalog = GameSession.Instance.Catalog;
        var state = new DaySnapshot
        {
            phase = InterviewsCompleted ? SavedDayPhase.Completed : !HasStartedDay ? SavedDayPhase.Briefing
                : IsInterviewPhase ? SavedDayPhase.Interview : SavedDayPhase.Review,
            remaining = catalog.Identify(cvPile.RemainingCandidates), selected = catalog.Identify(acceptedCandidates.Candidates),
            currentReview = catalog.Id(cvReview.CurrentCandidate ?? letterReview.CurrentCandidate),
            employeesAtStart = catalog.Identify(Progress.EmployeesAtStart), interviewIndex = interviewIndex,
            dialogueStep = activeSession == null ? 0 : activeSession.StepIndex,
            question = activeSession == null ? -1 : activeSession.SelectedQuestion,
            playerPosition = playerController.transform.position, playerRotation = playerController.transform.rotation,
            headRotation = playerHead == null ? Quaternion.identity : playerHead.localRotation,
            seated = playerController.IsSeated, focus = playerRayCast.CurrentFocus,
            cvScroll = cvReview.ScrollOffset, letterScroll = letterReview.ScrollOffset,
            computerDocument = catalog.Id(cvReview.CurrentComputerPage),
            originalHead = hasSavedHeadRotation ? originalHeadRotation : Quaternion.identity,
            originalBody = hasSavedBodyRotation ? originalBodyRotation : Quaternion.identity,
            savedHead = hasSavedHeadRotation, savedBody = hasSavedBodyRotation, completedReport = CompletedReport
        };
        if (state.phase != SavedDayPhase.Review) state.focus = SavedFocus.None;
        foreach (var decision in Progress.Decisions)
            state.decisions.Add(new SavedDecision { candidateId = catalog.Id(decision.Key), retained = decision.Value });
        return new SaveGameData
        {
            dayId = catalog.Id(dayDefinition), dayTitle = dayDefinition.title,
            scenePath = SceneManager.GetActiveScene().path, day = state,
            campaign = new CampaignSnapshot
            {
                retained = catalog.Identify(InterviewResults.Instance.RetainedCandidates),
                rejected = catalog.Identify(InterviewResults.Instance.RejectedCandidates),
                reports = new List<DayReport>(InterviewResults.Instance.DayReports)
            }
        };
    }

    private void RequestAutoSave() => autoSaveRequested = true;
    private void LateUpdate()
    {
        if (!autoSaveRequested || !CanSave || GameSession.InputBlocked) return;
        autoSaveRequested = false;
        GameSession.Instance.SaveAutomatic(this);
    }

    private void ClearInterviewInstance()
    {
        if (CurrentInterviewInstance != null)
        {
            CurrentInterviewInstance.SetActive(false);
            Destroy(CurrentInterviewInstance);
        }
        CurrentInterviewInstance = null;
        CurrentInterviewCandidate = null;
    }

    private void ReleasePlayerFocus()
    {
        if (!ownsPlayerFocus)
            return;
        if (playerController != null)
            playerController.SetUIFocusMode(false);
        if (playerRayCast != null)
            playerRayCast.enabled = previousRayCastEnabled;
        ownsPlayerFocus = false;
    }

    private void AcquirePlayerFocus()
    {
        if (ownsPlayerFocus) return;
        if (playerRayCast != null)
        {
            previousRayCastEnabled = playerRayCast.enabled;
            playerRayCast.enabled = false;
        }
        if (playerController != null) playerController.SetUIFocusMode(true);
        ownsPlayerFocus = true;
    }

    public bool TryLeaveDay()
    {
        if (!CanLeaveDay) return false;
        if (!string.IsNullOrWhiteSpace(dayDefinition.nextScenePath) &&
            !Application.CanStreamedLevelBeLoaded(dayDefinition.nextScenePath))
        {
            Debug.LogError("La scène suivante n'est pas disponible dans les scènes du build : " + dayDefinition.nextScenePath, this);
            return false;
        }
        GameSession.Instance.SaveAutomatic(this);
        leavingDay = true;
        if (exitDoor != null) exitDoor.SetAvailable(false);
        StartCoroutine(string.IsNullOrWhiteSpace(dayDefinition.nextScenePath) ? ShowLastReport() : LoadNextDay());
        return true;
    }

    private IEnumerator LoadNextDay()
    {
        AcquirePlayerFocus();
        CreateFadeOverlay();
        yield return FadeTo(1f);
        var loading = SceneManager.LoadSceneAsync(dayDefinition.nextScenePath);
        if (loading != null)
        {
            while (!loading.isDone) yield return null;
            yield break;
        }
        yield return FadeTo(0f);
        Destroy(fadeOverlay.gameObject);
        fadeOverlay = null;
        ReleasePlayerFocus();
        leavingDay = false;
        if (exitDoor != null) exitDoor.SetAvailable(true);
    }

    private IEnumerator ShowLastReport()
    {
        AcquirePlayerFocus();
        briefingUI.Show(CompletedReport.ToDisplayText() + "\nFIN DU PROTOTYPE\nAucune journée suivante n'est encore configurée.",
            playerRayCast == null ? null : playerRayCast.InteractionAction, true);
        while (briefingUI.IsOpen) yield return null;
        yield return null;
        ReleasePlayerFocus();
        leavingDay = false;
        if (exitDoor != null) exitDoor.SetAvailable(true);
    }

    private void RestoreHeadAndChair()
    {
        if (hasSavedBodyRotation)
        {
            if (playerBody != null)
                playerBody.localRotation = originalBodyRotation;
            hasSavedBodyRotation = false;
        }
        if (hasSavedHeadRotation)
        {
            if (playerHead != null)
                playerHead.localRotation = originalHeadRotation;
            hasSavedHeadRotation = false;
        }
        if (playerController != null) playerController.SynchronizeLookRotation();
        if (chairLayers.Count == 0)
            return;
        foreach (var entry in chairLayers)
            if (entry.Key != null)
                entry.Key.gameObject.layer = entry.Value;
        chairLayers.Clear();
        if (chair != null)
            chair.enabled = true;
    }

    private void CreateFadeOverlay()
    {
        var overlay = new GameObject("Interview Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        fadeOverlay = overlay.GetComponent<CanvasGroup>();
        fadeOverlay.alpha = 0f;
        var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(overlay.transform, false);
        image.GetComponent<Image>().color = Color.black;
        image.GetComponent<Image>().raycastTarget = false;
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeTo(float alpha)
    {
        fading = true;
        try
        {
            float initialAlpha = fadeOverlay.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(initialAlpha, alpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            fadeOverlay.alpha = alpha;
        }
        finally { fading = false; }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (activeSession != null) activeSession.Changed -= RequestAutoSave;
        if (acceptedCandidates != null) acceptedCandidates.Changed -= RequestAutoSave;
        if (cvPile != null) cvPile.Changed -= RequestAutoSave;
        if (Progress != null) Progress.Changed -= RequestAutoSave;
        if (saveTrash != null) saveTrash.Changed -= RequestAutoSave;
        RestoreHeadAndChair();
        ReleasePlayerFocus();
        ClearInterviewInstance();
        if (interviewUI != null)
        {
            Destroy(interviewUI.gameObject);
            interviewUI = null;
        }
        if (fadeOverlay != null)
        {
            Destroy(fadeOverlay.gameObject);
            fadeOverlay = null;
        }
    }
}
