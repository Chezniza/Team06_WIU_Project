using System.Collections;
using UnityEngine;

// ============================================================
//  Projectiles.cs  —  SteamForge / Team 06
//  Attach to your projectile prefab.
//  Requires: Rigidbody, Collider (set as Trigger) on prefab.
// ============================================================

public class Projectiles : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float baseDamage = 15f; // default — overridden by EnemyAI.Init()

    [Header("Movement")]
    [SerializeField] private float speed    = 12f;
    [SerializeField] private float lifetime = 5f;

    [Header("Impact")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private float      impactVFXDuration = 1.5f;

    [Header("Homing (Boss Phase 2)")]
    [SerializeField] private bool  isHoming      = false;
    [SerializeField] private float homingStrength = 3f;

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private float     damage;
    private Vector3   direction;
    private Rigidbody rb;
    private Transform player;
    private bool      isReady = false;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity    = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Use baseDamage as default — Init() overrides this when EnemyAI fires it
        damage = baseDamage;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!isReady) return;

        if (isHoming && player != null)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, toPlayer, homingStrength * Time.fixedDeltaTime);
            direction.Normalize();
        }

        rb.linearVelocity = direction * speed;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    // ─────────────────────────────────────────────
    // INIT — called by EnemyAI right after Instantiate
    // ─────────────────────────────────────────────

    public void Init(float dmg, Vector3 dir)
    {
        damage    = dmg;              // overrides baseDamage with EnemyAI's rangedDamage
        direction = dir.normalized;
        isReady   = true;

        if (isHoming)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    // ─────────────────────────────────────────────
    // COLLISION
    // ─────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // FIX 1: GetComponentInParent instead of GetComponent
            //        Damageable may be on a parent GameObject, not the collider itself
            Damageable damageable = other.GetComponentInParent<Damageable>();

            if (damageable != null)
            {
                // FIX 2: Convert float damage to int to match TakeDamage(int)
              //  int dmgInt = Mathf.RoundToInt(damage);
                Debug.Log($"[Projectile] Hit player for {damage})");
                damageable.TakeDamage(damage);
            }
            else
            {
                // This warning will appear in Console if the tag or component is wrong
                Debug.LogWarning($"[Projectile] Hit '{other.name}' with Player tag but NO Damageable found on it or any parent!");
            }

            SpawnImpactVFX();
            Destroy(gameObject);
            return;
        }

        // Destroy on environment — ignore other enemies, boss, and other projectiles
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss") && !other.CompareTag("Projectile"))
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

    // Manual damage setter — useful for player-fired projectiles
    public void SetDamage(float dmg) => damage = dmg;
    public float GetDamage()         => damage;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
