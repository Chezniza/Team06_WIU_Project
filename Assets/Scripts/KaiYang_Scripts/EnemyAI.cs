    using System.Collections;
    using UnityEngine;

   
    public class EnemyAI : MonoBehaviour
    {

        public enum EnemyPhase { Phase1, Phase2 }


        [Header("--- BOSS TOGGLE ---")]
        [SerializeField] private bool   isBoss    = false;
        [SerializeField] private string enemyName = "Enemy";

        [Header("References")]
        [SerializeField] private Transform          player;
        [SerializeField] private Animator           animator;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Damageable         damageable;

        [Header("Movement")]
        [SerializeField] private float moveSpeed   = 2.5f;
        [SerializeField] private float rotateSpeed = 720f;

        [Header("Ranges")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackRange    = 2f;

        [Header("Attack")]
        [SerializeField] private AttackHandler attackHandler;
        [SerializeField] private float         attackCooldown = 1.2f;
        private float attackTimer;

        [Header("Blocking")]
        [SerializeField] private float   blockChance        = 0.2f;
        [SerializeField] private Vector2 blockDurationRange = new Vector2(0.5f, 1.2f);
        private bool  isBlocking;
        private float blockTimer;

        // ─────────────────────────────────────────────
        // INSPECTOR — BOSS ONLY (ignored if isBoss = false)
        // ─────────────────────────────────────────────

        [Header("--- BOSS ONLY ---")]
        [SerializeField] private float phase2HPThreshold       = 0.5f;  // 50% HP triggers Phase 2
        [SerializeField] private float phase2SpeedBonus        = 1.2f;  // added to moveSpeed in P2
        [SerializeField] private float phase2BlockChance       = 0.4f;  // boss blocks more in P2
        [SerializeField] private float phase2AttackCooldownMult = 0.7f; // boss attacks faster in P2

        [Header("Boss — Ranged & Spell")]
        [SerializeField] private float      rangedRange            = 10f;
        [SerializeField] private float      rangedCooldown         = 3.5f;
        [SerializeField] private float      rangedDamage           = 15f;  
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform  projectileSpawnPoint;
        [SerializeField] private float      spellCooldown          = 6f;
        [SerializeField] private float      spellDamage            = 30f;
        [SerializeField] private float      aoeRadius              = 4f;
        [SerializeField] private float      aoeDelay               = 1.2f;
        [SerializeField] private GameObject burstProjectilePrefab;  // Phase 2 spread shot
        [SerializeField] private GameObject aoeWarningPrefab;       // warning circle before AoE

        // ─────────────────────────────────────────────
        // PRIVATE STATE
        // ─────────────────────────────────────────────

        private EnemyPhase currentPhase        = EnemyPhase.Phase1;
        private bool       phaseTransitionDone = false;
        private bool       isActing            = false; // true while a coroutine runs

        private float rangedTimer = 0f;
        private float spellTimer  = 0f;

        private Vector3 velocity;
        private float   gravity = -9.81f;

        // ─────────────────────────────────────────────
        // UNITY LIFECYCLE
        // ─────────────────────────────────────────────

        private void Update()
        {
            if (!player) return;
            if (damageable.GetHealth() <= 0) return;

            // Boss only: watch for phase transition threshold
            if (isBoss && !phaseTransitionDone)
                CheckPhaseTransition();

            // Coroutine is running (transition / ranged / spell) — skip normal AI
            if (isActing) { ApplyGravity(); return; }

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance > detectionRange)
                Idle();
            else if (distance > attackRange)
                ChaseOrRanged(distance);    // boss may shoot from range; minions just chase
            else
                Attack();

            if (isBoss) TickBossCooldowns();

            ApplyGravity();
        }

        // ─────────────────────────────────────────────
        // STATES
        // ─────────────────────────────────────────────

        private void Idle()
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttack",  false);
        }

        // Replaces plain Chase() — boss can fire/cast from range instead of only chasing
        private void ChaseOrRanged(float distance)
        {
            if (isBoss && distance <= rangedRange)
            {
                // Phase 2 boss prefers spell over ranged shot
                if (currentPhase == EnemyPhase.Phase2 && spellTimer <= 0f)
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

        private void Chase()
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsAttack",  false);

            float speed = moveSpeed;
            if (isBoss && currentPhase == EnemyPhase.Phase2)
                speed += phase2SpeedBonus;

            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            dir.Normalize();
            RotateTowards(dir);
            controller.Move(dir * speed * Time.deltaTime);
        }

        private void Attack()
        {
            animator.SetBool("IsWalking", false);
            RotateTowards(player.position - transform.position);

            // ── Handle active block (YOUR original logic — unchanged) ──
            if (isBlocking)
            {
                blockTimer -= Time.deltaTime;
                if (blockTimer <= 0f)
                {
                    isBlocking = false;
                    attackHandler.StopBlock();
                }
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;

            // ── Decide what to do this cycle ──────────────────────────

            // Boss uses a higher block chance in Phase 2
            float currentBlockChance = (isBoss && currentPhase == EnemyPhase.Phase2)
                ? phase2BlockChance
                : blockChance;

            if (Random.value < currentBlockChance)
            {
                StartAIBlock();
                return;
            }

            // Boss Phase 2: 40% chance to do a rapid melee combo instead of single hit
            if (isBoss && currentPhase == EnemyPhase.Phase2 && Random.value < 0.4f)
            {
                StartCoroutine(HeavyComboAttack());
                return;
            }

            // Default for all enemies: light or heavy single hit
            if (Random.value < 0.25f)
                attackHandler.RequestHeavyAttack();
            else
                attackHandler.RequestLightAttack();

            attackTimer = attackCooldown;
        }

        // ─────────────────────────────────────────────
        // BOSS COROUTINE ATTACKS
        // ─────────────────────────────────────────────

        // Phase 2 melee: quick 2-hit combo
        private IEnumerator HeavyComboAttack()
        {
            isActing = true;
            animator.SetTrigger("MeleeCombo");

            attackHandler.RequestHeavyAttack();
            yield return new WaitForSeconds(0.4f);
            attackHandler.RequestLightAttack();         // fast follow-up

            attackTimer = attackCooldown * phase2AttackCooldownMult;
            isActing = false;
        }

        private IEnumerator RangedAttack()
        {
            isActing = true;
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("RangedShot");

            yield return new WaitForSeconds(0.5f);      // telegraph windup

            if (currentPhase == EnemyPhase.Phase2)
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
                FireProjectile(projectilePrefab);       // single aimed shot
            }

            rangedTimer = rangedCooldown;
            isActing = false;
        }

        private IEnumerator SpellAttack()
        {
            isActing = true;
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("SpellCast");

            // Show AoE warning circle at player's current position
            Vector3 targetPos = player.position;
            GameObject warning = null;
            if (aoeWarningPrefab != null)
                warning = Instantiate(aoeWarningPrefab, targetPos, Quaternion.identity);

            yield return new WaitForSeconds(aoeDelay);  // player has time to dodge

            if (warning != null) Destroy(warning);
            DealAoeDamage(targetPos, aoeRadius, spellDamage);

            spellTimer  = spellCooldown;
            rangedTimer = rangedCooldown * 0.5f;        // stop ranged chaining immediately after
            isActing = false;
        }

        // ─────────────────────────────────────────────
        // PHASE TRANSITION — BOSS ONLY
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

            yield return new WaitForSeconds(2.5f);

            currentPhase = EnemyPhase.Phase2;
            Debug.Log($"[{enemyName}] Entered Phase 2!");

            // ── Stat upgrades on Phase 2 entry ──
            attackCooldown *= phase2AttackCooldownMult; // attack faster
            moveSpeed      += phase2SpeedBonus;         // already added in Chase() too, so don't double-add

            isActing = false;
        }

        // ─────────────────────────────────────────────
        // BLOCKING — YOUR ORIGINAL CODE, UNCHANGED
        // ─────────────────────────────────────────────

        private void StartAIBlock()
        {
            isBlocking = true;
            blockTimer = Random.Range(blockDurationRange.x, blockDurationRange.y);
            attackHandler.StartBlock();
        }

        public void BreakBlockAndStagger()
        {
            animator.SetTrigger("Stagger");
            isBlocking = false;
            blockTimer = 0f;
            attackHandler.StopBlock();
        }

        // ─────────────────────────────────────────────
        // HELPERS — YOUR ORIGINAL + NEW BOSS ONES
        // ─────────────────────────────────────────────

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && velocity.y < 0)
                velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void TickBossCooldowns()
        {
            rangedTimer = Mathf.Max(0f, rangedTimer - Time.deltaTime);
            spellTimer  = Mathf.Max(0f, spellTimer  - Time.deltaTime);
        }

        private void FireProjectile(GameObject prefab)
        {
            if (prefab == null || projectileSpawnPoint == null) return;
            Vector3 dir = (player.position - projectileSpawnPoint.position).normalized;
            GameObject p = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
            // FIX: was incorrectly passing spellDamage — ranged shots use rangedDamage
            Projectiles proj = p.GetComponent<Projectiles>();
            if (proj != null) proj.Init(rangedDamage, dir);
        }

        private void FireProjectileAngled(GameObject prefab, float angleOffset)
        {
            if (prefab == null || projectileSpawnPoint == null) return;
            Vector3 baseDir = (player.position - projectileSpawnPoint.position).normalized;
            Vector3 dir     = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;
            GameObject p    = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(dir));
            // FIX: was incorrectly passing spellDamage — burst shots use rangedDamage * 0.8
            Projectiles proj = p.GetComponent<Projectiles>();
            if (proj != null) proj.Init(rangedDamage * 0.8f, dir);
        }

        private void DealAoeDamage(Vector3 origin, float radius, float dmg)
        {
            Collider[] hits = Physics.OverlapSphere(origin, radius);
            foreach (var hit in hits)
                if (hit.CompareTag("Player"))
                    hit.GetComponent<Damageable>()?.TakeDamage(dmg);
        }

        // ─────────────────────────────────────────────
        // GIZMOS — Scene view range visualisation
        // ─────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange); // aggro

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);    // melee

            if (isBoss)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, rangedRange);// ranged

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, aoeRadius);  // AoE spell
            }
        }
    }


