using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  BossAI.cs
//  Two-phase boss: melee combo, ranged burst, AoE spell,
//  and a Phase 2 pillar mechanic that locks movement.
//  Inherits shared movement, blocking, and gravity from EnemyBase.
// ============================================================

public class BossAI : EnemyBase
{
    public enum BossPhase { Phase1, Phase2 }

    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Phase Transition")]
    [SerializeField] private float phase2HPThreshold        = 0.5f;
    [SerializeField] private float phase2SpeedBonus         = 1.2f;
    [SerializeField] private float phase2BlockChance        = 0.4f;
    [SerializeField] private float phase2AttackCooldownMult = 0.7f;

    [Header("Ranged Attack")]
    [SerializeField] private Transform  AimPoint;
    [SerializeField] private float      rangedRange    = 10f;
    [SerializeField] private float      rangedCooldown = 3.5f;
    [SerializeField] private int        rangedDamage   = 15;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  projectileSpawnPoint;
    [SerializeField] private GameObject burstProjectilePrefab;

    [Header("Spell / AoE")]
    [SerializeField] private float      spellCooldown  = 6f;
    [SerializeField] private int        spellDamage    = 30;
    [SerializeField] private float      aoeRadius      = 4f;
    [SerializeField] private float      aoeDelay       = 1.2f;
    [SerializeField] private GameObject aoeWarningPrefab;

    [Header("Pillar Phase (Phase 2 only)")]
    [SerializeField] private GameObject pillarPrefab;
    [SerializeField] private int        pillarCount          = 4;
    [SerializeField] private float      pillarSpawnRadius    = 4f;    // distance from boss
    [SerializeField] private float      pillarPhaseInterval  = 30f;   // seconds between pillar phases
    [SerializeField] private float      pillarRangedCooldown = 1.5f;  // faster shooting while locked

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private BossPhase currentPhase        = BossPhase.Phase1;
    private bool      phaseTransitionDone = false;

    private float rangedTimer      = 0f;
    private float spellTimer       = 0f;
    private float pillarPhaseTimer = 30f;  // matches pillarPhaseInterval default — set via DoPhaseTransition

    private bool               inPillarPhase = false;
    private List<PillarHealth> activePillars = new List<PillarHealth>();

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        if (!phaseTransitionDone)
            CheckPhaseTransition();

        // Blocking: freeze in place
        if (isBlocking)
        {
            animator.SetBool("IsWalking", false);
            ApplyGravity();
            return;
        }

        rangedTimer = Mathf.Max(0f, rangedTimer - Time.deltaTime);
        spellTimer  = Mathf.Max(0f, spellTimer  - Time.deltaTime);

        // Pillar phase interval tick — only in Phase 2, outside an active pillar phase
        if (currentPhase == BossPhase.Phase2 && !inPillarPhase)
        {
            pillarPhaseTimer -= Time.deltaTime;
            if (pillarPhaseTimer <= 0f)
            {
                // Force-stop any stuck coroutine before starting pillar phase
                StopAllCoroutines();
                isActing   = false;
                isBlocking = false;
                attackHandler.StopBlock();
                StartCoroutine(PillarPhase());
                return;
            }
        }

        // During pillar phase: rooted in place, shoot only
        if (inPillarPhase)
        {
            animator.SetBool("IsWalking", false);
            RotateTowards(player.position - transform.position);
            PillarPhaseShoot();
            ApplyGravity();
            return;
        }

        base.Update();  // normal FSM
    }

    // ─────────────────────────────────────────────
    // CHASE & ATTACK (normal FSM hooks)
    // ─────────────────────────────────────────────

    protected override void OnChaseRange(float distance)
    {
        if (distance <= rangedRange)
        {
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

    protected override void OnAttackRange()
    {
        animator.SetBool("IsWalking", false);
        RotateTowards(player.position - transform.position);

        if (isBlocking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        float currentBlockChance = currentPhase == BossPhase.Phase2
            ? phase2BlockChance : blockChance;

        if (Random.value < currentBlockChance) { StartAIBlock(); return; }

        if (currentPhase == BossPhase.Phase2 && Random.value < 0.4f)
        {
            StartCoroutine(HeavyComboAttack());
            return;
        }

        if (Random.value < 0.25f) attackHandler.RequestHeavyAttack();
        else                      attackHandler.RequestLightAttack();

        attackTimer = attackCooldown;
    }

    // ─────────────────────────────────────────────
    // PILLAR PHASE
    // ─────────────────────────────────────────────

    private IEnumerator PillarPhase()
    {
        inPillarPhase = true;
        activePillars.Clear();

        animator.SetBool("IsWalking", false);
        animator.SetTrigger("SpellCast");

        yield return new WaitForSeconds(1f);    // windup before pillars appear

        SpawnPillars();
        Debug.Log($"[{enemyName}] Pillar phase — {activePillars.Count} pillars spawned.");

        // Wait until every pillar has been destroyed
        yield return new WaitUntil(() => activePillars.Count == 0);

        Debug.Log($"[{enemyName}] All pillars destroyed — resuming normal behaviour.");
        inPillarPhase    = false;
        pillarPhaseTimer = pillarPhaseInterval;
    }

    private void SpawnPillars()
    {
        if (pillarPrefab == null)
        {
            Debug.LogWarning($"[{enemyName}] pillarPrefab is not assigned!");
            inPillarPhase = false;
            return;
        }

        float angleStep = 360f / pillarCount;

        for (int i = 0; i < pillarCount; i++)
        {
            float   angle    = i * angleStep;
            Vector3 offset   = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * pillarSpawnRadius;
            Vector3 spawnPos = transform.position + offset;

            // Snap to ground on uneven terrain
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f))
                spawnPos = hit.point;

            GameObject    go = Instantiate(pillarPrefab, spawnPos, Quaternion.identity);
            PillarHealth  ph = go.GetComponent<PillarHealth>();

            if (ph != null)
            {
                ph.Init(this);
                activePillars.Add(ph);
            }
            else
            {
                Debug.LogWarning($"[{enemyName}] Pillar prefab missing PillarHealth component!");
            }
        }
    }

    // Called by PillarHealth.Die()
    public void OnPillarDestroyed(PillarHealth pillar)
    {
        activePillars.Remove(pillar);
        Debug.Log($"[{enemyName}] Pillar down — {activePillars.Count} remaining.");
    }

    // Shoot logic while rooted during pillar phase
    private void PillarPhaseShoot()
    {
        if (isActing) return;

        if (rangedTimer <= 0f && projectilePrefab != null)
        {
            StartCoroutine(PillarRangedAttack());
            return;
        }

        if (spellTimer <= 0f)
            StartCoroutine(SpellAttack());
    }

    private IEnumerator PillarRangedAttack()
    {
        isActing = true;
        animator.SetTrigger("RangedShot");

        yield return new WaitForSeconds(0.4f);

        FireProjectile(burstProjectilePrefab ?? projectilePrefab);
        yield return new WaitForSeconds(0.1f);
        FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab,  15f);
        yield return new WaitForSeconds(0.1f);
        FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab, -15f);

        rangedTimer = pillarRangedCooldown;
        isActing    = false;
    }

    // ─────────────────────────────────────────────
    // STANDARD ATTACKS
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

        yield return new WaitForSeconds(0.5f);
        if (isBlocking) { isActing = false; yield break; }

        if (currentPhase == BossPhase.Phase2)
        {
            FireProjectile(burstProjectilePrefab ?? projectilePrefab);
            yield return new WaitForSeconds(0.12f);
            FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab,  15f);
            yield return new WaitForSeconds(0.12f);
            FireProjectileAngled(burstProjectilePrefab ?? projectilePrefab, -15f);
        }
        else
        {
            FireProjectile(projectilePrefab);
        }

        rangedTimer = rangedCooldown;
        isActing    = false;
    }

    private IEnumerator SpellAttack()
    {
        isActing = true;
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("SpellCast");

        Vector3    targetPos = player.position;
        GameObject warning   = null;
        if (aoeWarningPrefab != null)
            warning = Instantiate(aoeWarningPrefab, targetPos, Quaternion.identity);

        yield return new WaitForSeconds(aoeDelay);

        if (warning != null) Destroy(warning);
        DealAoeDamage(targetPos, aoeRadius, spellDamage);

        spellTimer  = spellCooldown;
        rangedTimer = rangedCooldown * 0.5f;
        isActing    = false;
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

        yield return new WaitForSeconds(3f);

        currentPhase     = BossPhase.Phase2;
        pillarPhaseTimer = pillarPhaseInterval;     // first pillar phase fires after interval
        attackCooldown  *= phase2AttackCooldownMult;

        Debug.Log($"[{enemyName}] Entered Phase 2!");
        isActing = false;
    }

    // ─────────────────────────────────────────────
    // PROJECTILE HELPERS
    // ─────────────────────────────────────────────

    private Vector3 GetAimPosition() =>
        AimPoint != null ? AimPoint.position : player.position + Vector3.up * 1f;

    private void FireProjectile(GameObject prefab)
    {
        if (prefab == null || projectileSpawnPoint == null) return;
        Vector3    dir = (GetAimPosition() - projectileSpawnPoint.position).normalized;
        GameObject p   = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
        InitProjectile(p, dir);
    }

    private void FireProjectileAngled(GameObject prefab, float angleOffset)
    {
        if (prefab == null || projectileSpawnPoint == null) return;
        Vector3 baseDir = (GetAimPosition() - projectileSpawnPoint.position).normalized;
        Vector3 dir     = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;
        GameObject p    = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
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
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pillarSpawnRadius);
    }
}
