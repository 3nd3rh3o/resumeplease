using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveGameValidator
{
    public static void Validate(SaveGameData save, GameContentCatalog catalog)
    {
        if (save == null || save.day == null || save.campaign == null) throw new InvalidOperationException("Sauvegarde incomplète.");
        var entry = catalog.Day(save.dayId);
        if (entry.scenePath != save.scenePath || !Application.CanStreamedLevelBeLoaded(entry.scenePath))
            throw new InvalidOperationException("La scène de cette sauvegarde n'est pas disponible dans le build.");
        var state = save.day;
        if (!Enum.IsDefined(typeof(SavedDayPhase), state.phase) || !Enum.IsDefined(typeof(SavedFocus), state.focus))
            throw new InvalidOperationException("Étape de journée inconnue.");
        var allowed = new HashSet<Candidate>(entry.asset.candidates);
        var remaining = ResolveUnique(state.remaining, catalog);
        var selected = ResolveUnique(state.selected, catalog);
        var before = ResolveUnique(state.employeesAtStart, catalog);
        var retained = ResolveUnique(save.campaign.retained, catalog);
        var rejected = ResolveUnique(save.campaign.rejected, catalog);
        if (retained.Overlaps(rejected) || remaining.Overlaps(selected) || selected.Count > entry.asset.interviewCapacity)
            throw new InvalidOperationException("Listes de candidats incohérentes.");
        foreach (var candidate in remaining) if (!allowed.Contains(candidate)) throw new InvalidOperationException("Candidat hors de la journée.");
        foreach (var candidate in selected) if (!allowed.Contains(candidate)) throw new InvalidOperationException("Candidat hors de la journée.");
        var current = catalog.Candidate(state.currentReview);
        if (current != null && (!allowed.Contains(current) || remaining.Contains(current) || selected.Contains(current)))
            throw new InvalidOperationException("Dossier courant incohérent.");
        if (state.decisions == null || save.campaign.reports == null) throw new InvalidOperationException("Historique absent.");
        foreach (var report in save.campaign.reports)
            if (report == null || !report.IsValidSnapshot) throw new InvalidOperationException("Un rapport de journée est incomplet.");
        var decided = new HashSet<Candidate>();
        foreach (var decision in state.decisions)
        {
            if (decision == null) throw new InvalidOperationException("Décision invalide.");
            var candidate = catalog.Candidate(decision.candidateId);
            if (candidate == null || !selected.Contains(candidate) || !decided.Add(candidate) ||
                (decision.retained ? !retained.Contains(candidate) : !rejected.Contains(candidate)))
                throw new InvalidOperationException("Historique des décisions incohérent.");
        }
        if (state.phase == SavedDayPhase.Interview)
        {
            if (current != null || state.interviewIndex < 0 || state.interviewIndex >= state.selected.Count || decided.Count != state.interviewIndex)
                throw new InvalidOperationException("Entretien courant incohérent.");
            var candidate = catalog.Candidate(state.selected[state.interviewIndex]);
            for (int i = 0; i < state.interviewIndex; i++)
                if (!decided.Contains(catalog.Candidate(state.selected[i]))) throw new InvalidOperationException("Ordre des entretiens incohérent.");
            InterviewSession.Restore(candidate.interviewDialogue, state.dialogueStep, state.question);
        }
        else if (state.phase == SavedDayPhase.Completed)
        {
            if (state.completedReport == null || !state.completedReport.IsValidSnapshot || current != null || decided.Count != selected.Count)
                throw new InvalidOperationException("Bilan de journée incomplet.");
        }
        else if (decided.Count != 0) throw new InvalidOperationException("Décisions présentes avant les entretiens.");
        if (state.phase != SavedDayPhase.Review && state.focus != SavedFocus.None)
            throw new InvalidOperationException("Focus invalide pour cette étape.");
        if ((state.focus == SavedFocus.CV || state.focus == SavedFocus.Letter) && current == null)
            throw new InvalidOperationException("Dossier focalisé absent.");
        catalog.Document(state.computerDocument);
        if (!Finite(state.playerPosition.x) || !Finite(state.playerPosition.y) || !Finite(state.playerPosition.z) ||
            !Valid(state.playerRotation) || !Valid(state.headRotation) || !Valid(state.originalHead) || !Valid(state.originalBody) ||
            !Finite(state.cvScroll.x) || !Finite(state.cvScroll.y) || !Finite(state.letterScroll.x) || !Finite(state.letterScroll.y))
            throw new InvalidOperationException("Position ou rotation invalide.");
    }

    private static HashSet<Candidate> ResolveUnique(List<string> ids, GameContentCatalog catalog)
    {
        if (ids == null) throw new InvalidOperationException("Liste de sauvegarde absente.");
        var result = new HashSet<Candidate>();
        foreach (var id in ids)
        {
            var candidate = catalog.Candidate(id);
            if (candidate == null || !result.Add(candidate)) throw new InvalidOperationException("Identifiant de candidat invalide ou dupliqué.");
        }
        return result;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Valid(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w) &&
        value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > .01f;
}
