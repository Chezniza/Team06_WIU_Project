using UnityEngine;

// ============================================================
//  MinionAI.cs
//  Simple melee enemy: detect → chase → attack.
//  Inherits shared movement, blocking, and gravity from EnemyBase.
// ============================================================

public class MinionAI : EnemyBase
{
    // No extra fields needed — everything lives in EnemyBase.
    // Add minion-specific serialized fields here if you need them later.

    // ─────────────────────────────────────────────
    // CHASE RANGE — just run at the player
    // ─────────────────────────────────────────────

    protected override void OnChaseRange(float distance)
    {
        Chase();
    }

    // ─────────────────────────────────────────────
    // ATTACK RANGE — light / heavy / block
    // ─────────────────────────────────────────────

    protected override void OnAttackRange()
    {
        animator.SetBool("IsWalking", false);
        RotateTowards(player.position - transform.position);

        if (isBlocking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        // Roll for block first
        if (Random.value < blockChance)
        {
            StartAIBlock();
            return;
        }

        // Light or heavy hit
        if (Random.value < 0.25f)
            attackHandler.RequestHeavyAttack();
        else
            attackHandler.RequestLightAttack();

        attackTimer = attackCooldown;
    }

    // ─────────────────────────────────────────────
    // GIZMOS  (base already draws detection + attack rings)
    // ─────────────────────────────────────────────

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}
