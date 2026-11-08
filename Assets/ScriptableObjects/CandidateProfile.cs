using System;
using System.Collections.Generic;
using UnityEngine;

public enum CandidateGender { Undisclosed, Woman, Man, NonBinary }
public enum DisabilityStatus { Undisclosed, No, Yes }

[Serializable]
public class SkillLevel
{
    public SkillDefinition skill;
    [Range(0, 100)] public int level;
}

[Serializable]
public class CandidateProfile
{
    public JobRole role;
    [Tooltip("Niveaux réels, indépendants des compétences annoncées dans le CV.")]
    public List<SkillLevel> skills = new List<SkillLevel>();
    [Range(0, 100)] public int performance;
    public CandidateGender gender;
    public DisabilityStatus disability;
    [Tooltip("Salaire mensuel brut demandé, en euros. Indépendant des compétences et des caractéristiques personnelles.")]
    [Min(0)] public int monthlySalary;
    [Tooltip("Heures d'accompagnement hebdomadaires prévues pendant l'intégration.")]
    [Range(0, 40)] public int onboardingHours;
    [Tooltip("Risque lié aux pratiques de sécurité constatées dans le dossier, de 0 à 100.")]
    [Range(0, 100)] public int securityRisk;

    public int GetSkillLevel(SkillDefinition skill)
    {
        int level = 0;
        if (skill == null || skills == null)
            return level;
        foreach (var entry in skills)
            if (entry != null && entry.skill == skill)
                level = Mathf.Max(level, Mathf.Clamp(entry.level, 0, 100));
        return level;
    }
}
