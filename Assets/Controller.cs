using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Controller2D : MonoBehaviour
{
    [Tooltip("Horizontal speed")]
    public float speed = 6f;

    [Tooltip("Jump velocity")]
    public float jumpForce = 12f;

    [Tooltip("Gravity (negative)")]
    public float gravity = -30f;

    [Tooltip("Transform placed at the player's feet for ground checks")]
    public Transform groundCheck;

    [Tooltip("Radius of ground check circle")]
    public float groundCheckRadius = 0.1f;

    [Tooltip("Layer(s) considered ground/obstacles")]
    public LayerMask groundLayer;

    private Collider2D col;
    private Vector2 velocity;
    private bool isGrounded;
    private ContactFilter2D movementFilter;
    private RaycastHit2D[] castHits = new RaycastHit2D[8];
    private const float skinWidth = 0.02f;

    void Start()
    {
        col = GetComponent<Collider2D>();
        movementFilter = new ContactFilter2D();
        movementFilter.SetLayerMask(groundLayer);
        movementFilter.useLayerMask = true;
        movementFilter.useTriggers = false;
    }

    void Update()
    {
        // Ground check (safe if groundCheck not assigned)
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
        else
            isGrounded = false;

        // Jump: W or Space or configured "Jump" button
        if (isGrounded && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W)))
        {
            velocity.y = jumpForce;
        }

        // Horizontal input (WASD or arrows)
        float x = Input.GetAxisRaw("Horizontal");
        if (Mathf.Approximately(x, 0f))
        {
            if (Input.GetKey(KeyCode.A)) x = -1f;
            else if (Input.GetKey(KeyCode.D)) x = 1f;
        }

        velocity.x = x * speed;
        velocity.y += gravity * Time.deltaTime;
    }

    void FixedUpdate()
    {
        Vector2 delta = velocity * Time.fixedDeltaTime;
        Move(delta);
    }

    // Move using Collider2D.Cast to detect collisions (horizontal then vertical)
    void Move(Vector2 delta)
    {
        // Horizontal
        Vector2 move = new Vector2(delta.x, 0f);
        if (move.sqrMagnitude > Mathf.Epsilon)
        {
            int count = col.Cast(move.normalized, movementFilter, castHits, Mathf.Abs(move.x) + skinWidth);
            if (count == 0)
            {
                transform.position += (Vector3)move;
            }
            else
            {
                float minDist = castHits[0].distance;
                for (int i = 1; i < count; i++) minDist = Mathf.Min(minDist, castHits[i].distance);
                transform.position += (Vector3)(move.normalized * Mathf.Max(0f, minDist - skinWidth));
                velocity.x = 0f;
            }
        }

        // Vertical
        move = new Vector2(0f, delta.y);
        if (move.sqrMagnitude > Mathf.Epsilon)
        {
            int count = col.Cast(move.normalized, movementFilter, castHits, Mathf.Abs(move.y) + skinWidth);
            if (count == 0)
            {
                transform.position += (Vector3)move;
            }
            else
            {
                float minDist = castHits[0].distance;
                int hitIndex = 0;
                for (int i = 1; i < count; i++)
                    if (castHits[i].distance < minDist) { minDist = castHits[i].distance; hitIndex = i; }

                Vector2 hitNormal = castHits[hitIndex].normal;
                transform.position += (Vector3)(move.normalized * Mathf.Max(0f, minDist - skinWidth));

                // landed on ground if collision normal points up
                if (move.y < 0f)
                    isGrounded = hitNormal.y > 0.5f;

                velocity.y = 0f;
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
