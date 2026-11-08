using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider))]
public class PersonalComputer : IInteractible
{
    [SerializeField] private PanelRenderer screenPanelRenderer;
    [SerializeField, Min(0.01f)] private float focusDistance = 0.8f;

    private Camera focusCamera;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    public bool IsFocused { get; private set; }

    private void Awake()
    {
        requireSeated = true;
        if (screenPanelRenderer == null)
            screenPanelRenderer = GetComponentInChildren<PanelRenderer>();
    }

    public override void OnInteract()
    {
        if (IsFocused)
        {
            RestoreScreen();
            return;
        }

        if (screenPanelRenderer == null)
        {
            Debug.LogError("Assigne le PanelRenderer de l'écran du PC.", this);
            return;
        }

        focusCamera = Camera.main;
        if (focusCamera == null)
        {
            Debug.LogError("Impossible de focaliser l'écran : aucune caméra MainCamera.", this);
            return;
        }

        var screenTransform = screenPanelRenderer.transform;
        originalLocalPosition = screenTransform.localPosition;
        originalLocalRotation = screenTransform.localRotation;
        IsFocused = true;
        PositionScreen();
    }

    private void LateUpdate()
    {
        if (!IsFocused)
            return;

        if (focusCamera == null || screenPanelRenderer == null)
        {
            RestoreScreen();
            return;
        }

        PositionScreen();
    }

    private void PositionScreen()
    {
        var screenTransform = screenPanelRenderer.transform;
        screenTransform.position = focusCamera.transform.position + focusCamera.transform.forward * focusDistance;
        screenTransform.rotation = focusCamera.transform.rotation;
    }

    private void RestoreScreen()
    {
        if (screenPanelRenderer != null)
        {
            var screenTransform = screenPanelRenderer.transform;
            screenTransform.localPosition = originalLocalPosition;
            screenTransform.localRotation = originalLocalRotation;
        }

        focusCamera = null;
        IsFocused = false;
    }

    private void OnDisable()
    {
        if (IsFocused)
            RestoreScreen();
    }
}
