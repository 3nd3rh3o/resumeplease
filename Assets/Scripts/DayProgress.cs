using System;
using System.Collections.Generic;

public sealed class DayProgress
{
    private readonly List<Candidate> hiredToday = new List<Candidate>();
    private readonly List<Candidate> rejectedToday = new List<Candidate>();
    private readonly Dictionary<Candidate, bool> decidedCandidates = new Dictionary<Candidate, bool>();
    private readonly HashSet<Candidate> employeesAtStart;
    public DayDefinition Definition { get; }
    public int InterviewsCompleted => decidedCandidates.Count;
    public CompanyMetrics Today => new CompanyMetrics(hiredToday);
    public IReadOnlyList<Candidate> HiredToday => hiredToday;
    public IReadOnlyList<Candidate> RejectedToday => rejectedToday;
    public IReadOnlyDictionary<Candidate, bool> Decisions => decidedCandidates;
    public IEnumerable<Candidate> EmployeesAtStart => employeesAtStart;
    public event Action Changed;

    public DayProgress(DayDefinition definition, IEnumerable<Candidate> existingEmployees)
    {
        Definition = definition;
        employeesAtStart = existingEmployees == null ? new HashSet<Candidate>() : new HashSet<Candidate>(existingEmployees);
    }

    public void Record(Candidate candidate, bool retained)
    {
        if (candidate == null)
            return;
        decidedCandidates[candidate] = retained;
        hiredToday.Remove(candidate);
        rejectedToday.Remove(candidate);
        if (retained && !employeesAtStart.Contains(candidate)) hiredToday.Add(candidate);
        if (!retained) rejectedToday.Add(candidate);
        Changed?.Invoke();
    }

    public bool MeetsObjectives(CompanyMetrics company)
    {
        if (Definition == null || company == null)
            return false;
        foreach (var result in DayEvaluation.Evaluate(Definition, Today, company))
            if (!result.Met) return false;
        return true;
    }
}
