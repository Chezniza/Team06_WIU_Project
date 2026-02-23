using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class AttackHitResolver : MonoBehaviour
{
    // References
    [SerializeField] private LayerMask m_LayerMask;
    [SerializeField] private CinemachineImpulseSource source;

    // Events
    public UnityEvent attackHitEvent;
    public UnityEvent blockHitEvent;
    public UnityEvent staggerEvent;

    public void ResolveHit(WeaponData currentWeapon, Weapon equippedWeapon, bool _isHeavyAttacking)
    {
        BoxCollider[] detectors = equippedWeapon.GetColliders();

        foreach (var collider in detectors)
        {
            if (collider.enabled)
            {
                Vector3 center = collider.transform.TransformPoint(collider.center);
                Vector3 halfExtents = Vector3.Scale(collider.size * 0.5f, collider.transform.lossyScale);

                Collider[] hitColliders = Physics.OverlapBox(
                    center,
                    halfExtents,
                    collider.transform.rotation,
                    m_LayerMask
                );

                // Hit logic
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    GameObject target = hitColliders[i].gameObject;

                    // Check if target is blocking
                    if (target.TryGetComponent<AttackHandler>(out AttackHandler targetAttackHandler))
                    {
                        if (targetAttackHandler.IsBlocking() && IsFacingTarget(target.transform, this.transform))
                        {
                            // Get target's block time
                            float targetBlockTime = targetAttackHandler.GetBlockTime();

                            // Get parried if target blocked precisely
                            if (targetBlockTime < targetAttackHandler.GetParryTime())
                            {
                                Stagger(this.gameObject);
                                Debug.Log("Parry");
                                return;
                            }
                            // Light attack hit target
                            else if (!_isHeavyAttacking)
                            {
                                blockHitEvent.Invoke();
                                source.GenerateImpulse(Camera.main.transform.forward);

                                // Push target
                                Vector3 pushDir = (target.transform.position - transform.position).normalized;
                                targetAttackHandler.ApplyBlockPush(pushDir, 8f);

                                return; // stop here to prevent damage
                            }
                            // Heavy attack hit target
                            else
                            {
                                Stagger(target);
                            }
                        }
                    }

                    // Check if target can be damaged
                    if (target.TryGetComponent<Damageable>(out Damageable damageable))
                    {
                        int damage = currentWeapon.damage;
                        int finalDamage = _isHeavyAttacking ? damage * 2 : damage;
                        damageable.TakeDamage(finalDamage);
                        collider.enabled = false; // disable damage hitbox
                    }

                    attackHitEvent.Invoke();
                    source.GenerateImpulse(Camera.main.transform.forward);
                }
            }
        }
    }

    public void Stagger(GameObject target)
    {
        AttackHandler targetAttackHandler = target.GetComponent<AttackHandler>();

        // If AI, use a specialised stagger function
        if (target.TryGetComponent<EnemyBase>(out EnemyBase ai))
        {
            ai.BreakBlockAndStagger();
            staggerEvent.Invoke();
        }
        else
        {
            targetAttackHandler.TriggerStaggerAnim();
            targetAttackHandler.StopBlock();
            staggerEvent.Invoke();
        }
    }

    public bool IsFacingTarget(Transform defender, Transform attacker)
    {
        AttackHandler defenderAttackHandler = defender.GetComponent<AttackHandler>();

        Vector3 toAttacker = (attacker.position - defender.position).normalized;
        toAttacker.y = 0f;

        Vector3 forward = defender.forward;
        forward.y = 0f;

        float dot = Vector3.Dot(forward, toAttacker);
        float dotThreshold = Mathf.Cos(defenderAttackHandler.GetBlockAngle() * 0.5f * Mathf.Deg2Rad);
        return dot >= dotThreshold;
    }
}
