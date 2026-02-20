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

    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;

    [Header("Impact")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private float impactVFXDuration = 1.5f;

    [Header("Homing (Boss Phase 2)")]
    [SerializeField] private bool isHoming = false;
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
    private Transform player;
    private bool isReady = false;
    private bool hasHit = false;  

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

        // ── Homing steering ──────────────────────────────────
        if (isHoming && player != null)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, toPlayer, homingStrength * Time.fixedDeltaTime);
            direction.Normalize();
        }

        rb.linearVelocity = direction * speed;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

       
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius, m_LayerMask);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            GameObject target = hitColliders[i].gameObject;

            
            if (target.TryGetComponent<AttackHandler>(out AttackHandler targetAttackHandler))
            {
                if (targetAttackHandler.IsBlocking())
                {
                    Debug.Log("[Projectile] Blocked by player.");
                    SpawnImpactVFX();
                    hasHit = true;
                    Destroy(gameObject);
                    return;
                }
            }

            
            if (target.TryGetComponent<Damageable>(out Damageable damageable))
            {
                Debug.Log($"[Projectile] Hit player for {damage}");
                damageable.TakeDamage(damage);
                SpawnImpactVFX();
                hasHit = true;
                Destroy(gameObject);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────
    // INIT — called by EnemyAI right after Instantiate
    // ─────────────────────────────────────────────

    public void Init(int dmg, Vector3 dir)
    {
        damage = dmg;
        direction = dir.normalized;
        isReady = true;

        if (isHoming)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    // ─────────────────────────────────────────────
    // ENVIRONMENT COLLISION — destroy on walls/floor
    // (kept as trigger for non-player geometry)
    // ─────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Ignore enemies, boss, and other projectiles
        if (other.CompareTag("Enemy") || other.CompareTag("Boss") || other.CompareTag("Projectile"))
            return;

        // Ignore the player layer — handled by OverlapSphere above
        if (other.CompareTag("Player"))
            return;

        // Hit environment
        SpawnImpactVFX();
        hasHit = true;
        Destroy(gameObject);
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
}