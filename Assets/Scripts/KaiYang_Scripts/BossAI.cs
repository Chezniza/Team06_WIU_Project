using System.Collections;
using UnityEngine;

// ============================================================
//  BossAI.cs
//  Two-phase boss: melee combo, ranged burst, AoE spell.
//  Inherits shared movement, blocking, and gravity from EnemyBase.
// ============================================================

public class BossAI : EnemyBase
{
    public enum BossPhase { Phase1, Phase2 }

    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Phase Transition")]
    [SerializeField] private float phase2HPThreshold        = 0.5f;   // 50 % HP
    [SerializeField] private float phase2SpeedBonus         = 1.2f;
    [SerializeField] private float phase2BlockChance        = 0.4f;
    [SerializeField] private float phase2AttackCooldownMult = 0.7f;

    [Header("Ranged Attack")]
    [SerializeField] private Transform  AimPoint;
    [SerializeField] private float      rangedRange        = 10f;
    [SerializeField] private float      rangedCooldown     = 3.5f;
    [SerializeField] private int        rangedDamage       = 15;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  projectileSpawnPoint;
    [SerializeField] private GameObject burstProjectilePrefab;  // Phase 2 spread shot

    [Header("Spell / AoE")]
    [SerializeField] private float      spellCooldown  = 6f;
    [SerializeField] private int        spellDamage    = 30;
    [SerializeField] private float      aoeRadius      = 4f;
    [SerializeField] private float      aoeDelay       = 1.2f;
    [SerializeField] private GameObject aoeWarningPrefab;

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private BossPhase currentPhase        = BossPhase.Phase1;
    private bool      phaseTransitionDone = false;

    private float rangedTimer = 0f;
    private float spellTimer  = 0f;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        if (!phaseTransitionDone)
            CheckPhaseTransition();

        // Tick cooldowns every frame regardless of state
        rangedTimer = Mathf.Max(0f, rangedTimer - Time.deltaTime);
        spellTimer  = Mathf.Max(0f, spellTimer  - Time.deltaTime);

        base.Update();  // runs the shared FSM (idle / chase / attack)
    }

    // ─────────────────────────────────────────────
    // CHASE RANGE — shoot or close in
    // ─────────────────────────────────────────────

    protected override void OnChaseRange(float distance)
    {
        if (distance <= rangedRange)
        {
            // Phase 2 prefers spell over ranged shot
            if (currentPhase == BossPhase.Phase2 && spellTimer <= 0f)
            {
                StartCoroutine(SpellAttack());
                return;
            }
            if (rangedTimer <= 0f && projectilePrefab != null)
            {
                StartCoroutine(RangedAttack());
                return;
            }
        }

        Chase();
    }

    // ─────────────────────────────────────────────
    // ATTACK RANGE — melee with phase-aware choices
    // ─────────────────────────────────────────────

    protected override void OnAttackRange()
    {
        animator.SetBool("IsWalking", false);
        RotateTowards(player.position - transform.position);

        if (isBlocking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        // Higher block chance in Phase 2
        float currentBlockChance = currentPhase == BossPhase.Phase2
            ? phase2BlockChance
            : blockChance;

        if (Random.value < currentBlockChance)
        {
            StartAIBlock();
            return;
        }

        // Phase 2: 40 % chance for rapid melee combo
        if (currentPhase == BossPhase.Phase2 && Random.value < 0.4f)
        {
            StartCoroutine(HeavyComboAttack());
            return;
        }

        // Default single hit
        if (Random.value < 0.25f)
            attackHandler.RequestHeavyAttack();
        else
            attackHandler.RequestLightAttack();

        attackTimer = attackCooldown;
    }

    // ─────────────────────────────────────────────
    // COROUTINE ATTACKS
    // ─────────────────────────────────────────────

    private IEnumerator HeavyComboAttack()
    {
        isActing = true;
        animator.SetTrigger("MeleeCombo");

        attackHandler.RequestHeavyAttack();
        yield return new WaitForSeconds(0.4f);
        attackHandler.RequestLightAttack();

        attackTimer = attackCooldown * phase2AttackCooldownMult;
        isActing = false;
    }

    private IEnumerator RangedAttack()
    {
        isActing = true;
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("RangedShot");

        yield return new WaitForSeconds(0.5f);  // telegraph windup

        if (isBlocking) { isActing = false; yield break; }

        if (currentPhase == BossPhase.Phase2)
        {
            // Fan burst: centre + two angled projectiles
            FireProjectile(burstProjectilePrefab ?? projectilePrefab);
            yield return new WaitForSeconds(0.12f);
            FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab,  15f);
            yield return new WaitForSeconds(0.12f);
            FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab, -15f);
        }
        else
        {
            FireProjectile(projectilePrefab);   // single aimed shot
        }

        rangedTimer = rangedCooldown;
        isActing = false;
    }

    private IEnumerator SpellAttack()
    {
        isActing = true;
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("SpellCast");

        Vector3 targetPos = player.position;
        GameObject warning = null;
        if (aoeWarningPrefab != null)
            warning = Instantiate(aoeWarningPrefab, targetPos, Quaternion.identity);

        yield return new WaitForSeconds(aoeDelay);

        if (warning != null) Destroy(warning);
        DealAoeDamage(targetPos, aoeRadius, spellDamage);

        spellTimer  = spellCooldown;
        rangedTimer = rangedCooldown * 0.5f;
        isActing = false;
    }

    // ─────────────────────────────────────────────
    // PHASE TRANSITION
    // ─────────────────────────────────────────────

    private void CheckPhaseTransition()
    {
        float hpPercent = (float)damageable.GetHealth() / damageable.GetMaxHealth();
        if (hpPercent <= phase2HPThreshold)
        {
            phaseTransitionDone = true;
            StartCoroutine(DoPhaseTransition());
        }
    }

    private IEnumerator DoPhaseTransition()
    {
        isActing = true;
        animator.SetTrigger("PhaseTransition");

        // TODO: screen shake, VFX burst, audio cue

        yield return new WaitForSeconds(3f);

        currentPhase = BossPhase.Phase2;
        Debug.Log($"[{enemyName}] Entered Phase 2!");

        // NOTE: only modify attackCooldown here — Chase() reads moveSpeed directly
        // so don't add phase2SpeedBonus to moveSpeed (it's applied in Chase() via the check)
        attackCooldown *= phase2AttackCooldownMult;

        isActing = false;
    }

    // ─────────────────────────────────────────────
    // PROJECTILE HELPERS
    // ─────────────────────────────────────────────

    private Vector3 GetAimPosition()
    {
        return AimPoint != null ? AimPoint.position : player.position + Vector3.up * 1f;
    }

    private void FireProjectile(GameObject prefab)
    {
        if (prefab == null || projectileSpawnPoint == null) return;
        Vector3 dir = (GetAimPosition() - projectileSpawnPoint.position).normalized;
        GameObject p = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
        InitProjectile(p, dir);
    }

    private void FireProjectileAngled(GameObject prefab, float angleOffset)
    {
        if (prefab == null || projectileSpawnPoint == null) return;
        Vector3 baseDir = (GetAimPosition() - projectileSpawnPoint.position).normalized;
        Vector3 dir = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;
        GameObject p = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
        InitProjectile(p, dir);
    }

    private void InitProjectile(GameObject p, Vector3 dir)
    {
        Projectiles proj = p.GetComponent<Projectiles>();
        if (proj == null) return;
        proj.Init(rangedDamage, dir, AimPoint);
        if (currentPhase == BossPhase.Phase2) proj.EnableHoming();
    }

    private void DealAoeDamage(Vector3 origin, float radius, int dmg)
    {
        foreach (var hit in Physics.OverlapSphere(origin, radius))
            if (hit.CompareTag("Player"))
                hit.GetComponent<Damageable>()?.TakeDamage(dmg);
    }

    // ─────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();    // detection (yellow) + melee (red)

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
