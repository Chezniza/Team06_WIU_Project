using System.Collections;
using UnityEngine;

// ============================================================
//  Projectiles.cs  —  SteamForge / Team 06
//  Attach to your projectile prefab.
//  Requires: Rigidbody, Collider on prefab.
// ============================================================

public class Projectiles : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int baseDamage = 15; // default — overridden by EnemyAI.Init()
    [SerializeField] private LayerMask m_LayerMask;
    [SerializeField] private LayerMask m_Ignore; // prevent destroy on hit for certain object

    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;

    [Header("Impact")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private float impactVFXDuration = 1.5f;

    [Header("Homing")]
    [SerializeField] private bool  isHoming      = false;
    [SerializeField] private float homingStrength = 3f;

    [Header("Hit Detection")]
    [SerializeField] private float hitRadius = 0.3f;   // tune to match your projectile size
    [SerializeField] private LayerMask m_LayerMask;        // set to your Player layer in Inspector

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private int damage;
    private Vector3 direction;
    private Rigidbody rb;
    private Transform target;
    private bool      isReady = false;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        damage = baseDamage;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!isReady || hasHit) return;

        if (isHoming && target != null)
        {
            Vector3 toPlayer = (target.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, toPlayer, homingStrength * Time.fixedDeltaTime);
            direction.Normalize();
        }

        rb.linearVelocity = direction * speed;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

       
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius, m_LayerMask);

       
    }
    public void EnableHoming()
    {

        isHoming = true;
    }
    // ─────────────────────────────────────────────
    // INIT — called by EnemyAI right after Instantiate
    // ─────────────────────────────────────────────

    public void Init(int dmg, Vector3 dir, Transform target = null)
    {
        damage    = dmg;              // overrides baseDamage with EnemyAI's rangedDamage
        direction = dir.normalized;
        isReady   = true;
    }

  
    private void OnTriggerEnter(Collider other)
    {
        if (IsInLayerMask(other.transform, m_LayerMask))
        {
            if (other.TryGetComponent<AttackHandler>(out AttackHandler targetAttackHandler))
            {
                // FIX 2: Convert float damage to int to match TakeDamage(int)
              //  int dmgInt = Mathf.RoundToInt(damage);
                Debug.Log($"[Projectile] Hit gameObject for {damage})");
                damageable.TakeDamage(damage);
                SpawnImpactVFX();
                hasHit = true;
                Destroy(gameObject);
                return;
            }
            else
            {
                // This warning will appear in Console if the tag or component is wrong
                Debug.LogWarning($"[Projectile] Hit '{other.name}' with layer but NO Damageable found on it or any parent!");
            }

            SpawnImpactVFX();
            Destroy(gameObject);
            return;
        }

        // Destroy on environment — ignore other enemies, boss, and other projectiles
        if (!IsInLayerMask(other.transform, m_Ignore))
        {
            SpawnImpactVFX();
            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private void SpawnImpactVFX()
    {
        if (impactVFXPrefab == null) return;
        GameObject vfx = Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        Destroy(vfx, impactVFXDuration);
    }

    public void SetDamage(int dmg) => damage = dmg;
    public float GetDamage() => damage;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }

    public bool IsInLayerMask(Transform other, LayerMask mask)
    {
        return (mask.value & (1 << other.gameObject.layer)) != 0;
    }
}
