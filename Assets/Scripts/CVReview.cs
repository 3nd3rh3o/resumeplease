using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CVReview : IInteractible
{
    private static readonly int HasCVHash = Animator.StringToHash("HasCV");
    [SerializeField] private PanelRenderer cvPanelRenderer;
    [SerializeField] private float focusDistance = 0.8f;
    [SerializeField] private float focusedFontSize = 1f;

    [SerializeField] private PanelRenderer pcPanelRenderer;

    private ScrollView cvScrollView;
    private Label documentTitleLabel;
    private readonly List<TaggedDocumentText> fields = new List<TaggedDocumentText>();
    private VisualElement cvRoot;
    private TaggedDocumentText hoveredField;
    private int hoveredLink = -1;
    private Candidate current;
    private bool focus = false;
    private PanelRenderer.VersionedUIReloadCallback uiReloadCallback;
    private Camera focusCamera;
    [SerializeField]
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private static readonly Quaternion FocusRotationOffset = Quaternion.identity;
    private bool hasSavedPanelTransform;

    public bool IsFocused => focus;
    public Candidate CurrentCandidate => current;
    public Vector2 ScrollOffset { get => cvScrollView == null ? Vector2.zero : cvScrollView.scrollOffset; set { if (cvScrollView != null) cvScrollView.scrollOffset = value; } }
    public VisualTreeAsset CurrentComputerPage => pcPanelRenderer == null ? null : pcPanelRenderer.visualTreeAsset;
    public void RestoreComputerPage(VisualTreeAsset page) { if (page != null && pcPanelRenderer != null) pcPanelRenderer.visualTreeAsset = page; }

    public void Clear()
    {
        if (focus)
            RestorePanel();
        current = null;
        UpdateCVLabels();
        if (cvScrollView != null)
            cvScrollView.scrollOffset = Vector2.zero;
        var animator = GetComponentInParent<Animator>();
        if (animator != null)
            animator.SetBool("HasCV", false);
    }

    private void Awake()
    {
        requireSeated = true;

        uiReloadCallback = OnUIReloaded;
        if (cvPanelRenderer != null)
            cvPanelRenderer.RegisterUIReloadCallback(uiReloadCallback);
    }

    private void OnDestroy()
    {
        if (cvPanelRenderer != null && uiReloadCallback != null)
            cvPanelRenderer.UnregisterUIReloadCallback(uiReloadCallback);
    }

    private void OnUIReloaded(PanelRenderer renderer, VisualElement root, int version)
    {
        cvRoot = root;
        cvScrollView = root.Q<ScrollView>("cv-scroll");
        documentTitleLabel = root.Q<Label>("document-title");
        if (cvScrollView == null)
            Debug.LogError("ScrollView UI Toolkit introuvable : cv-scroll", this);

        if (documentTitleLabel == null)
            Debug.LogError("Label UI Toolkit introuvable : document-title", this);

        UpdateCVLabels();

        if (focus)
            SetCVFontSize(focusedFontSize);
    }

    private void Update()
    {
        var animator = GetComponentInParent<Animator>();
        if (animator != null)
            animator.SetBool(HasCVHash, current != null);
    }

    private void LateUpdate()
    {
        if (focus && cvPanelRenderer != null && focusCamera != null)
        {
            var panelTransform = cvPanelRenderer.transform;
            panelTransform.position = focusCamera.transform.position + focusCamera.transform.forward * focusDistance;
            panelTransform.rotation = focusCamera.transform.rotation * FocusRotationOffset;
        }

        UpdateProjectedPointer();
    }

    public bool HasCV()
    {
        return current?.m_CV != null;
    }

    public void ReceiveCV(ref Candidate cv)
    {
        current = cv;
        UpdateCVLabels();

        if (current == null || current.m_CV == null)
        {
            Debug.LogError("Le candidat reçu n'a pas de CV assigné.", this);
            return;
        }

        Debug.Log($"CV reçu : {current.m_CV.m_name} {current.m_CV.m_surname}", this);
    }

    private void UpdateCVLabels()
    {
        if (cvScrollView == null)
            return;

        cvScrollView.contentContainer.Clear();
        fields.Clear();
        hoveredField = null;
        hoveredLink = -1;
        if (documentTitleLabel != null)
            documentTitleLabel.text = "CURRICULUM VITAE";

        if (current == null)
            return;

        var contentStyle = cvScrollView.contentContainer.style;
        contentStyle.flexDirection = FlexDirection.Column;
        contentStyle.alignItems = Align.Stretch;
        contentStyle.paddingTop = 0;
        contentStyle.paddingBottom = 0;
        contentStyle.paddingLeft = 0;
        contentStyle.paddingRight = 0;

        if (current.m_CV == null)
            return;

        var cv = current.m_CV;
        AddCVField("Nom", $"{cv.m_surname} {cv.m_name}".Trim());
        AddCVField("Courriel", $"Courriel : {cv.m_email}");
        AddCVField("Téléphone", $"Téléphone : {cv.m_phone}");
        AddCVField("Adresse", $"Adresse : {cv.m_address}");
        AddCVSection("FORMATION", cv.m_education);
        AddCVSection("EXPÉRIENCE", cv.m_experience);
        AddCVSection("COMPÉTENCES", cv.m_skills);
        AddCVSection("CENTRES D'INTÉRÊT", cv.m_hobbies);
        AddCVSection("LANGUES", cv.m_languages);
        AddCVSection("RÉFÉRENCES", cv.m_references);
    }

    private void AddCVSection(string title, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            AddCVField(title, $"{title}\n{value.Trim()}");
    }

    private void AddCVField(string fieldName, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var field = new TaggedDocumentText(fieldName, text, focus ? focusedFontSize : 1f);
        cvScrollView.Add(field.Label);
        fields.Add(field);
    }

    private void UpdateProjectedPointer()
    {
        if (GameSession.InputBlocked) { SetHoveredField(null, -1); return; }
        var mouse = Mouse.current;
        var camera = focusCamera != null ? focusCamera : Camera.main;
        TaggedDocumentText field = null;
        int link = -1;
        if (mouse != null && camera != null && cvPanelRenderer != null &&
            cvRoot?.panel != null && cvScrollView != null && current != null)
        {
            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            // CVReview is on Cube; the UI belongs to the referenced sibling GameObject.
            // UI worldTransform is relative to that PanelRenderer's GameObject.
            var documentToWorld = cvPanelRenderer.transform.localToWorldMatrix;
            if (RayIntersectsElement(ray, documentToWorld, cvScrollView.contentViewport))
            {
                foreach (var candidate in fields)
                {
                    if (!TryProjectPointer(ray, documentToWorld, candidate.Label, out var point))
                        continue;
                    link = candidate.HitTest(point);
                    if (link < 0)
                        continue;
                    field = candidate;
                    break;
                }
            }
        }

        SetHoveredField(field, link);
        if (field != null && mouse.leftButton.wasPressedThisFrame)
            OpenFieldDocument(field, link);
    }

    private void OpenFieldDocument(TaggedDocumentText field, int link)
    {
        var documentId = field.DocumentId(link);

        var documents = current?.m_CV?.uIDocuments;
        if (documents == null || documentId < 0 || documentId >= documents.Count || documents[documentId] == null)
        {
            Debug.LogWarning($"Aucun document UI pour la balise {documentId} du champ {field.FieldName}.", this);
            return;
        }

        if (pcPanelRenderer == null)
        {
            Debug.LogWarning("Assigne le PanelRenderer du PC pour afficher les documents du CV.", this);
            return;
        }

        pcPanelRenderer.visualTreeAsset = documents[documentId];
    }

    private static bool RayIntersectsElement(Ray worldRay, Matrix4x4 documentToWorld, VisualElement element)
    {
        return TryProjectPointer(worldRay, documentToWorld, element, out _);
    }

    private static bool TryProjectPointer(Ray worldRay, Matrix4x4 documentToWorld, VisualElement element, out Vector2 localPoint)
    {
        localPoint = default;
        // Include the UI layout, pivot, pixel scale, and the actual sheet transform.
        var worldToElement = (documentToWorld * element.worldTransform).inverse;
        var origin = worldToElement.MultiplyPoint3x4(worldRay.origin);
        var direction = worldToElement.MultiplyVector(worldRay.direction);
        if (Mathf.Abs(direction.z) < 0.000001f)
            return false;

        var distance = -origin.z / direction.z;
        if (distance < 0f)
            return false;

        var point = origin + direction * distance;
        localPoint = new Vector2(point.x, point.y);
        return element.visible && element.ContainsPoint(localPoint);
    }

    private void SetHoveredField(TaggedDocumentText field, int link)
    {
        if (hoveredField == field && hoveredLink == link)
            return;
        hoveredField?.SetHoveredLink(-1);
        hoveredField = field;
        hoveredLink = link;
        hoveredField?.SetHoveredLink(link);
    }

    private void SetCVFontSize(float fontSize)
    {
        if (documentTitleLabel != null)
            documentTitleLabel.style.fontSize = fontSize;

        foreach (var field in fields)
            field.Label.style.fontSize = fontSize;
    }
    
    public override void OnInteract()
    {
        if (current?.m_CV != null)
        {
            if (focus)
                RestorePanel();
            else
                FocusPanel();
        }
        else
        {
            Debug.Log("No CV to review.");
        }
    }

    private void FocusPanel()
    {
        if (cvPanelRenderer == null)
        {
            Debug.LogError("Impossible de focaliser le CV : aucun PanelRenderer n'est assigné.", this);
            return;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Impossible de focaliser le CV : aucune caméra avec le tag MainCamera.", this);
            return;
        }

        var panelTransform = cvPanelRenderer.transform;
        focusCamera = camera;
        originalLocalPosition = panelTransform.localPosition;
        originalLocalRotation = panelTransform.localRotation;
        hasSavedPanelTransform = true;
        UpdateCVLabels();
        panelTransform.position = camera.transform.position + camera.transform.forward * focusDistance;
        panelTransform.rotation = camera.transform.rotation * FocusRotationOffset;

        SetCVFontSize(focusedFontSize);

        focus = true;
    }

    private void RestorePanel()
    {
        if (cvPanelRenderer == null || !hasSavedPanelTransform)
            return;

        var panelTransform = cvPanelRenderer.transform;
        panelTransform.localPosition = originalLocalPosition;
        panelTransform.localRotation = originalLocalRotation;

        SetCVFontSize(1f);

        focusCamera = null;
        hasSavedPanelTransform = false;
        focus = false;
        UpdateCVLabels();
    }

    private void OnDisable()
    {
        SetHoveredField(null, -1);
        if (focus)
            RestorePanel();
    }
}
