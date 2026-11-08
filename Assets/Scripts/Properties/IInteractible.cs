using UnityEngine;

/// <summary>
/// Interface for objects that can be interacted with.
/// </summary>
public abstract class IInteractible : MonoBehaviour
{
    protected bool requireSeated = false;
    public bool getRequireSeated()
    {
        return requireSeated;
    }
    protected bool lookedAt = false;
    public virtual void OnLook()
    {
        lookedAt = true;
        this.gameObject.GetComponent<Outline>().enabled = true;
    }

    public virtual void OnStopLook()
    {
        lookedAt = false;
        this.gameObject.GetComponent<Outline>().enabled = false;
    }
    public abstract void OnInteract();
}