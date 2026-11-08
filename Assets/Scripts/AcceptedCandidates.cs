using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Outline))]
public class AcceptedCandidates : IInteractible
{
    [SerializeField] private CVReview cvReview;
    [SerializeField] private MotivationLettersReview motivationLettersReview;
    private int maxCapacity;
    [SerializeField] private List<Candidate> acceptedCandidates = new List<Candidate>();

    public IReadOnlyList<Candidate> Candidates => acceptedCandidates;
    public int MaxCapacity => maxCapacity;
    public bool IsFull => acceptedCandidates.Count >= maxCapacity;
    public event System.Action Changed;

    public void ConfigureCapacity(int capacity)
    {
        maxCapacity = Mathf.Max(0, capacity);
        Changed?.Invoke();
    }

    public void RestoreSelection(IEnumerable<Candidate> candidates)
    {
        acceptedCandidates = new List<Candidate>(candidates);
        Changed?.Invoke();
    }

    private void Awake()
    {
        requireSeated = true;
    }

    public override void OnInteract()
    {
        if (IsFull)
        {
            Debug.Log("La liste des candidats acceptés est pleine.", this);
            return;
        }

        if (cvReview == null || motivationLettersReview == null)
        {
            Debug.LogError("Assigne les deux reviews aux candidats acceptés.", this);
            return;
        }

        var candidate = cvReview.CurrentCandidate != null
            ? cvReview.CurrentCandidate
            : motivationLettersReview.CurrentCandidate;
        if (candidate == null)
            return;

        acceptedCandidates.Add(candidate);
        cvReview.Clear();
        motivationLettersReview.Clear();
        Changed?.Invoke();
    }
}
