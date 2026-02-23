using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AttackHandler : MonoBehaviour
{
    // References
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private AttackHitResolver _combatHitResolver;
    [SerializeField] private Damageable damageable;
    [SerializeField] private WeaponData[] weapons; // Held weapons / inventory

    // Weapon visual
    private WeaponVisual _weaponVisual;
    // Current weapon
    private WeaponData currentWeapon;
    private Weapon equippedWeapon;
    private int currentWeaponIndex = 0;
    // Weapon information
    private string[] _attackNames;
    private string _heavyAttackName;
    private Transform projectileSpawn;

    // Events
    public UnityEvent attackEvent;
    public UnityEvent heavyAttackEvent;

    bool _isAttack;
    public bool _isBlock;
    int _attackStep;
    bool _isHeavyAttacking;

    private bool _lightQueued = false;
    private bool _heavyQueued = false;
    private Coroutine _comboCoroutine;

    float blockHeldTime = 0f;
    private Vector3 externalVelocity = Vector3.zero;

    private void Start()
    {
        _isAttack = false;
        _isBlock = false;
        _attackStep = 0;
        _isHeavyAttacking = false;
    }

    private void Update()
    {
        // Stop attack when dead
        if (damageable.GetHealth() <= 0)
            return;

        // Block
        UpdateBlock();
        
        // Resolve hit
        _combatHitResolver.ResolveHit(currentWeapon, equippedWeapon, _isHeavyAttacking);

        // Push velocity handler
        if (externalVelocity.magnitude > 0.01f)
        {
            _characterController.Move(externalVelocity * Time.deltaTime);
            // reduce velocity overtime
            // 8f is the velocity reduction mutliplier
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, 8f * Time.deltaTime);
        }
    }

    private void StartCombo()
    {
        if (_isAttack) return;

        _isAttack = true;
        _animator.SetBool("IsAttack", true);

        // run the queued attacks
        _comboCoroutine = StartCoroutine(PerformCombo());
    }

    private IEnumerator PerformCombo()
    {
        // Ranged weapon
        if (currentWeapon.isRanged)
        {
            FireProjectile();
        }
        // Melee weapon
        else
        {
            // Decide which attack to play
            while (_lightQueued || _heavyQueued)
            {
                string animName;

                // _heavyQueued might change during the loop
                bool wasHeavy = _heavyQueued;

                // Consume buffers
                _lightQueued = false;
                _heavyQueued = false;

                if (wasHeavy)
                {
                    animName = _heavyAttackName;
                    _isHeavyAttacking = true; // used for events related to heavy attacks
                    _attackStep = 0; // reset combo step

                    _animator.SetBool("IsHeavyAttack", true);
                }
                else
                {
                    _attackStep++;
                    _animator.SetInteger("AttackStep", _attackStep);

                    animName = _attackNames[Mathf.Clamp(_attackStep - 1, 0, _attackNames.Length - 1)];
                }

                yield return null; // wait a frame for animator to update

                int safetyFrames = 0;
                while (!IsCurrentAnimationReadyForNextStep(animName) && safetyFrames < 300)
                {
                    safetyFrames++;
                    yield return null;
                }
            }
        }

        ResetCombo();
    }

    public void FireProjectile()
    {
        if (currentWeapon.projectilePrefab == null || projectileSpawn == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Vector3 targetPoint = ray.origin + ray.direction * 50f;

        Vector3 direction = (targetPoint - projectileSpawn.position).normalized;

        GameObject proj = Instantiate(
            currentWeapon.projectilePrefab,
            projectileSpawn.position,
            Quaternion.LookRotation(direction)
        );

        // initialize projectile
        if (proj.TryGetComponent<Projectiles>(out var p))
        {
            p.Init(currentWeapon.damage, direction);
        }
    }

    private bool IsCurrentAnimationReadyForNextStep(string name)
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // allow transition when the animator is in the expected clip and normalized time passed threshold,
        // or if it's already beyond
        return stateInfo.IsName(name) && stateInfo.normalizedTime >= 0.7f;
    }

    private void ResetCombo()
    {
        if (_comboCoroutine != null)
        {
            StopCoroutine(_comboCoroutine);
            _comboCoroutine = null;
        }

        equippedWeapon?.DisableColliders();

        _isAttack = false;
        _lightQueued = false;
        _heavyQueued = false;
        _isHeavyAttacking = false;
        _attackStep = 0;

        _animator.SetInteger("AttackStep", 0);
        _animator.SetBool("IsAttack", false);
        _animator.SetBool("IsHeavyAttack", false);
    }

    public bool IsAttacking() { return _isAttack; }

    public bool IsBlocking() { return _isBlock; }

    public void RequestLightAttack()
    {
        _lightQueued = true;

        if (!_isAttack)
            StartCombo();
    }

    public void RequestHeavyAttack()
    {
        _heavyQueued = true;

        if (!_isAttack)
            StartCombo();
    }

    public void StartBlock()
    {
        ResetCombo();
        _isBlock = true;
        _animator.SetBool("IsBlocking", true);
    }

    private void UpdateBlock()
    {
        if (_isBlock)
            blockHeldTime += Time.deltaTime;
    }

    public void StopBlock()
    {
        _isBlock = false;
        _animator.SetBool("IsBlocking", false);
        blockHeldTime = 0f;
    }

    public float GetBlockTime() { return blockHeldTime; }

    public void TriggerStaggerAnim()
    {
        _animator.SetTrigger("Stagger");
    }

    

    public void ApplyBlockPush(Vector3 direction, float force)
    {
        direction.y = 0f;
        externalVelocity = direction.normalized * force;
    }

    public float GetParryTime() { return currentWeapon.parryTime; }

    public float GetBlockAngle() { return currentWeapon.blockAngle; }
}
