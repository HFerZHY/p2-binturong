using UnityEngine;

/// <summary>
/// Drives an NPC along an <see cref="NPCPath"/> asset (assign in the Inspector) or a path
/// supplied at runtime via <see cref="SetPath"/>.
///
/// Mirror of PlayerMovement:
///  • Uses Rigidbody2D.MovePosition for physics-consistent movement.
///  • Inertia lerp keeps acceleration / deceleration feeling natural.
///  • Sets Animator bool "isMoving" and flips localScale.x for facing direction —
///    identical to how PlayerMovement works so the same Animator Controller is reusable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NPCMovement : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────────────
    // Inspector fields
    // ──────────────────────────────────────────────────────────────────────────

    [Header("Path")]
    [Tooltip("Path asset created with the NPC Path editor. May also be set at runtime via SetPath().")]
    [SerializeField] private NPCPath path;

    [Tooltip("Index of the waypoint the NPC starts from.")]
    [SerializeField, Min(0)] private int startWaypointIndex = 0;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Same inertia semantics as PlayerMovement – 0 = instant, ~0.85 = floaty.")]
    [SerializeField, Range(0f, 1f)] private float inertia = 0.85f;

    [Tooltip("Distance (world units) at which a waypoint is considered 'reached'.")]
    [SerializeField] private float waypointReachRadius = 0.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // ──────────────────────────────────────────────────────────────────────────
    // Public state (readable by Animator, AI, etc.)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>True while the NPC is moving between waypoints.</summary>
    public bool IsMoving  { get; private set; }

    /// <summary>True while the NPC is pausing at a waypoint.</summary>
    public bool IsStopped { get; private set; }

    /// <summary>True when the NPC has finished a non-looping path.</summary>
    public bool PathComplete { get; private set; }

    /// <summary>Current waypoint index the NPC is walking toward.</summary>
    public int CurrentWaypointIndex => _targetIndex;

    // ──────────────────────────────────────────────────────────────────────────
    // Private state
    // ──────────────────────────────────────────────────────────────────────────

    private Rigidbody2D  _rb;
    private Vector2      _currentVelocity;

    private int   _targetIndex;       // waypoint we are currently heading to
    private float _stopTimer;         // countdown while pausing at a waypoint
    private bool  _facingRight = true;
    private bool  _pausing;           // true while serving a stop-duration

    // ──────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; // top-down: no gravity
    }

    private void Start()
    {
        if (path != null && path.IsValid)
            InitialisePath(path, startWaypointIndex);
    }

    private void FixedUpdate()
    {
        if (path == null || !path.IsValid || PathComplete) return;

        if (_pausing)
        {
            // Count down the stop timer; apply full deceleration while waiting.
            _currentVelocity = Vector2.Lerp(Vector2.zero, _currentVelocity, inertia);
            _rb.MovePosition(_rb.position + _currentVelocity * Time.fixedDeltaTime);

            IsMoving  = _currentVelocity.sqrMagnitude > 0.01f;
            IsStopped = !IsMoving;
            UpdateAnimator();

            _stopTimer -= Time.fixedDeltaTime;
            if (_stopTimer <= 0f)
                FinishPause();

            return;
        }

        // ── Normal movement toward the current target waypoint ──────────────
        Vector2 targetPos  = path.waypoints[_targetIndex].position;
        Vector2 toTarget   = targetPos - _rb.position;
        float   distSq     = toTarget.sqrMagnitude;

        Vector2 moveDir = distSq > 0.0001f ? toTarget.normalized : Vector2.zero;

        Vector2 targetVelocity = moveDir * moveSpeed;
        _currentVelocity = Vector2.Lerp(targetVelocity, _currentVelocity, inertia);

        _rb.MovePosition(_rb.position + _currentVelocity * Time.fixedDeltaTime);

        // ── Update public state & animator ───────────────────────────────────
        IsMoving  = _currentVelocity.sqrMagnitude > 0.01f;
        IsStopped = false;

        if (moveDir.x != 0f)
            _facingRight = moveDir.x > 0f;

        UpdateAnimator();

        // ── Waypoint reached? ─────────────────────────────────────────────────
        if (distSq <= waypointReachRadius * waypointReachRadius)
            OnWaypointReached();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Assign a new path (and optionally a starting waypoint index) at runtime.
    /// Safe to call at any time; immediately begins walking the new path.
    /// </summary>
    public void SetPath(NPCPath newPath, int startIndex = 0)
    {
        path = newPath;
        InitialisePath(path, startIndex);
    }

    /// <summary>Pause the NPC in place (e.g. during dialogue).</summary>
    public void Pause()  => _pausing = true;

    /// <summary>Resume walking after a manual <see cref="Pause"/>.</summary>
    public void Resume() => _pausing = false;

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void InitialisePath(NPCPath p, int startIndex)
    {
        if (p == null || !p.IsValid) return;

        _targetIndex  = Mathf.Clamp(startIndex, 0, p.waypoints.Length - 1);
        _currentVelocity = Vector2.zero;
        PathComplete  = false;
        _pausing      = false;
        _stopTimer    = 0f;

        // Snap to start position so the NPC doesn't sprint from wherever it was.
        _rb.position  = p.waypoints[
            Mathf.Clamp(startIndex - 1 < 0 ? 0 : startIndex - 1, 0, p.waypoints.Length - 1)
        ].position;
    }

    private void OnWaypointReached()
    {
        float stopDuration = path.waypoints[_targetIndex].stopDuration;

        // Advance to the next waypoint index.
        int nextIndex = _targetIndex + 1;

        if (nextIndex >= path.waypoints.Length)
        {
            if (path.loop)
            {
                nextIndex = 0;
            }
            else
            {
                // Non-looping path is complete after serving any stop at the last waypoint.
                if (stopDuration > 0f)
                    BeginPause(stopDuration, endPath: true);
                else
                    EndPath();
                return;
            }
        }

        if (stopDuration > 0f)
            BeginPause(stopDuration, endPath: false, nextIndex: nextIndex);
        else
            _targetIndex = nextIndex;
    }

    private void BeginPause(float duration, bool endPath, int nextIndex = -1)
    {
        _pausing   = true;
        _stopTimer = duration;

        // Store next index in a small closure via a flag we re-read in FinishPause.
        // We borrow _targetIndex temporarily: after the pause _targetIndex is advanced.
        if (!endPath && nextIndex >= 0)
            _pendingTargetIndex = nextIndex;

        _pendingEndPath = endPath;
    }

    private int  _pendingTargetIndex;
    private bool _pendingEndPath;

    private void FinishPause()
    {
        _pausing = false;

        if (_pendingEndPath)
            EndPath();
        else
            _targetIndex = _pendingTargetIndex;
    }

    private void EndPath()
    {
        PathComplete     = true;
        _currentVelocity = Vector2.zero;
        IsMoving         = false;
        IsStopped        = true;
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        animator?.SetBool("isMoving", IsMoving);

        // Flip sprite the same way PlayerMovement does.
        Vector3 scale = transform.localScale;
        scale.x = _facingRight ? 1f : -1f;
        transform.localScale = scale;
    }
}