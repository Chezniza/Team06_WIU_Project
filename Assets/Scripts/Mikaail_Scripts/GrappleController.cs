using UnityEngine;
using System.Collections;

public class GrappleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private MoverSystem mover;
    [SerializeField] private InputHandler inputHandler;

    [Header("Settings")]
    [SerializeField] private float maxDistance = 30f;
    [SerializeField] private float grappleDuration = 0.7f;
    [SerializeField] private float hideUIDistance = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ropeLaunchClip;
    [SerializeField] private AudioClip pullUpClip;

    private GrapplePoint currentTarget;
    private bool isGrappling;

    public bool IsGrappling => isGrappling;
    public bool HasTarget => currentTarget != null && !isGrappling;

    [SerializeField] private float grappleCooldown = 1.2f;
    private bool onCooldown;

    void Update()
    {
        if (isGrappling || onCooldown) return;

        DetectTarget();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(GrappleToTarget());
        }

        Debug.Log("HasTarget: " + HasTarget);
    }

    private void DetectTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, grappleLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.name);

            GrapplePoint gp = hit.collider.GetComponent<GrapplePoint>();

            if (gp != null)
            {
                Debug.Log("GrapplePoint detected: " + gp.name);

                float dist = Vector3.Distance(transform.position, gp.destination.position);
                Debug.Log("Distance to target: " + dist);

                if (dist > hideUIDistance)
                {
                    currentTarget = gp;
                    Debug.Log("Target LOCKED: " + gp.name);
                }
                else
                {
                    currentTarget = null;
                    Debug.Log("Too close to grapple point, hiding target.");
                }

                return;
            }
            else
            {
                Debug.Log("Hit object is NOT a GrapplePoint.");
            }
        }

        if (currentTarget != null)
            Debug.Log("Lost target.");

        currentTarget = null;
    }

    private IEnumerator GrappleToTarget()
    {
        isGrappling = true;
        inputHandler.LockControls();

        if (audioSource && ropeLaunchClip)
            audioSource.PlayOneShot(ropeLaunchClip);

        yield return new WaitForSeconds(0.1f);

        if (audioSource && pullUpClip)
            audioSource.PlayOneShot(pullUpClip);

        yield return StartCoroutine(
            mover.MoveToTarget(currentTarget.destination.position, grappleDuration)
        );

        inputHandler.UnlockControls();
        isGrappling = false;

        // START COOLDOWN
        onCooldown = true;
        yield return new WaitForSeconds(grappleCooldown);
        onCooldown = false;
    }
}