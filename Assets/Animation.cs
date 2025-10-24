using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple state-only animation switcher: idle, walk, run, jump, attack, hit (Get Hit).
/// Assign the GameObjects you want shown for each state in the inspector.
/// Call TriggerHit() from gameplay code to show the "Get Hit" object for a short time.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [Header("Ground check (optional, used to detect jump)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("State objects (assign one GameObject per state)")]
    public GameObject idle;
    public GameObject walk;
    public GameObject run;
    public GameObject jump;
    public GameObject attack;
    public GameObject getHit;

    [Header("Settings")]
    [Tooltip("Movement magnitude (0..1) above which we consider 'moving'")]
    public float moveThreshold = 0.1f;
    [Tooltip("If LeftShift held (or movement magnitude > runThreshold) treat as run")]
    public float runThreshold = 0.9f;
    public float attackDuration = 1f;
    public float hitDuration = 0.5f;

    [Header("Jump animation")]
    [Tooltip("How long to show the jump state after the player presses jump (seconds)")]
    public float jumpDisplayTime = 0.25f;

    // internal
    private List<GameObject> allObjects;
    private float attackTimer = 0f;
    private float hitTimer = 0f;
    private float jumpTimer = 0f;

    // facing / flip
    private bool facingRight = true;
    private bool prevFacingRight = true;

    // track vertical motion without requiring a Rigidbody2D
    private float prevY;
    private const float ascendThreshold = 0.05f; // units/sec threshold to consider "ascending"

    void Start()
    {
        allObjects = new List<GameObject> { idle, walk, run, jump, attack, getHit };
        allObjects.RemoveAll(g => g == null);

        prevFacingRight = facingRight;
        ApplyFlip(facingRight);

        prevY = transform.position.y;
    }

    void Update()
    {
        // timers
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (hitTimer > 0f) hitTimer -= Time.deltaTime;
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;

        // ground check (still available for other logic but NOT used to force jump state)
        bool isGrounded = true;
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;

        // input
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // fallback keys if axis not configured
        if (Mathf.Approximately(x, 0f))
        {
            if (Input.GetKey(KeyCode.A)) x = -1f;
            else if (Input.GetKey(KeyCode.D)) x = 1f;
        }
        if (Mathf.Approximately(y, 0f))
        {
            if (Input.GetKey(KeyCode.S)) y = -1f;
            else if (Input.GetKey(KeyCode.W)) y = 1f;
        }

        // update facing based on horizontal input (remember last non-zero horizontal)
        if (!Mathf.Approximately(x, 0f))
        {
            facingRight = x > 0f;
        }

        // apply flip only when changed
        if (facingRight != prevFacingRight)
        {
            ApplyFlip(facingRight);
            prevFacingRight = facingRight;
        }

        // Only consider A/D (horizontal input) for walk/run states:
        bool horizontalKey = !Mathf.Approximately(x, 0f); // true when pressing A or D (or axis non-zero)
        bool moving = horizontalKey; // walk object should play when A or D pressed
        // Run only when A/D is pressed AND LeftShift is held (the "switch" paired with A/D)
        bool running = horizontalKey && Input.GetKey(KeyCode.LeftShift);

        // attack input: Fire1, J, K or E key
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.E))
        {
            attackTimer = GetAttackDuration();
        }

        // jump animation trigger when player presses Space (or configured "Jump" button)
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpTimer = jumpDisplayTime;
        }

        // compute vertical speed from transform (works without Rigidbody2D)
        float verticalSpeed = 0f;
        if (Time.deltaTime > 0f)
            verticalSpeed = (transform.position.y - prevY) / Time.deltaTime;
        prevY = transform.position.y;

        // priority: hit > attack > jump (pressed or ascending) > run/walk > idle
        GameObject toShow = idle;

        if (hitTimer > 0f)
        {
            toShow = getHit ?? idle;
        }
        else if (attackTimer > 0f)
        {
            toShow = attack ?? idle;
        }
        else if (jumpTimer > 0f) // show jump immediately after pressing jump/space
        {
            toShow = jump ?? idle;
        }
        else if (verticalSpeed > ascendThreshold) // only show jump when ascending, not when falling
        {
            toShow = jump ?? idle;
        }
        else if (moving)
        {
            toShow = running ? (run ?? walk ?? idle) : (walk ?? idle);
        }
        else
        {
            toShow = idle;
        }

        ActivateOnly(toShow);
    }

    // Call this from other scripts when the player gets hit
    public void TriggerHit(float duration = -1f)
    {
        hitTimer = (duration > 0f) ? duration : hitDuration;
    }

    // Optional: allow external code to trigger the attack animation
    public void TriggerAttack(float duration = -1f)
    {
        attackTimer = (duration > 0f) ? duration : GetAttackDuration();
    }

    // Auto-detect the attack animation length; fallback to inspector attackDuration
    private float GetAttackDuration()
    {
        if (attack != null)
        {
            var animator = attack.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                {
                    // prefer clip with "attack" in the name, otherwise return the longest clip
                    AnimationClip best = null;
                    foreach (var c in clips)
                    {
                        if (c == null) continue;
                        if (best == null || c.length > best.length) best = c;
                        if (c.name.ToLower().Contains("attack")) return c.length;
                    }
                    if (best != null) return best.length;
                }
            }

            var legacy = attack.GetComponent<Animation>();
            if (legacy != null)
            {
                foreach (AnimationState st in legacy)
                {
                    if (st.clip != null)
                    {
                        if (st.clip.name.ToLower().Contains("attack")) return st.clip.length;
                        return st.clip.length;
                    }
                }
            }
        }

        return attackDuration;
    }

    void ActivateOnly(GameObject obj)
    {
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            go.SetActive(go == obj);
        }
    }

    // Flip sprite renderers on states to face left/right.
    void ApplyFlip(bool facingRight)
    {
        bool flip = !facingRight;
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                sr.flipX = flip;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}