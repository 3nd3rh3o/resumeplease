using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Outline))]
public sealed class DayExitDoor : IInteractible
{
    [SerializeField] private ScenarioController scenario;
    private void Awake() => requireSeated = false;

    public void Configure(ScenarioController controller)
    {
        scenario = controller;
        SetAvailable(false);
    }

    public void SetAvailable(bool available)
    {
        if (!available) OnStopLook();
        int layer = available ? LayerMask.NameToLayer("Interactible") : 0;
        foreach (var child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    public override void OnLook()
    {
        if (scenario != null && scenario.CanLeaveDay) base.OnLook();
    }

    public override void OnInteract()
    {
        if (scenario != null) scenario.TryLeaveDay();
    }
}
