using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Exhaustive content check: use after editing the day SOs to catch an accidental dead end before the finale.
public static class CampaignValidationTools
{
    private sealed class Route
    {
        public List<Candidate> employees = new List<Candidate>();
        public string choices = "";
    }

    [MenuItem("Resume Please/Valider la progression de la campagne")]
    public static void ValidateMenu()
    {
        try { Debug.Log(ValidateCampaign()); }
        catch (Exception error) { Debug.LogException(error); }
    }

    public static string ValidateCampaign()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(GameBuildTools.CatalogPath);
        if (catalog == null) throw new InvalidOperationException("Catalogue absent.");
        var routes = new List<Route> { new Route() };
        var seen = new HashSet<DayDefinition>();
        string scene = catalog.firstDayScene;
        var summary = new StringBuilder();
        while (!string.IsNullOrWhiteSpace(scene))
        {
            var entry = catalog.days.Find(item => item.scenePath == scene);
            if (entry == null || entry.asset == null || !seen.Add(entry.asset)) throw new InvalidOperationException("Chaîne de journées absente ou cyclique : " + scene);
            var day = entry.asset;
            if (day.candidates == null || day.candidates.Count > 16 || day.candidates.Any(candidate => candidate == null) ||
                day.candidates.Distinct().Count() != day.candidates.Count) throw new InvalidOperationException("Liste de candidats invalide : " + day.name);
            foreach (var person in day.candidates)
            {
                if (person.profile == null || person.m_CV == null || person.m_motivationLetter == null || person.mesh == null || person.interviewDialogue == null)
                    throw new InvalidOperationException("Dossier incomplet : " + person.name);
                string cv = person.m_CV.m_education + "\n" + person.m_CV.m_experience + "\n" + person.m_CV.m_skills + "\n" + person.m_CV.m_references;
                ValidateTags(cv, person.m_CV.uIDocuments, person.name + " (CV)");
                ValidateTags(person.m_motivationLetter.m_content, person.m_motivationLetter.uIDocuments, person.name + " (lettre)");
            }
            var next = new List<Route>();
            int successfulSubsets = 0;
            foreach (var route in routes)
                for (int mask = 0; mask < (1 << day.candidates.Count); mask++)
                {
                    var hires = new List<Candidate>();
                    for (int i = 0; i < day.candidates.Count; i++) if ((mask & (1 << i)) != 0) hires.Add(day.candidates[i]);
                    if (hires.Count > day.interviewCapacity) continue;
                    var employees = new List<Candidate>(route.employees);
                    employees.AddRange(hires);
                    var company = new CompanyMetrics(employees);
                    var progress = new DayProgress(day, route.employees);
                    foreach (var person in hires) progress.Record(person, true);
                    if (!progress.MeetsObjectives(company)) continue;
                    successfulSubsets++;
                    if (!day.isFinalDay && CampaignOutcome.Evaluate(day, company, 0).Kind == CampaignEnding.None)
                        next.Add(new Route { employees = employees, choices = route.choices + day.name + " : " + string.Join(", ", hires.Select(person => person.name)) + "\n" });
                }
            if (day.isFinalDay)
            {
                if (!string.IsNullOrWhiteSpace(day.nextScenePath) || successfulSubsets != 0 || string.IsNullOrWhiteSpace(day.finalMessage))
                    throw new InvalidOperationException("Le dernier jour doit être terminal, impossible à satisfaire et posséder un texte de fin.");
                return summary.AppendLine(day.name + " : aucune combinaison ne satisfait toutes les demandes.")
                    .AppendLine("Exemple de parcours jusqu'à la finale :").Append(routes[0].choices).ToString();
            }
            if (next.Count == 0) throw new InvalidOperationException("Aucun parcours sans échec vers la fin de " + day.name);
            summary.AppendLine(day.name + " : " + next.Count + " parcours satisfont les objectifs.");
            routes = next;
            scene = day.nextScenePath;
        }
        throw new InvalidOperationException("La chaîne se termine sans journée finale.");
    }

    private static void ValidateTags(string text, List<UnityEngine.UIElements.VisualTreeAsset> documents, string owner)
    {
        foreach (Match tag in Regex.Matches(text ?? "", @"<([0-9]+)>"))
        {
            int id = int.Parse(tag.Groups[1].Value);
            if (documents == null || id >= documents.Count || documents[id] == null || !text.Contains("<" + id + "/>"))
                throw new InvalidOperationException("Balise sans justificatif : " + owner + " / " + id);
        }
    }
}
