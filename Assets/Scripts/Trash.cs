using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Outline))]
public class Trash : IInteractible
{
    public event System.Action Changed;
    [SerializeField] private CVReview cvReview;
    [SerializeField] private MotivationLettersReview motivationLettersReview;

    private void Awake()
    {
        requireSeated = true;
    }

    public override void OnInteract()
    {
        if (cvReview == null || motivationLettersReview == null)
        {
            Debug.LogError("Assigne les deux reviews à la corbeille.", this);
            return;
        }

        cvReview.Clear();
        motivationLettersReview.Clear();
        Changed?.Invoke();
    }
}
