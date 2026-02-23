using UnityEngine;

public class AttackHandler : MonoBehaviour
{
    private ComboController combo;
    private BlockController block;

    private void Awake()
    {
        combo = GetComponent<ComboController>();
        block = GetComponent<BlockController>();
    }

    public void RequestLightAttack()
    {
        combo.RequestLightAttack();
    }

    public void RequestHeavyAttack()
    {
        combo.RequestHeavyAttack();
    }

    public void StartBlock()
    {
        block.StartBlock();
    }

    public void StopBlock()
    {
        block.StopBlock();
    }
}