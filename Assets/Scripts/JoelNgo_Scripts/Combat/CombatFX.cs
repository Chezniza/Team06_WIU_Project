using UnityEngine;

public class CombatFX : MonoBehaviour
{
    public static CombatFX Instance;

    public GameObject blockFX;
    public GameObject parryFX;
    public GameObject hitFX;

    private void Awake()
    {
        Instance = this;
    }

    public static void particleAtHit(GameObject target, BoxCollider collider, GameObject effect)
    {
        Collider targetCollider = target.GetComponent<Collider>();

        Vector3 hitPoint = targetCollider != null ?
            targetCollider.ClosestPoint(collider.transform.position) : target.transform.position;

        Vector3 dir = (target.transform.position - hitPoint).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);

        Instantiate(effect, hitPoint, rot);
    }
}