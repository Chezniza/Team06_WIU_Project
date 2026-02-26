using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    private Animator animator;

    public float idleDuration = 2f;
    public float walkDuration = 3f;
    public float attackDuration = 1.5f;

    private float timer = 0f;
    private int currentState = 0; // 0 = idle, 1 = walk, 2 = attack

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            currentState = (currentState + 1) % 3;
            SetState(currentState);
        }
    }

    void SetState(int state)
    {
        // Reset all
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsAttacking", false);

        switch (state)
        {
            case 0: // Idle
                timer = idleDuration;
                break;
            case 1: // Walk
                animator.SetBool("IsWalking", true);
                timer = walkDuration;
                break;
            case 2: // Attack
                animator.SetBool("IsAttacking", true);
                timer = attackDuration;
                break;
        }
    }
}