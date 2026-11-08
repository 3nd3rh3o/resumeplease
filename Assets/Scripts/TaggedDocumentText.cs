using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

// Shared text rendering only: document ownership stays in each review component.
internal sealed class TaggedDocumentText
{
    private static readonly Regex LinkPattern = new Regex(@"<([0-9]+)>([\s\S]*?)<\1/>");
    private static readonly Regex TagPattern = new Regex(@"<[0-9]+/?>");
    private readonly string source;
    private readonly List<Match> links = new List<Match>();
    private readonly List<int> documentIds = new List<int>();
    private readonly List<LinkBounds> bounds = new List<LinkBounds>();
    private int hoveredLink = -1;

    private struct LinkBounds
    {
        public int link;
        public int line;
        public Rect rect;
    }

    public Label Label { get; }
    public string FieldName { get; }

    public TaggedDocumentText(string fieldName, string text, float fontSize)
    {
        FieldName = fieldName;
        source = text;
        foreach (Match match in LinkPattern.Matches(text))
        {
            if (!int.TryParse(match.Groups[1].Value, out var id))
                continue;
            links.Add(match);
            documentIds.Add(id);
        }

        Label = new Label { enableRichText = true, parseEscapeSequences = false };
        Label.style.width = Length.Percent(100);
        Label.style.height = StyleKeyword.Auto;
        Label.style.flexGrow = 0;
        Label.style.flexShrink = 0;
        Label.style.alignSelf = Align.Stretch;
        Label.style.paddingTop = Label.style.paddingBottom = 0;
        Label.style.paddingLeft = Label.style.paddingRight = 0;
        Label.style.marginTop = Label.style.marginBottom = 0;
        Label.style.marginLeft = Label.style.marginRight = 0;
        Label.style.fontSize = fontSize;
        Label.style.whiteSpace = WhiteSpace.Normal;
        Label.PostProcessTextVertices += CacheLinkBounds;
        RefreshText();
    }

    public int HitTest(Vector2 localPoint)
    {
        foreach (var area in bounds)
            if (area.rect.Contains(localPoint))
                return area.link;
        return -1;
    }

    public int DocumentId(int link) => documentIds[link];

    public void SetHoveredLink(int link)
    {
        if (hoveredLink == link)
            return;
        hoveredLink = link;
        RefreshText();
    }

    private void RefreshText()
    {
        var result = new StringBuilder();
        int start = 0;
        for (int i = 0; i < links.Count; i++)
        {
            var match = links[i];
            result.Append(Escape(source.Substring(start, match.Index - start)));
            result.Append("<link=\"").Append(i).Append("\"><b>");
            if (i == hoveredLink) result.Append("<u>");
            result.Append(Escape(match.Groups[2].Value));
            if (i == hoveredLink) result.Append("</u>");
            result.Append("</b></link>");
            start = match.Index + match.Length;
        }
        result.Append(Escape(source.Substring(start)));
        Label.text = result.ToString();
    }

    private static string Escape(string text)
    {
        text = TagPattern.Replace(text, string.Empty);
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private void CacheLinkBounds(TextElement.GlyphsEnumerable glyphs)
    {
        // Use the actual rendered positions, including wrapping and font metrics.
        bounds.Clear();
        foreach (var glyph in glyphs)
        {
            if (!int.TryParse(glyph.linkID, out var link) || glyph.vertices.Length == 0)
                continue;

            var min = (Vector2)glyph.vertices[0].position;
            var max = min;
            for (int i = 1; i < glyph.vertices.Length; i++)
            {
                var point = (Vector2)glyph.vertices[i].position;
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            int index = bounds.Count - 1;
            if (index >= 0 && bounds[index].link == link && bounds[index].line == glyph.line)
            {
                min = Vector2.Min(min, bounds[index].rect.min);
                max = Vector2.Max(max, bounds[index].rect.max);
                bounds[index] = new LinkBounds { link = link, line = glyph.line, rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y) };
            }
            else
                bounds.Add(new LinkBounds { link = link, line = glyph.line, rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y) });
        }
    }
}
