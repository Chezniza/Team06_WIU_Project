using System.Collections;
using UnityEngine;



public abstract class EnemyBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform           player;
    [SerializeField] protected Animator            animator;
    [SerializeField] protected CharacterController controller;
    [SerializeField] protected Damageable          damageable;
    [SerializeField] protected AttackHandler       attackHandler;
    [SerializeField] protected string              enemyName = "Enemy";

    [Header("Movement")]
    [SerializeField] protected float moveSpeed   = 2.5f;
    [SerializeField] protected float rotateSpeed = 720f;

    [Header("Ranges")]
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange    = 2f;

    [Header("Attack")]
    [SerializeField] protected float attackCooldown = 1.2f;
    protected float attackTimer;

    [Header("Blocking")]
    [SerializeField] protected float   blockChance        = 0.2f;
    [SerializeField] protected Vector2 blockDurationRange = new Vector2(0.5f, 1.2f);
    protected bool  isBlocking;
    protected float blockTimer;

    // ── Shared private physics state ──────────────────────
    protected Vector3 velocity;
    protected float   gravity = -9.81f;

    // ── Coroutine lock — prevents overlapping actions ──────
    protected bool isActing = false;

    // ── Invincibility flag — set by subclasses (e.g. boss pillar phase) ──
    protected bool isInvincible = false;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE  (subclasses call base.Update or override fully)
    // ─────────────────────────────────────────────

    protected virtual void Awake()
    {
        // Auto-find player if not assigned in Inspector
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    protected virtual void Update()
    {
        if (!player) return;
        if (damageable.GetHealth() <= 0) return;

        if (isActing) { ApplyGravity(); return; }

        if (isBlocking)
        {
            blockTimer -= Time.deltaTime;
            if (blockTimer <= 0f)
            {
                isBlocking = false;
                attackHandler.StopBlock();
                animator.SetBool("IsWalking", false);
            }
            ApplyGravity();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if      (distance > detectionRange) Idle();
        else if (distance > attackRange)    OnChaseRange(distance);
        else                                OnAttackRange();

        ApplyGravity();
    }

    // ─────────────────────────────────────────────
    // ABSTRACT HOOKS — subclasses fill these in
    // ─────────────────────────────────────────────

    protected abstract void OnChaseRange(float distance);   // what to do while chasing
    protected abstract void OnAttackRange();                // what to do in melee range

    // ─────────────────────────────────────────────
    // SHARED STATES
    // ─────────────────────────────────────────────

    protected void Idle()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsAttack",  false);
    }

    protected void Chase()
    {
        if (isBlocking) return;
        animator.SetBool("IsWalking", true);
        animator.SetBool("IsAttack",  false);

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        dir.Normalize();
        RotateTowards(dir);
        controller.Move(dir * moveSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // BLOCKING
    // ─────────────────────────────────────────────

    protected void StartAIBlock()
    {
        isBlocking = true;
        blockTimer = Random.Range(blockDurationRange.x, blockDurationRange.y);
        attackHandler.StartBlock();
    }

    public bool IsInvincible() => isInvincible;

    public void BreakBlockAndStagger()
    {
        animator.SetTrigger("Stagger");
        isBlocking = false;
        blockTimer = 0f;
        attackHandler.StopBlock();
    }

    // ─────────────────────────────────────────────
    // PHYSICS HELPERS
    // ─────────────────────────────────────────────

    protected void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, rotateSpeed * Time.deltaTime);
    }

    protected void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
