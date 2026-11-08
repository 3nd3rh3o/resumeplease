using System.Text;

public static class DayObjectiveText
{
    public static string Recruitment(RecruitmentObjective objective)
    {
        var text = new StringBuilder(objective.role == null ? "Recrutements" : objective.role.DisplayName);
        if (objective.requiredSkills != null)
            foreach (var skill in objective.requiredSkills)
                if (skill != null && skill.skill != null)
                    text.Append(" ; ").Append(skill.skill.DisplayName).Append(" ≥ ").Append(skill.level).Append("/100");
        return text.ToString();
    }

    public static string Composition(CompositionMetric metric)
    {
        switch (metric)
        {
            case CompositionMetric.Women: return "Femmes dans l'effectif";
            case CompositionMetric.Men: return "Hommes dans l'effectif";
            case CompositionMetric.NonBinary: return "Personnes non binaires";
            default: return "Salariés en situation de handicap";
        }
    }

    public static string Briefing(DayDefinition day, DayReport previous)
    {
        var text = new StringBuilder();
        if (previous != null) text.AppendLine(previous.ToDisplayText()).AppendLine("────────────────────────").AppendLine();
        text.AppendLine("CONSIGNES — " + day.title).AppendLine();
        if (!string.IsNullOrWhiteSpace(day.briefing)) text.AppendLine(day.briefing).AppendLine();
        if (!string.IsNullOrWhiteSpace(day.newConstraints)) text.AppendLine("Nouvelles contraintes").AppendLine(day.newConstraints).AppendLine();
        text.AppendLine("Objectifs du jour");
        if (day.recruitment != null)
            foreach (var objective in day.recruitment)
                if (objective != null) text.Append("• ").Append(objective.hiresRequired).Append(" recrutement(s) : ").AppendLine(Recruitment(objective));
        text.Append("• ").Append(day.interviewCapacity).AppendLine(" place(s) d'entretien maximum.");
        if (day.requirePerformance) text.Append("• Performance moyenne de l'équipe ≥ ").Append(day.minimumAveragePerformance.ToString("0.#")).AppendLine("/100.");
        if (day.composition != null)
            foreach (var objective in day.composition)
                if (objective != null) text.Append("• ").Append(Composition(objective.metric)).Append(" ≥ ").Append(objective.minimumPercent.ToString("0.#")).AppendLine(" %.");
        if (day.requireSalaryBudget) text.Append("• Masse salariale mensuelle de l'équipe ≤ ").Append(day.maximumMonthlyPayroll).AppendLine(" €.");
        if (day.requireNewHireBudget) text.Append("• Salaires mensuels ajoutés aujourd'hui ≤ ").Append(day.maximumNewHireMonthlyPayroll).AppendLine(" €.");
        if (day.requireOnboardingCapacity) text.Append("• Accompagnement total des recrues ≤ ").Append(day.maximumOnboardingHours).AppendLine(" h/semaine.");
        if (day.requireSecurity) text.Append("• Risque de sécurité maximal dans l'équipe ≤ ").Append(day.maximumSecurityRisk).AppendLine("/100.");
        if (day.failureRules != null && !day.isFinalDay)
        {
            text.AppendLine().AppendLine("Conséquences évaluées en fin de journée");
            foreach (var rule in day.failureRules)
                if (rule != null && rule.ending != CampaignEnding.None && rule.ending != CampaignEnding.Finale)
                    text.Append("• ").AppendLine(rule.Description);
        }
        if (day.requireSalaryBudget || day.requireNewHireBudget || day.requireOnboardingCapacity || day.requireSecurity)
            text.AppendLine().AppendLine("Les propositions de contrat et les références consultables indiquent salaires, accompagnement et pratiques de sécurité. L'accompagnement est suivi sur cette semaine d'intégration, sans diminution automatique entre deux jours.");
        return text.ToString();
    }
}
