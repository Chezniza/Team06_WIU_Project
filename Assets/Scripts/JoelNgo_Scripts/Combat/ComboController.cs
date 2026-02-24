using System.Collections;
using UnityEngine;

public class ComboController : MonoBehaviour
{
    private Animator animator;
    private WeaponController weapon;

    private bool isAttack;
    private bool isHeavyAttacking;
    private int attackStep;

    // Combo
    private bool lightQueued;
    private bool heavyQueued;
    private Coroutine comboCoroutine;

    // Ranged
    private float nextFireTime = 0f;

    public bool IsAttacking => isAttack;
    public bool IsHeavyAttacking => isHeavyAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        weapon = GetComponent<WeaponController>();
    }

    private void Update()
    {
        var weaponData = weapon.CurrentWeapon;

        if (nextFireTime > 0f)
            nextFireTime -= Time.deltaTime;
    }

    public void RequestLightAttack()
    {
        // Ranged
        var weaponData = weapon.CurrentWeapon;

        if (weaponData.isRanged)
        {
            TryFireRanged();
            return;
        }

        // Melee
        lightQueued = true;

        if (!isAttack)
            StartCombo();
    }

    public void RequestHeavyAttack()
    {
        heavyQueued = true;

        if (!isAttack)
            StartCombo();
    }

    private void StartCombo()
    {
        if (isAttack) return;

        isAttack = true;
        animator.SetBool("IsAttack", true);

        comboCoroutine = StartCoroutine(PerformCombo());
    }

    private IEnumerator PerformCombo()
    {
        var weaponData = weapon.CurrentWeapon;

        while (lightQueued || heavyQueued)
        {
            string animName;

            bool wasHeavy = heavyQueued;

            lightQueued = false;
            heavyQueued = false;

            if (wasHeavy)
            {
                animName = weaponData.heavyAttackName;
                isHeavyAttacking = true;
                attackStep = 0;

                animator.SetBool("IsHeavyAttack", true);
            }
            else
            {
                attackStep++;

                animator.SetInteger("AttackStep", attackStep);

                animName = weaponData.lightAttackNames[
                    Mathf.Clamp(attackStep - 1, 0, weaponData.lightAttackNames.Length - 1)
                ];
            }

            yield return null;

            int safety = 0;

            while (!IsReady(animName) && safety < 300)
            {
                safety++;
                yield return null;
            }
        }

        ResetCombo();
    }

    private bool IsReady(string name)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        return state.IsName(name) && state.normalizedTime >= 0.7f;
    }

    public void ResetCombo()
    {
        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);

        isAttack = false;
        isHeavyAttacking = false;
        attackStep = 0;

        animator.SetInteger("AttackStep", 0);
        animator.SetBool("IsAttack", false);
        animator.SetBool("IsHeavyAttack", false);
    }

    private void TryFireRanged()
    {
        var weaponData = weapon.CurrentWeapon;

        if (nextFireTime > 0f)
            return;

        nextFireTime = weaponData.fireCooldown;

        weapon.FireProjectile(Camera.main);
    }
}