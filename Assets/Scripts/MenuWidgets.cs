using UnityEngine;
using UnityEngine.UI;

internal static class MenuWidgets
{
    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static Text Label(Transform parent, string value, int size = 20)
    {
        var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = Font; text.fontSize = size; text.color = Color.white;
        text.text = value; text.supportRichText = false; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    public static Button Button(Transform parent, string value, UnityEngine.Events.UnityAction callback)
    {
        var obj = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(.18f, .26f, .36f);
        obj.GetComponent<LayoutElement>().minHeight = 44;
        var button = obj.GetComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(callback);
        var label = Label(obj.transform, value);
        Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(12, 4); label.rectTransform.offsetMax = new Vector2(-12, -4);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    public static InputField Input(Transform parent, string value)
    {
        var obj = new GameObject("Save name", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<LayoutElement>().minHeight = 46;
        obj.GetComponent<Image>().color = new Color(.12f, .17f, .23f);
        var text = Label(obj.transform, value); Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12, 4); text.rectTransform.offsetMax = new Vector2(-12, -4);
        text.alignment = TextAnchor.MiddleLeft;
        var field = obj.GetComponent<InputField>();
        field.textComponent = text; field.targetGraphic = obj.GetComponent<Image>();
        field.characterLimit = 80; field.text = value;
        return field;
    }
    public static Dropdown Dropdown(Transform parent, string[] choices, int value)
    {
        var obj = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Dropdown), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(.15f, .22f, .31f);
        obj.GetComponent<LayoutElement>().minHeight = 44;
        var dropdown = obj.GetComponent<Dropdown>(); dropdown.targetGraphic = obj.GetComponent<Image>();
        var caption = Label(obj.transform, ""); Stretch(caption.rectTransform);
        caption.rectTransform.offsetMin = new Vector2(12, 0); caption.alignment = TextAnchor.MiddleLeft;
        dropdown.captionText = caption;

        var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(obj.transform, false);
        var tr = template.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0); tr.pivot = new Vector2(.5f, 1);
        tr.sizeDelta = new Vector2(0, 220);
        template.GetComponent<Image>().color = new Color(.08f, .12f, .18f);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(template.transform, false); Stretch(viewport.GetComponent<RectTransform>());
        var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1); cr.sizeDelta = new Vector2(0, 40);
        var item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle)); item.transform.SetParent(content.transform, false);
        var ir = item.GetComponent<RectTransform>(); Stretch(ir); ir.sizeDelta = Vector2.zero;
        item.GetComponent<Image>().color = new Color(.18f, .27f, .38f);
        var itemText = Label(item.transform, "Option"); Stretch(itemText.rectTransform);
        itemText.rectTransform.offsetMin = new Vector2(12, 0); itemText.alignment = TextAnchor.MiddleLeft;
        item.GetComponent<Toggle>().targetGraphic = item.GetComponent<Image>();
        dropdown.template = tr; dropdown.itemText = itemText;
        var scroll = template.GetComponent<ScrollRect>(); scroll.content = cr; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false;
        template.SetActive(false);
        dropdown.AddOptions(new System.Collections.Generic.List<string>(choices)); dropdown.value = value; dropdown.RefreshShownValue();
        return dropdown;
    }
    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
