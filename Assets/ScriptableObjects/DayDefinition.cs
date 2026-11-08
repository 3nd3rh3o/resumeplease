using System;
using System.Collections.Generic;
using UnityEngine;

public enum CompositionMetric { Women, Men, NonBinary, Disability }

[Serializable]
public class RecruitmentObjective
{
    public JobRole role;
    [Min(1)] public int hiresRequired = 1;
    public List<SkillLevel> requiredSkills = new List<SkillLevel>();
}

[Serializable]
public class CompositionObjective
{
    public CompositionMetric metric;
    [Range(0, 100)] public float minimumPercent;
}

[CreateAssetMenu(fileName = "Day", menuName = "Resume Please/Day")]
public class DayDefinition : ScriptableObject
{
    public string title = "Objectifs du jour";
    [TextArea(2, 6)] public string briefing;
    [TextArea(2, 6)] public string newConstraints;
    [Tooltip("Chemin de la prochaine scène, par exemple Assets/Scenes/Day1.unity. Vide pour la dernière journée.")]
    public string nextScenePath;
    public List<Candidate> candidates = new List<Candidate>();
    [Min(0)] public int interviewCapacity = 3;
    [Tooltip("Recrutements à effectuer aujourd'hui. Un profil doit satisfaire toutes les compétences d'un objectif.")]
    public List<RecruitmentObjective> recruitment = new List<RecruitmentObjective>();
    [Tooltip("Objectifs portant sur l'ensemble des salariés retenus, y compris les jours précédents.")]
    public List<CompositionObjective> composition = new List<CompositionObjective>();
    public bool requirePerformance;
    [Range(0, 100)] public float minimumAveragePerformance;

    [Header("Moyens accordés à l'équipe")]
    public bool requireSalaryBudget;
    [Min(0)] public int maximumMonthlyPayroll;
    public bool requireNewHireBudget;
    [Min(0)] public int maximumNewHireMonthlyPayroll;
    public bool requireOnboardingCapacity;
    [Min(0)] public int maximumOnboardingHours;
    public bool requireSecurity;
    [Range(0, 100)] public int maximumSecurityRisk;

    [Header("Conséquences de fin de journée")]
    [Tooltip("Évaluées après tous les entretiens. Le briefing annonce chaque règle active.")]
    public List<DayFailureRule> failureRules = new List<DayFailureRule>();
    public bool isFinalDay;
    [TextArea(3, 10)] public string finalMessage;

    private void OnValidate()
    {
        interviewCapacity = Mathf.Max(0, interviewCapacity);
        maximumMonthlyPayroll = Mathf.Max(0, maximumMonthlyPayroll);
        maximumNewHireMonthlyPayroll = Mathf.Max(0, maximumNewHireMonthlyPayroll);
        maximumOnboardingHours = Mathf.Max(0, maximumOnboardingHours);
    }
}
