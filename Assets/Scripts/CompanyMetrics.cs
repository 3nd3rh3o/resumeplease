using System.Collections.Generic;
using UnityEngine;

// A derived snapshot. No score is stored on, or written back to, shared assets.
public sealed class CompanyMetrics
{
    private readonly List<Candidate> employees = new List<Candidate>();
    public int EmployeeCount => employees.Count;
    public float AveragePerformance { get; }
    public long MonthlyPayroll { get; }
    public int OnboardingHours { get; }
    public int MaximumSecurityRisk { get; }

    public CompanyMetrics(IEnumerable<Candidate> candidates)
    {
        var seen = new HashSet<Candidate>();
        float performance = 0;
        if (candidates != null)
            foreach (var candidate in candidates)
            {
                if (candidate == null || !seen.Add(candidate))
                    continue;
                employees.Add(candidate);
                performance += candidate.profile == null ? 0 : Mathf.Clamp(candidate.profile.performance, 0, 100);
                if (candidate.profile != null)
                {
                    MonthlyPayroll += System.Math.Max(0, candidate.profile.monthlySalary);
                    OnboardingHours += Mathf.Clamp(candidate.profile.onboardingHours, 0, 40);
                    MaximumSecurityRisk = Mathf.Max(MaximumSecurityRisk, Mathf.Clamp(candidate.profile.securityRisk, 0, 100));
                }
            }
        AveragePerformance = EmployeeCount == 0 ? 0 : performance / EmployeeCount;
    }

    public int CountQualified(RecruitmentObjective objective)
    {
        if (objective == null)
            return 0;
        int count = 0;
        foreach (var candidate in employees)
        {
            var profile = candidate.profile;
            if (profile == null || (objective.role != null && profile.role != objective.role))
                continue;
            bool qualified = true;
            if (objective.requiredSkills != null)
                foreach (var requirement in objective.requiredSkills)
                    if (requirement != null && requirement.skill != null &&
                        profile.GetSkillLevel(requirement.skill) < Mathf.Clamp(requirement.level, 0, 100))
                    {
                        qualified = false;
                        break;
                    }
            if (qualified) count++;
        }
        return count;
    }

    public int CountWithSkill(SkillDefinition skill, int minimumLevel)
    {
        if (skill == null)
            return 0;
        int count = 0;
        foreach (var candidate in employees)
            if (candidate.profile != null && candidate.profile.GetSkillLevel(skill) >= minimumLevel)
                count++;
        return count;
    }

    public float Percentage(CompositionMetric metric)
    {
        if (EmployeeCount == 0)
            return 0;
        int count = 0;
        foreach (var candidate in employees)
        {
            var profile = candidate.profile;
            if (profile == null) continue;
            bool matches = metric == CompositionMetric.Women ? profile.gender == CandidateGender.Woman
                : metric == CompositionMetric.Men ? profile.gender == CandidateGender.Man
                : metric == CompositionMetric.NonBinary ? profile.gender == CandidateGender.NonBinary
                : profile.disability == DisabilityStatus.Yes;
            if (matches) count++;
        }
        // Undisclosed profiles stay in the total; they are never inferred into a category.
        return count * 100f / EmployeeCount;
    }
}
