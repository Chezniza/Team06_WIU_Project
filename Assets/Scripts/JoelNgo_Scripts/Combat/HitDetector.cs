using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class HitDetector : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private CinemachineImpulseSource impulse;
    [SerializeField] private CharacterController characterController;

    [Header("Events")]
    public UnityEvent attackHitEvent;
    public UnityEvent blockHitEvent;
    public UnityEvent parryEvent;
    public UnityEvent staggerEvent;

    private WeaponController weapon;
    private ComboController combo;

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    private Vector3 externalVelocity;

    private void Awake()
    {
        weapon = GetComponent<WeaponController>();
        combo = GetComponent<ComboController>();
    }

    private void Update()
    {
        HandlePushVelocity();

        var detectors = weapon.Detectors;

        if (detectors == null) return;

        foreach (var collider in detectors)
        {
            if (!collider.enabled) continue;

            Vector3 center = collider.transform.TransformPoint(collider.center);
            Vector3 halfExtents = Vector3.Scale(
                collider.size * 0.5f,
                collider.transform.lossyScale
            );

            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                collider.transform.rotation,
                layerMask
            );

            foreach (var hit in hits)
            {
                ResolveHit(hit.gameObject, collider);
            }
        }
    }

    private void ResolveHit(GameObject target, BoxCollider collider)
    {
        if (target == gameObject) return;

        if (hitTargets.Contains(target))
            return;

        hitTargets.Add(target);

        // Block / parry check
        if (target.TryGetComponent<BlockController>(out var targetBlock))
        {
            if (targetBlock.IsBlocking &&
                targetBlock.IsFacingTarget(target.transform, transform))
            {
                float targetBlockTime = targetBlock.BlockTime;

                // Parry
                if (targetBlockTime < targetBlock.ParryTime)
                {
                    parryEvent.Invoke();

                    Stagger(this.gameObject);
                    CombatFX.particleAtHit(target, collider, CombatFX.Instance.parryFX);

                    return;
                }

                // Light block
                if (!combo.IsHeavyAttacking)
                {
                    blockHitEvent.Invoke();

                    CombatFX.particleAtHit(target, collider, CombatFX.Instance.blockFX);

                    //impulse?.GenerateImpulse(Camera.main.transform.forward);
                    Vector3 pushDir = (target.transform.position - transform.position).normalized;
                    ApplyPush(pushDir, 8f);

                    return;
                }

                // Heavy guard break
                else
                {
                    Stagger(target);
                }
            }
        }

        // Damage
        if (target.TryGetComponent<Damageable>(out var damageable))
        {
            int dmg = weapon.CurrentWeapon.damage;

            if (combo.IsHeavyAttacking)
                dmg *= 2;

            damageable.TakeDamage(dmg);

            CombatFX.particleAtHit(target, collider, CombatFX.Instance.hitFX);
            attackHitEvent.Invoke();

            impulse?.GenerateImpulse(Camera.main.transform.forward);
        }
    }

    // Stagger
    private void Stagger(GameObject target)
    {
        if (target.TryGetComponent<BlockController>(out var block))
        {
            block.StopBlock();
        }
        if (target.TryGetComponent<Animator>(out var anim))
        {
            anim.SetTrigger("Stagger");
        }

        staggerEvent.Invoke();
    }

    public void ApplyPush(Vector3 direction, float force)
    {
        direction.y = 0f;
        externalVelocity = direction.normalized * force;
    }

    private void HandlePushVelocity()
    {
        if (externalVelocity.magnitude > 0.01f)
        {
            characterController.Move(externalVelocity * Time.deltaTime);

            externalVelocity = Vector3.Lerp(
                externalVelocity,
                Vector3.zero,
                8f * Time.deltaTime
            );
        }
    }

    // Runs at the start of every attack to clear hit targets
    public void clearHitTargets()
    {
        hitTargets.Clear();
    }
}