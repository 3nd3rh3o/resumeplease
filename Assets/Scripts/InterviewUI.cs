using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Screen-space dialogue controls, independent from the read-only computer screen.
public sealed class InterviewUI : MonoBehaviour
{
    private Text title;
    private Text body;
    private Button first;
    private Button second;
    private Button next;
    private ScrollRect responseScroll;
    private Font font;
    private InterviewSession session;
    private string candidateName;

    public void Initialize()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            var events = new GameObject("Interview Input", typeof(EventSystem));
            events.transform.SetParent(transform, false);
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var panel = new GameObject("Dialogue", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.02f);
        rect.anchorMax = new Vector2(0.9f, 0.67f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.97f);
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 8;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        title = MakeText("Candidate", panel.transform, 24);
        title.gameObject.AddComponent<LayoutElement>().minHeight = 32;

        var scrollObject = new GameObject("Response", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        scrollObject.transform.SetParent(panel.transform, false);
        scrollObject.GetComponent<Image>().color = new Color(0, 0, 0, 0.1f);
        var scrollLayout = scrollObject.GetComponent<LayoutElement>();
        scrollLayout.minHeight = 100;
        scrollLayout.flexibleHeight = 1;
        responseScroll = scrollObject.GetComponent<ScrollRect>();
        responseScroll.horizontal = false;
        responseScroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        responseScroll.viewport = viewport.GetComponent<RectTransform>();
        body = MakeText("Text", viewport.transform, 20);
        var content = body.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        responseScroll.content = content;

        first = MakeButton("First question", panel.transform, () => Choose(0), 66);
        second = MakeButton("Second question", panel.transform, () => Choose(1), 66);
        next = MakeButton("Continuer", panel.transform, Continue, 44);
        Hide();
    }

    public void Show(Candidate candidate, InterviewSession interview)
    {
        session = interview;
        candidateName = candidate.m_CV != null
            ? $"{candidate.m_CV.m_name} {candidate.m_CV.m_surname}".Trim()
            : candidate.name;
        gameObject.SetActive(true);
        Render();
    }

    public void Hide()
    {
        session = null;
        gameObject.SetActive(false);
    }

    private void Choose(int index)
    {
        if (session == null)
            return;
        if (session.AwaitingDecision)
        {
            if (session.Decide(index == 0))
                Hide();
        }
        else if (session.ChooseQuestion(index))
            Render();
    }

    private void Continue()
    {
        if (session != null && session.Continue())
            Render();
    }

    private void Render()
    {
        bool decision = session.AwaitingDecision;
        bool answered = session.SelectedQuestion >= 0;
        title.text = decision ? candidateName + " — Décision"
            : $"{candidateName} — Étape {session.StepIndex + 1}/{session.StepCount}";
        body.text = decision ? "Souhaitez-vous retenir ce candidat ?"
            : answered ? session.Response : "Choisissez une question à poser.";
        first.gameObject.SetActive(!answered);
        second.gameObject.SetActive(!answered);
        first.GetComponentInChildren<Text>(true).text = decision ? "Retenir le candidat" : session.CurrentStep?.first?.question;
        second.GetComponentInChildren<Text>(true).text = decision ? "Rejeter le candidat" : session.CurrentStep?.second?.question;
        next.gameObject.SetActive(answered && !decision);
        Canvas.ForceUpdateCanvases();
        responseScroll.verticalNormalizedPosition = 1;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private Text MakeText(string name, Transform parent, int size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string caption, Transform parent, UnityEngine.Events.UnityAction action, float height)
    {
        var obj = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(0.18f, 0.26f, 0.36f);
        obj.GetComponent<LayoutElement>().minHeight = height;
        var button = obj.GetComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        var label = MakeText("Caption", obj.transform, 20);
        label.text = caption;
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.GetComponent<RectTransform>());
        label.GetComponent<RectTransform>().offsetMin = new Vector2(12, 4);
        label.GetComponent<RectTransform>().offsetMax = new Vector2(-12, -4);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
