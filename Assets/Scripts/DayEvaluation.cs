using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectiveResult
{
    public string Text { get; }
    public bool Met { get; }
    public ObjectiveResult(string text, bool met) { Text = text; Met = met; }
}

// The HUD, frozen reports and outcome rules use the same evaluation.
public static class DayEvaluation
{
    public static List<ObjectiveResult> Evaluate(DayDefinition day, CompanyMetrics today, CompanyMetrics company)
    {
        var results = new List<ObjectiveResult>();
        if (day == null || today == null || company == null) return results;
        if (day.recruitment != null)
            foreach (var objective in day.recruitment)
            {
                if (objective == null) continue;
                int count = today.CountQualified(objective), required = Mathf.Max(1, objective.hiresRequired);
                results.Add(new ObjectiveResult(DayObjectiveText.Recruitment(objective) + " : " + count + " / " + required, count >= required));
            }
        if (day.requirePerformance)
            results.Add(new ObjectiveResult("Performance moyenne : " + company.AveragePerformance.ToString("0.#") +
                " / minimum " + day.minimumAveragePerformance.ToString("0.#"), company.EmployeeCount > 0 && company.AveragePerformance >= day.minimumAveragePerformance));
        if (day.composition != null)
            foreach (var objective in day.composition)
            {
                if (objective == null) continue;
                float value = company.Percentage(objective.metric);
                results.Add(new ObjectiveResult(DayObjectiveText.Composition(objective.metric) + " : " + value.ToString("0.#") +
                    " % / minimum " + objective.minimumPercent.ToString("0.#") + " %", company.EmployeeCount > 0 && value >= objective.minimumPercent));
            }
        if (day.requireSalaryBudget)
            results.Add(new ObjectiveResult("Masse salariale mensuelle : " + company.MonthlyPayroll + " € / plafond " + day.maximumMonthlyPayroll + " €",
                company.MonthlyPayroll <= day.maximumMonthlyPayroll));
        if (day.requireNewHireBudget)
            results.Add(new ObjectiveResult("Salaires mensuels ajoutés aujourd'hui : " + today.MonthlyPayroll + " € / plafond " + day.maximumNewHireMonthlyPayroll + " €",
                today.MonthlyPayroll <= day.maximumNewHireMonthlyPayroll));
        if (day.requireOnboardingCapacity)
            results.Add(new ObjectiveResult("Accompagnement des nouvelles recrues : " + company.OnboardingHours + " h/semaine / capacité " + day.maximumOnboardingHours + " h",
                company.OnboardingHours <= day.maximumOnboardingHours));
        if (day.requireSecurity)
            results.Add(new ObjectiveResult("Risque de sécurité maximal : " + company.MaximumSecurityRisk + "/100 / plafond " + day.maximumSecurityRisk,
                company.MaximumSecurityRisk <= day.maximumSecurityRisk));
        return results;
    }
}
