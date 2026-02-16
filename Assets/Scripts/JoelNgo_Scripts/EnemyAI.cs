using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Damageable damageable;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotateSpeed = 720f;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 2f;

    [Header("Attack")]
    [SerializeField] private AttackHandler attackHandler;
    [SerializeField] private float attackCooldown = 1.2f;
    private float attackTimer;
    // Blocking
    [SerializeField] float blockChance = 0.2f;
    [SerializeField] Vector2 blockDurationRange = new Vector2(0.5f, 1.2f);
    bool isBlocking;
    float blockTimer;

    private Vector3 velocity;
    private float gravity = -9.81f;

    private void Update()
    {
        if (!player) return;

        if (damageable.GetHealth() <= 0)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            Idle();
        }
        else if (distance > attackRange)
        {
            Chase();
        }
        else
        {
            Attack();
        }

        ApplyGravity();
    }

    private void Idle()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsAttack", false);
    }

    private void Chase()
    {
        animator.SetBool("IsWalking", true);
        animator.SetBool("IsAttack", false);

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        dir.Normalize();

        RotateTowards(dir);
        controller.Move(dir * moveSpeed * Time.deltaTime);
    }

    private void Attack()
    {
        animator.SetBool("IsWalking", false);
        RotateTowards(player.position - transform.position);

        // Handle active block
        if (isBlocking)
        {
            blockTimer -= Time.deltaTime;
            if (blockTimer <= 0f)
            {
                isBlocking = false;
                attackHandler.StopBlock();
            }
            return; // do nothing else while blocking
        }

        attackTimer -= Time.deltaTime;

        // Randomly decide to block instead of attacking
        if (attackTimer <= 0f)
        {
            if (Random.value < blockChance)
            {
                StartAIBlock();
                return;
            }

            // Randomly select between heavy and light attacks
            if (Random.value < 0.25f)
                attackHandler.RequestHeavyAttack();
            else
                attackHandler.RequestLightAttack();

            attackTimer = attackCooldown;
        }
    }


    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            rotateSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

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
}
