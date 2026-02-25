using System.Collections;
using UnityEngine;

// ============================================================
//  BossAttack.cs
//  Central home for all boss attack logic.
//
//  HOW TO ADD A NEW ATTACK:
//    1. Add a new inner class at the bottom of this file
//    2. Copy the BossContext fields and call signature from existing attacks
//    3. Wire it up in BossAI by adding a field + calling Execute() at the right time
//
//  Each attack is a plain C# class (not MonoBehaviour) that receives
//  everything it needs via BossContext. No extra GameObjects required.
// ============================================================

// ── Shared context passed into every attack ──────────────────
// BossAI creates one of these and hands it to all attacks.
public class BossContext
{
    public BossAI                Boss;
    public Transform             Player;
    public Animator              Animator;
    public CharacterController   Controller;
    public AttackHandler         AttackHandler;
    public BossAI.BossPhase      CurrentPhase;

    // Shared cooldown write-back — attacks set these so BossAI can read them
    public float RangedCooldownRemaining;
    public float SpellCooldownRemaining;
    public float SlamCooldownRemaining;

    // Shared settings
    public float      AoeRadius;
    public GameObject SpellProjectilePrefab;
    public float      SpellDropHeight;
}

// ── Base class all attacks inherit from ──────────────────────
public abstract class BossAttackBase
{
    protected BossContext ctx;

    public void SetContext(BossContext context) => ctx = context;

    // Run the attack — BossAI wraps this in a coroutine
    public abstract IEnumerator Execute();
}

// ════════════════════════════════════════════════════════════
//  ATTACKS
// ════════════════════════════════════════════════════════════

// ── Light / Heavy single hit ─────────────────────────────────
public class BasicAttack : BossAttackBase
{
    private float heavyChance;

    public BasicAttack(float heavyChance = 0.25f)
    {
        this.heavyChance = heavyChance;
    }

    public override IEnumerator Execute()
    {
        if (Random.value < heavyChance)
            ctx.AttackHandler.RequestHeavyAttack();
        else
            ctx.AttackHandler.RequestLightAttack();

        yield return null;
    }
}

// ── Phase 2 melee combo ──────────────────────────────────────
public class HeavyComboAttack : BossAttackBase
{
    private float attackCooldownMult;

    public HeavyComboAttack(float attackCooldownMult = 0.7f)
    {
        this.attackCooldownMult = attackCooldownMult;
    }

    public override IEnumerator Execute()
    {
        ctx.Animator.SetTrigger("MeleeCombo");

        ctx.AttackHandler.RequestHeavyAttack();
        yield return new WaitForSeconds(0.4f);
        ctx.AttackHandler.RequestLightAttack();
    }
}
public class SlamAttack : BossAttackBase
{
    private int        damage;
    private float      cooldown;
    private float      chargeTime;
    private float      slamRadius;
    private float      flashInterval;
    private GameObject warningPrefab;
    private float      warningHeightOffset;  // how high above ground to spawn the warning disc

    public SlamAttack(int damage = 60, float cooldown = 15f, float chargeTime = 1.5f,
                      float slamRadius = 12f, float flashInterval = 0.4f,
                      GameObject warningPrefab = null, float warningHeightOffset = 0.05f)
    {
        this.damage               = damage;
        this.cooldown             = cooldown;
        this.chargeTime           = chargeTime;
        this.slamRadius           = slamRadius;
        this.flashInterval        = flashInterval;
        this.warningPrefab        = warningPrefab;
        this.warningHeightOffset  = warningHeightOffset;
    }

    public override IEnumerator Execute()
    {
        ctx.Boss.SetInvincible(true);
        ctx.Animator.SetBool("IsWalking", false);
        ctx.Animator.SetTrigger("SlamAttack");

        // ── Spawn warning prefab centred on the boss ──────────
        GameObject warning = null;
        if (warningPrefab != null)
        {
            Vector3 spawnPos = ctx.Boss.transform.position + Vector3.up * warningHeightOffset;
            warning = Object.Instantiate(warningPrefab, spawnPos, Quaternion.identity);
            float diameter = slamRadius * 2f;
            warning.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            Debug.Log($"[SlamAttack] Warning prefab spawned at {spawnPos}, diameter {diameter}");
        }
        else
        {
            Debug.LogWarning("[SlamAttack] warningPrefab is null — assign Slam Warning Prefab in Inspector!");
        }

        Renderer warningRend = warning != null ? warning.GetComponent<Renderer>() : null;
        if (warning != null && warningRend == null)
            Debug.LogWarning("[SlamAttack] No Renderer found on warning prefab!");

        float elapsed    = 0f;
        bool  flashOn    = false;
        float flashTimer = 0f;

        Debug.Log($"[SlamAttack] Charging — {chargeTime}s until slam...");

        while (elapsed < chargeTime)
        {
            elapsed    += Time.deltaTime;
            flashTimer += Time.deltaTime;

            // Log every 0.5s so you can see it counting down
            if (Mathf.FloorToInt(elapsed * 2f) != Mathf.FloorToInt((elapsed - Time.deltaTime) * 2f))
                Debug.Log($"[SlamAttack] Charging... {elapsed:F1}s / {chargeTime}s");

            // Flash starts slow and only gently speeds up near the end
            float interval = Mathf.Lerp(flashInterval, flashInterval * 0.5f, elapsed / chargeTime);
            if (flashTimer >= interval)
            {
                flashOn    = !flashOn;
                flashTimer = 0f;

                if (warningRend != null)
                {
                    Color c = warningRend.material.color;
                    c.a = flashOn ? 0.8f : 0.3f;
                    warningRend.material.color = c;
                }
            }

            yield return null;
        }

        // ── Slam — destroy warning and deal damage ────────────
        if (warning != null) Object.Destroy(warning);

        CharacterController playerCC = ctx.Player.GetComponent<CharacterController>();
        bool playerGrounded = playerCC != null ? playerCC.isGrounded : true;

        if (playerGrounded)
        {
            float dist = Vector3.Distance(ctx.Boss.transform.position, ctx.Player.position);
            if (dist <= slamRadius)
            {
                ctx.Player.GetComponent<Damageable>()?.TakeDamage(damage);

                Animator playerAnim = ctx.Player.GetComponent<Animator>();
                if (playerAnim != null) playerAnim.SetTrigger("Stagger");

                Debug.Log("[BossAI] Slam hit grounded player!");
            }
        }
        else
        {
            Debug.Log("[BossAI] Slam missed — player jumped!");
        }

        yield return new WaitForSeconds(0.5f); // recovery

        ctx.RangedCooldownRemaining = cooldown;
        ctx.SlamCooldownRemaining   = cooldown;
        ctx.Boss.SetInvincible(false);
    }
}
// ── Single or fan-burst ranged shot ─────────────────────────
public class RangedAttack : BossAttackBase
{
    private int        damage;
    private float      cooldown;
    private GameObject projectilePrefab;
    private GameObject burstProjectilePrefab;
    private Transform  spawnPoint;
    private Transform  aimPoint;

    public RangedAttack(int damage, float cooldown,
                        GameObject projectilePrefab, Transform spawnPoint,
                        GameObject burstProjectilePrefab = null, Transform aimPoint = null)
    {
        this.damage                = damage;
        this.cooldown              = cooldown;
        this.projectilePrefab      = projectilePrefab;
        this.spawnPoint            = spawnPoint;
        this.burstProjectilePrefab = burstProjectilePrefab;
        this.aimPoint              = aimPoint;
    }

    public override IEnumerator Execute()
    {
        ctx.Animator.SetBool("IsWalking", false);
        ctx.Animator.SetTrigger("RangedShot");

        yield return new WaitForSeconds(0.5f); // telegraph windup

        if (ctx.CurrentPhase == BossAI.BossPhase.Phase2)
        {
            // Fan burst
            Fire(burstProjectilePrefab ?? projectilePrefab, 0f);
            yield return new WaitForSeconds(0.12f);
            Fire(burstProjectilePrefab ?? projectilePrefab, 15f);
            yield return new WaitForSeconds(0.12f);
            Fire(burstProjectilePrefab ?? projectilePrefab, -15f);
        }
        else
        {
            Fire(projectilePrefab, 0f);
        }

        ctx.RangedCooldownRemaining = cooldown;
    }

    private void Fire(GameObject prefab, float angleOffset)
    {
        if (prefab == null || spawnPoint == null) return;

        Vector3 aimPos  = aimPoint != null ? aimPoint.position : ctx.Player.position + Vector3.up;
        Vector3 baseDir = (aimPos - spawnPoint.position).normalized;
        Vector3 dir     = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

        GameObject  go   = Object.Instantiate(prefab, spawnPoint.position, Quaternion.LookRotation(dir));
        Projectiles proj = go.GetComponent<Projectiles>();
        if (proj == null) return;

        proj.Init(damage, dir, aimPoint);
        if (ctx.CurrentPhase == BossAI.BossPhase.Phase2) proj.EnableHoming();
    }
}

// ── Pillar-phase ranged burst (faster, always fan) ───────────
public class PillarRangedAttack : BossAttackBase
{
    private int        damage;
    private float      cooldown;
    private GameObject projectilePrefab;
    private GameObject burstProjectilePrefab;
    private Transform  spawnPoint;
    private Transform  aimPoint;

    public PillarRangedAttack(int damage, float cooldown,
                               GameObject projectilePrefab, Transform spawnPoint,
                               GameObject burstProjectilePrefab = null, Transform aimPoint = null)
    {
        this.damage                = damage;
        this.cooldown              = cooldown;
        this.projectilePrefab      = projectilePrefab;
        this.spawnPoint            = spawnPoint;
        this.burstProjectilePrefab = burstProjectilePrefab;
        this.aimPoint              = aimPoint;
    }

    public override IEnumerator Execute()
    {
        ctx.Animator.SetTrigger("RangedShot");
        yield return new WaitForSeconds(0.4f);

        Fire(burstProjectilePrefab ?? projectilePrefab, 0f);
        yield return new WaitForSeconds(0.1f);
        Fire(burstProjectilePrefab ?? projectilePrefab, 15f);
        yield return new WaitForSeconds(0.1f);
        Fire(burstProjectilePrefab ?? projectilePrefab, -15f);

        ctx.RangedCooldownRemaining = cooldown;
    }

    private void Fire(GameObject prefab, float angleOffset)
    {
        if (prefab == null || spawnPoint == null) return;

        Vector3 aimPos  = aimPoint != null ? aimPoint.position : ctx.Player.position + Vector3.up;
        Vector3 baseDir = (aimPos - spawnPoint.position).normalized;
        Vector3 dir     = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

        GameObject  go   = Object.Instantiate(prefab, spawnPoint.position, Quaternion.LookRotation(dir));
        Projectiles proj = go.GetComponent<Projectiles>();
        if (proj == null) return;

        proj.Init(damage, dir, aimPoint);
        proj.EnableHoming();
    }
}

// ── AoE spell with warning circle ───────────────────────────
public class SpellAttack : BossAttackBase
{
    private int        damage;
    private float      radius;
    private float      delay;
    private float      cooldown;
    private float      rangedSuppressionMult;
    private GameObject warningPrefab;

    public SpellAttack(int damage, float radius, float delay, float cooldown,
                       GameObject warningPrefab, float rangedSuppressionMult = 0.5f)
    {
        this.damage                  = damage;
        this.radius                  = radius;
        this.delay                   = delay;
        this.cooldown                = cooldown;
        this.warningPrefab           = warningPrefab;
        this.rangedSuppressionMult   = rangedSuppressionMult;
    }

    public override IEnumerator Execute()
    {
        ctx.Animator.SetBool("IsWalking", false);
        ctx.Animator.SetTrigger("SpellCast");

        // Snapshot target position now so it doesn't track a moving player mid-cast
        Vector3 targetPos = ctx.Player.position;

        // Spawn AoE warning ring on the ground
        GameObject warning = null;
        if (warningPrefab != null)
        {
            warning = Object.Instantiate(warningPrefab, targetPos, Quaternion.identity);
            float diameter = ctx.AoeRadius * 2f;
            warning.transform.localScale = new Vector3(diameter, warning.transform.localScale.y, diameter);
        }

        // Spawn sphere above the target and drop it down
        GameObject sphere = null;
        if (ctx.SpellProjectilePrefab != null)
        {
            float   dropHeight = ctx.SpellDropHeight > 0f ? ctx.SpellDropHeight : 8f;
            Vector3 startPos   = targetPos + Vector3.up * dropHeight;

            sphere = Object.Instantiate(ctx.SpellProjectilePrefab, startPos, Quaternion.identity);
            float diameter   = ctx.AoeRadius * 2f;
            float sphereRadius = ctx.AoeRadius;
            sphere.transform.localScale = new Vector3(diameter, diameter, diameter);

            // Land with center sitting ON the ground, not half-buried
            Vector3 landPos = targetPos + Vector3.up * sphereRadius;

            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Pow(elapsed / delay, 2f);
                if (sphere != null)
                    sphere.transform.position = Vector3.Lerp(startPos, landPos, t);
                yield return null;
            }
            // Snap to final position in case of float imprecision
            if (sphere != null)
                sphere.transform.position = landPos;
        }
        else
        {
            // No sphere prefab — just wait out the delay
            yield return new WaitForSeconds(delay);
        }

        // Impact — destroy visuals and deal damage
        if (warning != null) Object.Destroy(warning);
        if (sphere  != null) Object.Destroy(sphere);

        foreach (var hit in Physics.OverlapSphere(targetPos, radius))
            if (hit.CompareTag("Player"))
                hit.GetComponent<Damageable>()?.TakeDamage(damage);

        ctx.SpellCooldownRemaining  = cooldown;
        ctx.RangedCooldownRemaining = ctx.RangedCooldownRemaining * rangedSuppressionMult;
    }
}

// ── Summon minions — boss roots and shoots until all minions dead ─
public class SummonMinionAttack : BossAttackBase
{
    private GameObject[] minionPrefabs;    // pool of prefabs to pick from
    private int          minionCount;
    private float        spawnRadius;
    private float        shootInterval;    // how often boss shoots while waiting
    private float        rangedCooldown;
    private GameObject   projectilePrefab;
    private GameObject   burstProjectilePrefab;
    private Transform    spawnPoint;
    private Transform    aimPoint;
    private int          rangedDamage;

    public SummonMinionAttack(
        GameObject[] minionPrefabs, int minionCount, float spawnRadius,
        float shootInterval, float rangedCooldown, int rangedDamage,
        GameObject projectilePrefab, Transform spawnPoint,
        GameObject burstProjectilePrefab = null, Transform aimPoint = null)
    {
        this.minionPrefabs          = minionPrefabs;
        this.minionCount            = minionCount;
        this.spawnRadius            = spawnRadius;
        this.shootInterval          = shootInterval;
        this.rangedCooldown         = rangedCooldown;
        this.rangedDamage           = rangedDamage;
        this.projectilePrefab       = projectilePrefab;
        this.spawnPoint             = spawnPoint;
        this.burstProjectilePrefab  = burstProjectilePrefab;
        this.aimPoint               = aimPoint;
    }

    public override IEnumerator Execute()
    {
        // Boss is invincible while minions are alive
        ctx.Boss.SetInvincible(true);

        // Summon animation windup
        ctx.Animator.SetBool("IsWalking", false);
        ctx.Animator.SetTrigger("SpellCast");
        yield return new WaitForSeconds(1f);

        // Spawn minions in a circle around the boss
        var activeMinions = new System.Collections.Generic.List<GameObject>();
        float angleStep = 360f / minionCount;

        for (int i = 0; i < minionCount; i++)
        {
            if (minionPrefabs == null || minionPrefabs.Length == 0) break;

            float   angle    = i * angleStep;
            Vector3 offset   = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * spawnRadius;
            Vector3 spawnPos = ctx.Boss.transform.position + offset;

            // Snap to ground and offset up by half the minion's height so it stands on surface
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f))
                spawnPos = hit.point;

            // Pick a random prefab from the pool
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            // Offset up by half height so minion stands on ground instead of sinking in
            float halfHeight = prefab.transform.localScale.y * 0.5f;
            spawnPos += Vector3.up * halfHeight;
            GameObject minion = Object.Instantiate(prefab, spawnPos, Quaternion.identity);

            // Reset animator so the Die trigger from a previous run doesn't carry over
            Animator minionAnim = minion.GetComponent<Animator>();
            if (minionAnim != null)
            {
                minionAnim.Rebind();
                minionAnim.Update(0f);
            }

            // Ensure it's active (prefab may have been left inactive)
            minion.SetActive(true);

            activeMinions.Add(minion);
        }

        Debug.Log($"[BossAI] Summoned {activeMinions.Count} minions.");

        // Root the boss — shoot every shootInterval until all minions dead
        float shootTimer = 0f;

        while (true)
        {
            // Check if all minions are gone
            bool allDead = true;
            foreach (var m in activeMinions)
            {
                if (m != null && m.activeSelf) { allDead = false; break; }
            }
            if (allDead) break;

            // Rotate toward player while waiting
            Vector3 dir = (ctx.Player.position - ctx.Boss.transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                ctx.Boss.transform.rotation = Quaternion.RotateTowards(
                    ctx.Boss.transform.rotation,
                    Quaternion.LookRotation(dir),
                    720f * Time.deltaTime);

            // Shoot on interval
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f && projectilePrefab != null)
            {
                Fire(burstProjectilePrefab ?? projectilePrefab,  0f);
                Fire(burstProjectilePrefab ?? projectilePrefab,  15f);
                Fire(burstProjectilePrefab ?? projectilePrefab, -15f);
                shootTimer = shootInterval;
            }

            yield return null;
        }

        ctx.Boss.SetInvincible(false);
        Debug.Log("[BossAI] All minions dead — boss resuming.");
    }

    private void Fire(GameObject prefab, float angleOffset)
    {
        if (prefab == null || spawnPoint == null) return;

        Vector3 aimPos  = aimPoint != null ? aimPoint.position : ctx.Player.position + Vector3.up;
        Vector3 baseDir = (aimPos - spawnPoint.position).normalized;
        Vector3 dir     = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

        GameObject  go   = Object.Instantiate(prefab, spawnPoint.position, Quaternion.LookRotation(dir));
        Projectiles proj = go.GetComponent<Projectiles>();
        if (proj == null) return;

        proj.Init(rangedDamage, dir, aimPoint);
        proj.EnableHoming();
    }


}