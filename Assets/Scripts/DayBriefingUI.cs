using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public sealed class DayBriefingUI : MonoBehaviour
{
    [SerializeField] private Text bodyText;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Text footerText;
    private InputAction action;
    private bool enabledAction;
    private bool waitForRelease;
    private int openingFrame;
    public bool IsOpen => gameObject.activeSelf;

    public void Show(string text, InputActionReference interaction, bool finalReport = false)
    {
        action = interaction == null ? null : interaction.action;
        enabledAction = action != null && !action.enabled;
        if (enabledAction) action.Enable();
        waitForRelease = InteractionHeld();
        openingFrame = Time.frameCount;
        bodyText.text = text;
        footerText.text = finalReport ? "Molette : faire défiler  •  Touche d'interaction : fermer le bilan"
            : "Molette : faire défiler  •  Touche d'interaction : commencer la journée";
        gameObject.SetActive(true);
        if (EventSystem.current == null && GetComponentInChildren<EventSystem>(true) == null)
        {
            var events = new GameObject("Briefing Input", typeof(EventSystem));
            events.transform.SetParent(transform, false);
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1;
    }

    private void Update()
    {
        if (GameSession.InputBlocked) return;
        if (action == null || Time.frameCount <= openingFrame) return;
        if (waitForRelease)
        {
            if (!InteractionHeld()) waitForRelease = false;
            return;
        }
        if (InteractionHeld()) Dismiss();
    }

    public void Dismiss() => gameObject.SetActive(false);

    private bool InteractionHeld()
    {
        if (action == null) return false;
        if (action.IsPressed()) return true;
        foreach (var control in action.controls)
            if (control is ButtonControl button && button.isPressed) return true;
        return false;
    }

    private void OnDisable()
    {
        if (enabledAction && action != null) action.Disable();
        action = null;
        enabledAction = false;
    }

    public static DayBriefingUI CreateView(Transform parent)
    {
        var root = new GameObject("Day Briefing", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 200;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        var backdrop = new GameObject("Fullscreen", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(root.transform, false);
        SetBounds(backdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        backdrop.GetComponent<Image>().color = new Color(.035f,.05f,.075f,1f);

        var heading = MakeText(root.transform, "Rapport et consignes", 28);
        heading.fontStyle = FontStyle.Bold;
        SetBounds(heading.rectTransform, new Vector2(.08f,.88f), new Vector2(.92f,.96f));
        var footer = MakeText(root.transform, "Molette : faire défiler  •  Touche d'interaction : commencer la journée", 20);
        SetBounds(footer.rectTransform, new Vector2(.08f,.03f), new Vector2(.92f,.12f));

        var scrollObject = new GameObject("Report Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(root.transform, false);
        SetBounds(scrollObject.GetComponent<RectTransform>(), new Vector2(.08f,.16f), new Vector2(.92f,.86f));
        scrollObject.GetComponent<Image>().color = new Color(.08f,.105f,.145f,1f);
        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        SetBounds(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        scroll.viewport = viewport.GetComponent<RectTransform>();
        var body = MakeText(viewport.transform, "", 20);
        body.rectTransform.anchorMin = new Vector2(0,1);
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.pivot = new Vector2(.5f,1);
        body.rectTransform.offsetMin = new Vector2(18,0);
        body.rectTransform.offsetMax = new Vector2(-18,0);
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = body.rectTransform;
        var view = root.AddComponent<DayBriefingUI>();
        view.bodyText = body;
        view.scroll = scroll;
        view.footerText = footer;
        root.SetActive(false);
        return view;
    }

    private static Text MakeText(Transform parent, string value, int size)
    {
        var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = value;
        return text;
    }

    private static void SetBounds(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
