using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  BossAI.cs
//  FSM and phase logic only — all attack logic lives in BossAttack.cs.
//  To add a new attack: write a new class in BossAttack.cs, then
//  instantiate it in SetupAttacks() below. That's it.
// ============================================================

public class BossAI : EnemyBase
{
    public enum BossPhase { Phase1, Phase2, Phase3 }

    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Phase Transition")]
    [SerializeField] private float phase2HPThreshold        = 0.5f;
    [SerializeField] private float phase2BlockChance        = 0.4f;
    [SerializeField] private float phase2AttackCooldownMult = 0.7f;
    [SerializeField] private float phase3HPThreshold = 0.5f;
    [SerializeField] private float phase3BlockChance = 0.2f;
    [SerializeField] private float phase3AttackCooldownMult = 0.7f;

    [Header("Ranged Attack")]
    [SerializeField] private Transform  AimPoint;
    [SerializeField] private float      rangedRange        = 10f;
    [SerializeField] private float      rangedCooldown     = 3.5f;
    [SerializeField] private int        rangedDamage       = 15;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  projectileSpawnPoint;
    [SerializeField] private GameObject burstProjectilePrefab;

    [Header("Spell / AoE")]
    [SerializeField] private float      spellCooldown  = 6f;
    [SerializeField] private int        spellDamage    = 30;
    [SerializeField] private float      aoeRadius      = 4f;
    [SerializeField] private float      aoeDelay       = 1.2f;
    [SerializeField] private GameObject aoeWarningPrefab;
    [SerializeField] private GameObject spellProjectilePrefab;  // sphere that drops on impact
    [SerializeField] private float      spellDropHeight = 8f;   // how high above target it spawns


    [Header("Pillar Phase (Phase 2 only)")]
    [SerializeField] private GameObject pillarPrefab;
    [SerializeField] private int        pillarCount          = 4;
    [SerializeField] private float      pillarSpawnRadius    = 4f;
    [SerializeField] private float      pillarSpawnHeight    = 0f;
    [SerializeField] private float      pillarPhaseInterval  = 30f;
    [SerializeField] private float      pillarRangedCooldown = 1.5f;


    [Header("Slam Attack")]
    [SerializeField] private int        slamDamage        = 60;
    [SerializeField] private float      slamCooldown      = 15f;
    [SerializeField] private float      slamChargeTime    = 1.5f;
    [SerializeField] private float      slamRadius        = 12f;
    [SerializeField] private GameObject slamWarningPrefab;
    [SerializeField] private float      slamWarningHeightOffset = 0.05f; // tweak if disc spawns under the floor

    [Header("Minion Summon (Phase 3)")]
    [SerializeField] private GameObject[] minionPrefabs;          // drag minion prefabs here
    [SerializeField] private int minionCount = 2;
    [SerializeField] private float minionSpawnRadius = 3f;
    [SerializeField] private float summonCooldown = 40f;
    [SerializeField] private float summonShootInterval = 10f; // shoot every X seconds while waiting


    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private BossPhase currentPhase          = BossPhase.Phase1;
    private bool      phase2TransitionDone = false;
    private bool      phase3TransitionDone = false;

    private float rangedTimer      = 0f;
    private float spellTimer       = 0f;
    private float pillarPhaseTimer = 30f;
    private float summonTimer         = 0f;
    private float slamTimer           = 0f;

    private bool               inPillarPhase = false;
    private List<PillarHealth> activePillars = new List<PillarHealth>();

    // ── Attack instances (set up in SetupAttacks) ────────────
    private BasicAttack        basicAttack;
    private HeavyComboAttack   heavyComboAttack;
    private RangedAttack       rangedAttack;
    private PillarRangedAttack pillarRangedAttack;
    private SpellAttack        spellAttack;
    private SummonMinionAttack summonAttack;
    private SlamAttack         slamAttack;

    private BossContext ctx;

    // ─────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────

    private void Start()
    {
        SetupAttacks();
    }

    private void SetupAttacks()
    {
        // Build the shared context
        ctx = new BossContext
        {
            Boss          = this,
            Player        = player,
            Animator      = animator,
            Controller    = controller,
            AttackHandler = attackHandler,
            CurrentPhase  = currentPhase,
            AoeRadius            = aoeRadius,
            SpellProjectilePrefab = spellProjectilePrefab,
            SpellDropHeight       = spellDropHeight,
        };

        // ── Instantiate attacks here — add new ones below ────
        basicAttack = new BasicAttack(heavyChance: 0.25f);

        heavyComboAttack = new HeavyComboAttack(attackCooldownMult: phase2AttackCooldownMult);


        rangedAttack = new RangedAttack(
            damage:                rangedDamage,
            cooldown:              rangedCooldown,
            projectilePrefab:      projectilePrefab,
            spawnPoint:            projectileSpawnPoint,
            burstProjectilePrefab: burstProjectilePrefab,
            aimPoint:              AimPoint
        );

        pillarRangedAttack = new PillarRangedAttack(
            damage:                rangedDamage,
            cooldown:              pillarRangedCooldown,
            projectilePrefab:      projectilePrefab,
            spawnPoint:            projectileSpawnPoint,
            burstProjectilePrefab: burstProjectilePrefab,
            aimPoint:              AimPoint
        );

        summonAttack = new SummonMinionAttack(
            minionPrefabs: minionPrefabs,
            minionCount: minionCount,
            spawnRadius: minionSpawnRadius,
            shootInterval: summonShootInterval,
            rangedCooldown: rangedCooldown,
            rangedDamage: rangedDamage,
            projectilePrefab: projectilePrefab,
            spawnPoint: projectileSpawnPoint,
            burstProjectilePrefab: burstProjectilePrefab,
            aimPoint: AimPoint
        );

        spellAttack = new SpellAttack(
            damage:        spellDamage,
            radius:        aoeRadius,
            delay:         aoeDelay,
            cooldown:      spellCooldown,
            warningPrefab: aoeWarningPrefab
        );

        slamAttack = new SlamAttack(
            damage:               slamDamage,
            cooldown:             slamCooldown,
            chargeTime:           slamChargeTime,
            slamRadius:           slamRadius,
            warningPrefab:        slamWarningPrefab,
            warningHeightOffset:  slamWarningHeightOffset
        );

        // Give every attack the shared context
        basicAttack.SetContext(ctx);
        heavyComboAttack.SetContext(ctx);
        rangedAttack.SetContext(ctx);
        pillarRangedAttack.SetContext(ctx);
        spellAttack.SetContext(ctx);
        summonAttack.SetContext(ctx);
        slamAttack.SetContext(ctx);
    }

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        if (!phase2TransitionDone || !phase3TransitionDone)
            CheckPhaseTransition();

        // Keep context phase in sync
        ctx.CurrentPhase = currentPhase;

        // Tick cooldowns
        rangedTimer = Mathf.Max(0f, rangedTimer - Time.deltaTime);
        summonTimer = Mathf.Max(0f, summonTimer - Time.deltaTime);
        slamTimer   = Mathf.Max(0f, slamTimer   - Time.deltaTime);
        spellTimer  = Mathf.Max(0f, spellTimer  - Time.deltaTime);

        // Pillar timer ticks even while blocking so boss can't get permanently stuck
        if ((currentPhase == BossPhase.Phase2 || currentPhase == BossPhase.Phase3) && !inPillarPhase && !isActing)
        {
            pillarPhaseTimer -= Time.deltaTime;
            if (pillarPhaseTimer <= 0f)
            {
                StopAllCoroutines();
                isActing   = false;
                isBlocking = false;
                attackHandler.StopBlock();
                StartCoroutine(PillarPhase());
                return;
            }
        }

        // Blocking: tick timer manually here so it counts down even though
        // we return early and never reach base.Update() where it normally ticks
        if (isBlocking)
        {
            blockTimer -= Time.deltaTime;
            if (blockTimer <= 0f)
            {
                isBlocking = false;
                attackHandler.StopBlock();
            }
            animator.SetBool("IsWalking", false);
            ApplyGravity();
            return;
        }

        // Pillar phase: rooted, shooting only
        if (inPillarPhase)
        {
            animator.SetBool("IsWalking", false);
            RotateTowards(player.position - transform.position);
            PillarPhaseShoot();
            ApplyGravity();
            return;
        }

        base.Update();
    }

    // ─────────────────────────────────────────────
    // FSM HOOKS
    // ─────────────────────────────────────────────

    protected override void OnChaseRange(float distance)
    {
        if (distance <= rangedRange)
        {
            if ((currentPhase == BossPhase.Phase2 || currentPhase == BossPhase.Phase3) && spellTimer <= 0f)
            {
                spellTimer  = spellCooldown;
                rangedTimer = rangedCooldown * 0.5f;
                RunAttack(spellAttack);
                return;
            }
            if (rangedTimer <= 0f && projectilePrefab != null)
            {
                rangedTimer = rangedCooldown;   // set BEFORE coroutine starts
                RunAttack(rangedAttack);
                return;
            }
        }
        // Summon minions if off cooldown 
        if (currentPhase == BossPhase.Phase3 && summonTimer <= 0f && minionPrefabs != null && minionPrefabs.Length > 0)
        {
            summonTimer = summonCooldown;
            RunAttack(summonAttack);
            return;
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

        // Phase 2+: slam attack — highest priority, telegraphed so player can dodge
        if (slamTimer <= 0f)
        {
            slamTimer   = float.MaxValue;  // block re-trigger until coroutine resets it on completion
            attackTimer = attackCooldown;
            RunAttack(slamAttack);
            return;
        }

        // Phase 2: combo roll
        if ((currentPhase == BossPhase.Phase2 || currentPhase == BossPhase.Phase3) && Random.value < 0.4f)
        {
            RunAttack(heavyComboAttack);
            attackTimer = attackCooldown * phase2AttackCooldownMult;
            return;
        }

        // Fallback basic hit
        RunAttack(basicAttack);
        attackTimer = attackCooldown;
    }

    // Wraps any attack in the isActing coroutine lock
    private void RunAttack(BossAttackBase attack)
    {
        StartCoroutine(ExecuteAttack(attack));
    }

    private IEnumerator ExecuteAttack(BossAttackBase attack)
    {
        isActing = true;
        yield return StartCoroutine(attack.Execute());
        // Read slam cooldown write-back after attack finishes
        if (ctx.SlamCooldownRemaining > 0f)
        {
            slamTimer                   = ctx.SlamCooldownRemaining;
            ctx.SlamCooldownRemaining   = 0f;
        }
        isActing = false;
    }

    // ─────────────────────────────────────────────
    // PILLAR PHASE
    // ─────────────────────────────────────────────

    private IEnumerator PillarPhase()
    {
        inPillarPhase = true;
        isInvincible  = true;
        activePillars.Clear();

        animator.SetBool("IsWalking", false);
        animator.SetTrigger("SpellCast");

        yield return new WaitForSeconds(1f);

        SpawnPillars();
        Debug.Log($"[{enemyName}] Pillar phase — {activePillars.Count} pillars spawned.");

        yield return new WaitUntil(() => activePillars.Count == 0);

        Debug.Log($"[{enemyName}] All pillars destroyed — resuming normal behaviour.");
        inPillarPhase    = false;
        isInvincible     = false;
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
            Vector3 spawnPos = transform.position + offset + Vector3.up * pillarSpawnHeight;

            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f))
                spawnPos = hit.point + Vector3.up * (pillarPrefab.transform.localScale.y * 0.8f);

            GameObject   go = Instantiate(pillarPrefab, spawnPos, Quaternion.identity);
            PillarHealth ph = go.GetComponent<PillarHealth>();

            if (ph != null) { ph.Init(this); activePillars.Add(ph); }
            else Debug.LogWarning($"[{enemyName}] Pillar prefab missing PillarHealth component!");
        }
    }

    public void OnPillarDestroyed(PillarHealth pillar)
    {
        activePillars.Remove(pillar);
        Debug.Log($"[{enemyName}] Pillar down — {activePillars.Count} remaining.");
    }

    private void PillarPhaseShoot()
    {
        if (isActing) return;
        if (rangedTimer <= 0f && projectilePrefab != null)
        {
            rangedTimer = pillarRangedCooldown;   // set BEFORE coroutine starts
            RunAttack(pillarRangedAttack);
        }
        else if (spellTimer <= 0f)
        {
            spellTimer  = spellCooldown;
            rangedTimer = rangedCooldown * 0.5f;
            RunAttack(spellAttack);
        }
    }

    // ─────────────────────────────────────────────
    // PHASE TRANSITION
    // ─────────────────────────────────────────────

    private void CheckPhaseTransition()
    {
        float hpPercent = (float)damageable.GetHealth() / damageable.GetMaxHealth();

        if (!phase3TransitionDone && hpPercent <= phase3HPThreshold)
        {
            phase3TransitionDone = true;
            phase2TransitionDone = true; // ensure phase2 doesn't re-trigger
            StartCoroutine(DoPhase3Transition());
            return;
        }

        if (!phase2TransitionDone && hpPercent <= phase2HPThreshold)
        {
            phase2TransitionDone = true;
            StartCoroutine(DoPhaseTransition());
        }
    }

    private IEnumerator DoPhaseTransition()
    {
        isActing = true;
        isInvincible = true;
        animator.SetTrigger("PhaseTransition");

        yield return new WaitForSeconds(3f);

        currentPhase     = BossPhase.Phase2;
        pillarPhaseTimer = 0;
        attackCooldown  *= phase2AttackCooldownMult;

        Debug.Log($"[{enemyName}] Entered Phase 2!");
        isInvincible = false;
        isActing = false;
        
    }

    private IEnumerator DoPhase3Transition()
    {
        isActing = true;
        isInvincible = true;
        animator.SetTrigger("PhaseTransition");

        yield return new WaitForSeconds(3f);

        currentPhase   = BossPhase.Phase3;
        attackCooldown *= phase3AttackCooldownMult;
        summonTimer    = 0f;  // trigger summon immediately on phase 3 entry

        Debug.Log($"[{enemyName}] Entered Phase 3!");
        isInvincible = false;
        isActing = false;
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
