using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Serializable snapshot: later asset edits or hires do not rewrite a completed report.
[Serializable]
public sealed class DayReport
{
    [SerializeField] private string dayTitle;
    [SerializeField] private int examined;
    [SerializeField] private int unread;
    [SerializeField] private int interviews;
    [SerializeField] private int workforceBefore;
    [SerializeField] private int workforceAfter;
    [SerializeField] private float performanceBefore;
    [SerializeField] private float performanceAfter;
    [SerializeField] private bool objectivesMet;
    [SerializeField] private List<string> hires = new List<string>();
    [SerializeField] private List<string> rejections = new List<string>();
    [SerializeField] private List<string> objectives = new List<string>();
    [SerializeField] private long payrollBefore, payrollAfter;
    [SerializeField] private int onboardingBefore, onboardingAfter, securityBefore, securityAfter;
    [SerializeField] private int consecutiveMissedDays;
    [SerializeField] private CampaignEnding ending;
    [SerializeField] private string endingTitle, endingMessage;
    [SerializeField] private bool showPayroll, showOnboarding, showSecurity;

    public string DayTitle => dayTitle;
    public bool ObjectivesMet => objectivesMet;
    public int HiresCount => hires.Count;
    public int InterviewsCount => interviews;
    public int UnreadCount => unread;
    public CampaignEnding Ending => ending;
    public bool EndsCampaign => ending != CampaignEnding.None;
    public string EndingTitle => endingTitle;
    public string EndingMessage => endingMessage;
    public int ConsecutiveMissedDays => consecutiveMissedDays;
    public bool IsValidSnapshot => !string.IsNullOrWhiteSpace(dayTitle) && examined >= 0 && unread >= 0 && interviews >= 0 &&
        workforceBefore >= 0 && workforceAfter >= 0 && !float.IsNaN(performanceBefore) && !float.IsInfinity(performanceBefore) &&
        !float.IsNaN(performanceAfter) && !float.IsInfinity(performanceAfter) && performanceBefore >= 0 && performanceBefore <= 100 &&
        performanceAfter >= 0 && performanceAfter <= 100 && hires != null && rejections != null && objectives != null &&
        payrollBefore >= 0 && payrollAfter >= 0 && onboardingBefore >= 0 && onboardingAfter >= 0 &&
        securityBefore >= 0 && securityBefore <= 100 && securityAfter >= 0 && securityAfter <= 100 && consecutiveMissedDays >= 0 &&
        Enum.IsDefined(typeof(CampaignEnding), ending) && (!EndsCampaign || (!string.IsNullOrWhiteSpace(endingTitle) && !string.IsNullOrWhiteSpace(endingMessage)));

    public static DayReport Capture(DayDefinition day, DayProgress progress, int remaining,
        CompanyMetrics before, CompanyMetrics after, IReadOnlyList<DayReport> previousReports = null)
    {
        var report = new DayReport
        {
            dayTitle = string.IsNullOrWhiteSpace(day.title) ? day.name : day.title,
            unread = remaining,
            examined = Mathf.Max(0, (day.candidates == null ? 0 : day.candidates.Count) - remaining),
            interviews = progress.InterviewsCompleted,
            workforceBefore = before.EmployeeCount,
            workforceAfter = after.EmployeeCount,
            performanceBefore = before.AveragePerformance,
            performanceAfter = after.AveragePerformance,
            objectivesMet = progress.MeetsObjectives(after),
            payrollBefore = before.MonthlyPayroll, payrollAfter = after.MonthlyPayroll,
            onboardingBefore = before.OnboardingHours, onboardingAfter = after.OnboardingHours,
            securityBefore = before.MaximumSecurityRisk, securityAfter = after.MaximumSecurityRisk,
            showPayroll = day.requireSalaryBudget || day.requireNewHireBudget,
            showOnboarding = day.requireOnboardingCapacity, showSecurity = day.requireSecurity
        };
        foreach (var candidate in progress.HiredToday) report.hires.Add(CandidateName(candidate));
        foreach (var candidate in progress.RejectedToday) report.rejections.Add(CandidateName(candidate));
        foreach (var result in DayEvaluation.Evaluate(day, progress.Today, after))
            report.objectives.Add((result.Met ? "Atteint — " : "Non atteint — ") + result.Text);
        report.consecutiveMissedDays = CampaignOutcome.MissedDays(report.objectivesMet, previousReports);
        var outcome = CampaignOutcome.Evaluate(day, after, report.consecutiveMissedDays);
        report.ending = outcome.Kind;
        report.endingTitle = outcome.Title;
        report.endingMessage = outcome.Message;
        return report;
    }

    private static string CandidateName(Candidate candidate) => candidate.m_CV == null ? candidate.name
        : (candidate.m_CV.m_surname + " " + candidate.m_CV.m_name).Trim();

    public string ToDisplayText()
    {
        var text = new StringBuilder("RAPPORT — ").AppendLine(dayTitle);
        text.Append("Dossiers examinés : ").Append(examined).Append(" ; non examinés : ").AppendLine(unread.ToString());
        text.Append("Entretiens réalisés : ").AppendLine(interviews.ToString());
        text.Append("Recrutements : ").AppendLine(hires.Count == 0 ? "aucun" : string.Join(", ", hires));
        text.Append("Refus après entretien : ").AppendLine(rejections.Count == 0 ? "aucun" : string.Join(", ", rejections));
        text.AppendLine().AppendLine(objectivesMet ? "Objectifs de la journée atteints." : "Certains objectifs de la journée ne sont pas atteints.");
        foreach (var objective in objectives) text.Append("• ").AppendLine(objective);
        text.AppendLine().Append("Effectif : ").Append(workforceBefore).Append(" → ").AppendLine(workforceAfter.ToString());
        text.Append("Performance moyenne de l'équipe : ").Append(workforceBefore == 0 ? "—" : performanceBefore.ToString("0.#"))
            .Append(" → ").Append(workforceAfter == 0 ? "—" : performanceAfter.ToString("0.#")).AppendLine(" / 100");
        if (showPayroll) text.Append("Masse salariale mensuelle : ").Append(payrollBefore).Append(" → ").Append(payrollAfter).AppendLine(" €");
        if (showOnboarding) text.Append("Accompagnement hebdomadaire : ").Append(onboardingBefore).Append(" → ").Append(onboardingAfter).AppendLine(" h");
        if (showSecurity) text.Append("Risque de sécurité maximal : ").Append(securityBefore).Append(" → ").Append(securityAfter).AppendLine(" / 100");
        text.Append("Journées consécutives avec objectifs non atteints : ").AppendLine(consecutiveMissedDays.ToString());
        return text.ToString();
    }
}
