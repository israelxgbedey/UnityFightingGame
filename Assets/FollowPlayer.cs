using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Tooltip("The transform to follow. If null, will try to find object with tag 'Player' on Start.")]
    public Transform target;

    [Tooltip("Offset from the target (camera usually uses z = -10).")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Tooltip("Smoothing time for camera movement.")]
    public float smoothTime = 0.12f;

    // Optional axis locks
    public bool lockX = false;
    public bool lockY = false;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) target = go.transform;
        }
    }

    // Use LateUpdate so camera moves after player has moved this frame
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // apply axis locks
        Vector3 current = transform.position;
        if (lockX) desiredPosition.x = current.x;
        if (lockY) desiredPosition.y = current.y;

        transform.position = Vector3.SmoothDamp(current, desiredPosition, ref velocity, smoothTime);
    }
}
