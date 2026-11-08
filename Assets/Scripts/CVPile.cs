using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class CVPile : IInteractible
{
    
    
    [SerializeField]
    private CVReview cvReview;
    [SerializeField]
    private MotivationLettersReview motivationLettersReview;
    private List<Candidate> receivedCVs;
    public bool IsEmpty => receivedCVs != null && receivedCVs.Count == 0;
    public int RemainingCount => receivedCVs == null ? 0 : receivedCVs.Count;
    public IReadOnlyList<Candidate> RemainingCandidates => receivedCVs;
    public event System.Action Changed;

    public void ReceiveCVs(List<Candidate> cvs)
    {
        receivedCVs = new List<Candidate>(cvs);
    }
 
    public override void OnInteract()
    {
        if (lookedAt)
        {
            if (receivedCVs != null && receivedCVs.Count > 0 && !cvReview.HasCV())
            {
                Candidate candidate = ListUtils.PopFirst(ref receivedCVs);
                cvReview.ReceiveCV(ref candidate);
                if (motivationLettersReview != null)
                    motivationLettersReview.ReceiveMotivationLetter(ref candidate);
                Changed?.Invoke();
            }
            else
            {
                if (receivedCVs != null && receivedCVs.Count == 0)
                    Debug.Log("No more CVs to review.");
                else if (cvReview.HasCV())
                    Debug.Log("CV review already has a CV.");
                else
                    Debug.Log("CV list is not initialized.");
            }
        }
    }
    

    private void Awake()
    {
        requireSeated = true;
    }
}
