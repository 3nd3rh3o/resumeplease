using System;
using System.Collections.Generic;
using UnityEngine;

public enum CampaignEnding { None, Dismissed, Bankruptcy, DataBreach, Burnout, Finale }
public enum FailureMetric { ConsecutiveMissedDays, AveragePerformance, MonthlyPayroll, OnboardingHours, SecurityRisk }
public enum ThresholdComparison { Below, Above, AtLeast }

[Serializable]
public sealed class DayFailureRule
{
    public FailureMetric metric;
    public ThresholdComparison comparison;
    [Min(0)] public float threshold;
    public CampaignEnding ending = CampaignEnding.Dismissed;
    public string title;
    [TextArea(2, 8)] public string message;

    public string Description => MetricLabel(metric) + (comparison == ThresholdComparison.Below ? " < " : comparison == ThresholdComparison.Above ? " > " : " ≥ ") +
        threshold.ToString("0.#") + " : " + (string.IsNullOrWhiteSpace(title) ? ending.ToString() : title);

    private static string MetricLabel(FailureMetric metric)
    {
        switch (metric)
        {
            case FailureMetric.ConsecutiveMissedDays: return "Journées consécutives avec objectifs non atteints";
            case FailureMetric.AveragePerformance: return "Performance moyenne";
            case FailureMetric.MonthlyPayroll: return "Masse salariale mensuelle (€)";
            case FailureMetric.OnboardingHours: return "Accompagnement nécessaire (h/semaine)";
            default: return "Risque de sécurité maximal (/100)";
        }
    }
}

public sealed class CampaignOutcome
{
    public CampaignEnding Kind { get; }
    public string Title { get; }
    public string Message { get; }
    private CampaignOutcome(CampaignEnding kind, string title, string message) { Kind = kind; Title = title; Message = message; }

    public static int MissedDays(bool objectivesMet, IReadOnlyList<DayReport> previous)
    {
        if (objectivesMet) return 0;
        int missed = 1;
        if (previous != null)
            for (int i = previous.Count - 1; i >= 0 && !previous[i].ObjectivesMet; i--) missed++;
        return missed;
    }

    public static CampaignOutcome Evaluate(DayDefinition day, CompanyMetrics company, int missedDays)
    {
        // A narrative ending is reached by playing the final day, even when its contradictory targets are missed.
        if (day.isFinalDay)
            return new CampaignOutcome(CampaignEnding.Finale, "Des objectifs sans issue", day.finalMessage);
        if (day.failureRules != null)
            foreach (var rule in day.failureRules)
            {
                if (rule == null || rule.ending == CampaignEnding.None || rule.ending == CampaignEnding.Finale) continue;
                double value = rule.metric == FailureMetric.ConsecutiveMissedDays ? missedDays
                    : rule.metric == FailureMetric.AveragePerformance ? company.AveragePerformance
                    : rule.metric == FailureMetric.MonthlyPayroll ? company.MonthlyPayroll
                    : rule.metric == FailureMetric.OnboardingHours ? company.OnboardingHours : company.MaximumSecurityRisk;
                bool triggered = rule.comparison == ThresholdComparison.Below ? value < rule.threshold
                    : rule.comparison == ThresholdComparison.Above ? value > rule.threshold : value >= rule.threshold;
                if (triggered)
                    return new CampaignOutcome(rule.ending, rule.title, rule.message + "\n\nCondition constatée : " + rule.Description + " (valeur : " + value.ToString("0.#") + ").");
            }
        return new CampaignOutcome(CampaignEnding.None, "", "");
    }
}
