using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputActionReference moveAction;
    [SerializeField]
    private InputActionReference lookAction;

    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float lookSpeed = 2f;
    [SerializeField]
    private float gravity = -9.81f;

    private CharacterController characterController;
    private float pitch = 0f;
    private float verticalSpeed = 0f;
    private bool uiFocusMode;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisibility;

    public bool IsSeated = false;

    public void Sit(Transform chairTransform)
    {
        IsSeated = true;
        characterController.enabled = false;
        transform.position = new Vector3(chairTransform.position.x, 0.8f, chairTransform.position.z);
    }

    public void Stand()
    {
        IsSeated = false;
        characterController.enabled = true;
        transform.position = new Vector3(transform.position.x, 1.08f, transform.position.z);
    }
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (characterController == null) // si on trouve pas de character controller je l ajoute
        {
            characterController = gameObject.AddComponent<CharacterController>();

            if (capsuleCollider != null && capsuleCollider.direction == 1)  // on prends la forme de la capsule
            {
                characterController.height = capsuleCollider.height;
                characterController.radius = capsuleCollider.radius;
                characterController.center = capsuleCollider.center;
            }
        }

        if (capsuleCollider != null)    // on annule l'aurre collider
            capsuleCollider.enabled = false;
    }

    private void Update()
    {
        if (uiFocusMode || GameSession.InputBlocked) return;
        float deltaTime = Time.deltaTime;

        //la je sépare le regard et le mouvement
        if (!uiFocusMode && lookAction != null)
        {
            Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
            UpdateLook(lookInput * lookSpeed * deltaTime); //fonction rajouté a la fin
        }
        if (!IsSeated)
        {
            Vector2 moveInput = Vector2.zero;
            if (moveAction != null)
            {
                moveInput = Vector2.ClampMagnitude( //clamp maginitude trop bien pour pas aller + vite en diagonale
                    moveAction.action.ReadValue<Vector2>(),
                    1f
                );
            }

            Vector3 horizontalMovement =
                transform.right * moveInput.x +
                transform.forward * moveInput.y;
            horizontalMovement.y = 0f;

            //calcul gravityé
            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed += gravity * deltaTime;
            }

            //determine mouvement pour l'image en corus
            Vector3 velocity = horizontalMovement * moveSpeed;
            velocity.y = verticalSpeed;

            characterController.Move(velocity * deltaTime);
        }
    }

    private void UpdateLook(Vector2 lookInput)
    {
        transform.Rotate(0f, lookInput.x, 0f);

        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.GetChild(0).localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SetUIFocusMode(bool enabled)
    {
        if (uiFocusMode == enabled)
            return;

        uiFocusMode = enabled;
        if (enabled)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisibility;
    }

    public void RestorePose(Vector3 position, Quaternion rotation, Quaternion headRotation, bool seated)
    {
        characterController.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        var head = transform.Find("Head");
        if (head != null) head.localRotation = headRotation;
        pitch = Mathf.DeltaAngle(0, headRotation.eulerAngles.x);
        verticalSpeed = 0;
        IsSeated = seated;
        characterController.enabled = !seated;
    }

    public void SynchronizeLookRotation()
    {
        var head = transform.Find("Head");
        if (head != null) pitch = Mathf.DeltaAngle(0, head.localEulerAngles.x);
    }

    private void OnDisable()
    {
        if (uiFocusMode)
            SetUIFocusMode(false);
    }
}
