using UnityEngine;

public class BlockController : MonoBehaviour
{
    private Animator animator;

    [SerializeField] float parryTime = 0.2f;
    [SerializeField] float blockAngle = 120f;

    private bool isBlock;
    private float blockHeldTime;

    public bool IsBlocking => isBlock;
    public float BlockTime => blockHeldTime;
    public float ParryTime => parryTime;

    private void Update()
    {
        animator = GetComponent<Animator>();

        if (isBlock)
            blockHeldTime += Time.deltaTime;
    }

    public void StartBlock()
    {
        isBlock = true;
        animator.SetBool("IsBlocking", true);
    }

    public void StopBlock()
    {
        isBlock = false;
        animator.SetBool("IsBlocking", false);
        blockHeldTime = 0f;
    }

    public bool IsFacingTarget(Transform defender, Transform attacker)
    {
        Vector3 toAttacker = (attacker.position - defender.position).normalized;
        toAttacker.y = 0f;

        Vector3 forward = defender.forward;
        forward.y = 0f;

        float dot = Vector3.Dot(forward, toAttacker);
        float dotThreshold = Mathf.Cos(blockAngle * 0.5f * Mathf.Deg2Rad);

        return dot >= dotThreshold;
    }
}