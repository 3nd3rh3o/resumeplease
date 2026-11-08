using System.Collections.Generic;
using UnityEngine;

public class InterviewResults : MonoBehaviour
{
    private static InterviewResults instance;
    public static InterviewResults Instance
    {
        get
        {
            if (instance == null)
                new GameObject("Interview Results").AddComponent<InterviewResults>();
            return instance;
        }
    }

    [SerializeField] private List<Candidate> retainedCandidates = new List<Candidate>();
    [SerializeField] private List<Candidate> rejectedCandidates = new List<Candidate>();
    [SerializeField] private List<DayReport> dayReports = new List<DayReport>();
    public IReadOnlyList<Candidate> RetainedCandidates => retainedCandidates;
    public IReadOnlyList<Candidate> RejectedCandidates => rejectedCandidates;
    public CompanyMetrics Metrics => new CompanyMetrics(retainedCandidates);
    public IReadOnlyList<DayReport> DayReports => dayReports;
    public DayReport LastReport => dayReports.Count == 0 ? null : dayReports[dayReports.Count - 1];
    public event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Record(Candidate candidate, bool retained)
    {
        if (candidate == null)
            return;
        retainedCandidates.Remove(candidate);
        rejectedCandidates.Remove(candidate);
        (retained ? retainedCandidates : rejectedCandidates).Add(candidate);
        Changed?.Invoke();
    }

    public void ClearResults()
    {
        retainedCandidates.Clear();
        rejectedCandidates.Clear();
        dayReports.Clear();
        Changed?.Invoke();
    }

    public void StoreReport(DayReport report)
    {
        if (report == null || dayReports.Contains(report)) return;
        dayReports.Add(report);
        Changed?.Invoke();
    }

    public void Restore(CampaignSnapshot state, GameContentCatalog catalog)
    {
        retainedCandidates = catalog.Resolve(state.retained);
        rejectedCandidates = catalog.Resolve(state.rejected);
        dayReports = new List<DayReport>(state.reports);
        Changed?.Invoke();
    }
}
