using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float inertia = 0.85f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraSmoothing = 0f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Exposed for the Animator
    public bool IsMoving { get; private set; }
    public bool FacingRight { get; private set; } = true;

    private InputAction _moveAction;
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;

    private void Awake()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()  { _moveAction.Enable(); }
    private void OnDisable() { _moveAction.Disable(); }

    private void FixedUpdate()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();

        moveInput.Normalize();

        Vector2 targetVelocity = moveInput * moveSpeed;
        _currentVelocity = Vector2.Lerp(targetVelocity, _currentVelocity, inertia);

        _rb.MovePosition(_rb.position + _currentVelocity * Time.fixedDeltaTime);

        // Update animator properties
        IsMoving = _currentVelocity.sqrMagnitude > 0.01f;
        if (moveInput.x != 0f)
            FacingRight = moveInput.x > 0f;
        animator?.SetBool("isMoving", IsMoving);
        // animator?.SetBool("facingRight", FacingRight);
        Vector3 localScale = transform.localScale;
        localScale.x = FacingRight ? 1f : -1f;
        transform.localScale = localScale;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, cameraTransform.position.z);

        if (cameraSmoothing <= 0f)
            cameraTransform.position = targetPos;
        else
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPos, cameraSmoothing * Time.deltaTime);
    }
}