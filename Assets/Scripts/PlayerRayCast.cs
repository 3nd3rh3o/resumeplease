using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class PlayerRayCast : MonoBehaviour
{
    [SerializeField]
    private float interactionRange = 5.0f;
    [SerializeField]
    private float eyesHeight = 1.6f;

    [SerializeField]
    private InputActionReference useAction;

    private IInteractible lastLookedAt;
    private IInteractible focusedDocument;
    public InputActionReference InteractionAction => useAction;
    [SerializeField]
    private PlayerController playerController;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameSession.InputBlocked) return;
        Physics.Raycast(transform.position + (Vector3.up * eyesHeight), transform.forward, out RaycastHit hitInfo, interactionRange, LayerMask.GetMask("Interactible"));
        var target = hitInfo.collider == null ? null : hitInfo.collider.GetComponent<IInteractible>();
        if (target != null && target.enabled && target.getRequireSeated() == playerController.IsSeated)
        {
            if (lastLookedAt != null && lastLookedAt != hitInfo.collider?.GetComponent<IInteractible>())
            {
                lastLookedAt.OnStopLook();
                lastLookedAt = null;
            }
            if (lastLookedAt == null)
            {
                lastLookedAt = hitInfo.collider?.GetComponent<IInteractible>();
                lastLookedAt.OnLook();
            }    
        }
        else
        {
            if (lastLookedAt != null)
            {
                lastLookedAt.OnStopLook();
                lastLookedAt = null;
            }
        }
        if (focusedDocument != null)
        {
            if (!IsDocumentFocused(focusedDocument))
            {
                if (playerController != null)
                    playerController.SetUIFocusMode(false);
                focusedDocument = null;
            }
            else
            {
                if (useAction.action.WasPressedThisFrame())
                {
                    focusedDocument.OnInteract();
                    if (!IsDocumentFocused(focusedDocument))
                    {
                        if (playerController != null)
                            playerController.SetUIFocusMode(false);
                        focusedDocument = null;
                    }
                }

                return;
            }
        }

        bool clickedTarget = (lastLookedAt is PersonalComputer || lastLookedAt is DayExitDoor) && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;
        if ((useAction.action.WasPressedThisFrame() || clickedTarget) && lastLookedAt != null)
        {
            lastLookedAt.OnInteract();
            if (IsDocumentFocused(lastLookedAt))
            {
                focusedDocument = lastLookedAt;
                if (playerController != null)
                    playerController.SetUIFocusMode(true);
            }
        }
        
    }

    private static bool IsDocumentFocused(IInteractible document)
    {
        if (document is CVReview cvReview)
            return cvReview.IsFocused;
        if (document is MotivationLettersReview letterReview)
            return letterReview.IsFocused;
        if (document is PersonalComputer computer)
            return computer.IsFocused;
        return false;
    }

    public SavedFocus CurrentFocus => !IsDocumentFocused(focusedDocument) ? SavedFocus.None : focusedDocument is CVReview ? SavedFocus.CV
        : focusedDocument is MotivationLettersReview ? SavedFocus.Letter
        : focusedDocument is PersonalComputer ? SavedFocus.Computer : SavedFocus.None;

    public void RestoreFocus(IInteractible document)
    {
        if (document == null) return;
        document.OnInteract();
        if (!IsDocumentFocused(document)) return;
        focusedDocument = document;
        playerController.SetUIFocusMode(true);
    }

    private void OnDisable()
    {
        if (lastLookedAt != null)
        {
            lastLookedAt.OnStopLook();
            lastLookedAt = null;
        }
        if (focusedDocument != null && playerController != null)
            playerController.SetUIFocusMode(false);

        focusedDocument = null;
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + (Vector3.up * eyesHeight), transform.position + (Vector3.up * eyesHeight) + transform.forward * interactionRange);
    }
#endif
}
