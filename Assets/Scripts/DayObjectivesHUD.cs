using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class DayObjectivesHUD : MonoBehaviour
{
    [SerializeField] private Text objectivesText;
    private ScenarioController scenario;
    private AcceptedCandidates accepted;
    private InterviewResults results;
    private bool subscribed;

    public void Bind(ScenarioController controller, AcceptedCandidates selection, InterviewResults company)
    {
        Unsubscribe();
        scenario = controller;
        accepted = selection;
        results = company;
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribed || scenario == null || scenario.Progress == null || !isActiveAndEnabled)
            return;
        scenario.StateChanged += Refresh;
        scenario.Progress.Changed += Refresh;
        accepted.Changed += Refresh;
        results.Changed += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (scenario != null)
        {
            scenario.StateChanged -= Refresh;
            if (scenario.Progress != null) scenario.Progress.Changed -= Refresh;
        }
        if (accepted != null) accepted.Changed -= Refresh;
        if (results != null) results.Changed -= Refresh;
        subscribed = false;
    }

    private void Refresh()
    {
        if (objectivesText == null || scenario == null || scenario.Progress == null)
            return;
        var day = scenario.DayDefinition;
        var today = scenario.Progress.Today;
        var company = results.Metrics;
        var text = new StringBuilder(day.title);
        text.AppendLine().AppendLine(scenario.InterviewsCompleted ? "Journée terminée"
            : scenario.IsInterviewPhase ? "Entretiens" : "Revue des candidatures");
        foreach (var result in DayEvaluation.Evaluate(day, today, company))
            text.AppendLine().Append(result.Met ? "✓ " : "○ ").Append(result.Text);

        text.AppendLine().AppendLine();
        if (scenario.IsInterviewPhase || scenario.InterviewsCompleted)
            text.Append("Entretiens effectués : ").Append(scenario.Progress.InterviewsCompleted).Append(" / ").Append(accepted.Candidates.Count);
        else
            text.Append("Places d'entretien : ").Append(Mathf.Max(0, accepted.MaxCapacity - accepted.Candidates.Count))
                .Append(" / ").Append(accepted.MaxCapacity);

        // Keep later days readable as their list of constraints grows.
        objectivesText.fontSize = day.requireOnboardingCapacity ? 14 : day.requireSalaryBudget ? 16 : 18;
        objectivesText.text = text.ToString();
    }

    // Used by the editor authoring tool: the generated view is saved in BaseScenePrefab.
    public static DayObjectivesHUD CreateView(Transform parent)
    {
        var root = new GameObject("Day Objectives", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = new GameObject("Objectives Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(350, 0);
        var image = panel.GetComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        image.raycastTarget = false;
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var label = new GameObject("Objectives Text", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(panel.transform, false);
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var hud = root.AddComponent<DayObjectivesHUD>();
        hud.objectivesText = text;
        return hud;
    }
}
