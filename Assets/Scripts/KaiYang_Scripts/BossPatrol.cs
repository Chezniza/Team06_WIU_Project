using UnityEngine;

// ============================================================
//  BossPatrol.cs
//  Add this component to the boss GameObject alongside BossAI.
//
//  HOW TO SET UP WAYPOINTS IN UNITY:
//    1. Create empty GameObjects in the scene at the patrol positions
//    2. Drag them into the "Waypoints" array in the Inspector
//    3. The boss will walk between them in order while the player
//       is outside detection range
// ============================================================

public class BossPatrol : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float       waypointReachDistance = 0.5f;  // how close counts as "arrived"

    [Header("Patrol Behaviour")]
    [SerializeField] private float waitAtWaypoint = 1f;   // seconds to pause at each waypoint
    [SerializeField] private bool  loopPatrol     = true;  // loop back to first waypoint or ping-pong

    // ── State ────────────────────────────────────────────────
    private int   currentWaypointIndex = 0;
    private int   direction            = 1;   // 1 = forward, -1 = reverse (ping-pong)
    private float waitTimer            = 0f;
    private bool  isWaiting            = false;

    public bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    // ── Called by BossAI every frame when player is out of detection range ──
    public void Patrol(CharacterController controller, Animator animator,
                       float moveSpeed, float rotateSpeed)
    {
        if (!HasWaypoints) return;

        // Wait at waypoint
        if (isWaiting)
        {
            animator.SetBool("IsWalking", false);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                isWaiting = false;
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) { AdvanceWaypoint(); return; }

        Vector3 toTarget = target.position - controller.transform.position;
        toTarget.y = 0;
        float dist = toTarget.magnitude;

        // Arrived — pause then move to next
        if (dist <= waypointReachDistance)
        {
            isWaiting = true;
            waitTimer = waitAtWaypoint;
            AdvanceWaypoint();
            return;
        }

        // Walk toward waypoint
        animator.SetBool("IsWalking", true);

        Vector3 dir = toTarget.normalized;

        // Rotate toward waypoint
        Quaternion targetRot = Quaternion.LookRotation(dir);
        controller.transform.rotation = Quaternion.RotateTowards(
            controller.transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        controller.Move(dir * moveSpeed * Time.deltaTime);
    }

    private void AdvanceWaypoint()
    {
        if (waypoints.Length <= 1) return;

        if (loopPatrol)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
        else
        {
            // Ping-pong
            currentWaypointIndex += direction;
            if (currentWaypointIndex >= waypoints.Length - 1 ||
                currentWaypointIndex <= 0)
                direction = -direction;
            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
        }
    }

    // Draw waypoint path in the Scene view so you can see the patrol route
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            int next = loopPatrol
                ? (i + 1) % waypoints.Length
                : Mathf.Min(i + 1, waypoints.Length - 1);

            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}
