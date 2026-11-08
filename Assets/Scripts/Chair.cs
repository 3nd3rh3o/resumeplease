using UnityEngine;



public class Chair : IInteractible
{
    private void Awake()
    {
        requireSeated = false;
    }
    [SerializeField]
    private PlayerController playerController;
    public void RestoreSeatedState(bool seated) => requireSeated = seated;
    public override void OnInteract()
    {
        if (!requireSeated)
            playerController.Sit(transform);
        if (requireSeated)
            playerController.Stand();
        requireSeated = !requireSeated;
    }
}
