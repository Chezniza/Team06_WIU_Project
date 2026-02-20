using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class AttackHandler : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private BoxCollider[] detectors;
    [SerializeField] private CinemachineImpulseSource source;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private LayerMask m_LayerMask;
    // Stats
    [SerializeField] Stats stats;
    private int damage;
    [SerializeField] private Damageable damageable;
    [SerializeField] float parryTime = 0.2f;
    [SerializeField] float blockAngle = 120f;

    public UnityEvent attackEvent;
    public UnityEvent heavyAttackEvent;
    public UnityEvent attackHitEvent;
    public UnityEvent blockHitEvent;
    public UnityEvent staggerEvent;

    bool _isAttack;
    public bool _isBlock;
    int _attackStep;
    bool _isHeavyAttacking;

    private bool _lightQueued = false;
    private bool _heavyQueued = false;
    private Coroutine _comboCoroutine;

    private string[] _attackNames = new string[] { "LightAttack1", "LightAttack2", "LightAttack3" };
    private string _heavyAttackName = "HeavyAttack";
    int maxAttacks = 3;
    float blockHeldTime = 0f;
    private Vector3 externalVelocity = Vector3.zero;

    private void Start()
    {
        _isAttack = false;
        _isBlock = false;
        _attackStep = 0;
        _isHeavyAttacking = false;

        foreach (var collider in detectors)
            collider.enabled = false;

        damage = stats.damage;
    }

    private void EnableCollider(int index)
    {
        detectors[index].enabled = true;

        if (_isHeavyAttacking)
            heavyAttackEvent.Invoke();
        else
            attackEvent.Invoke();
    }
    private void DisableCollider(int index) => detectors[index].enabled = false;

    private void Update()
    {
        if (damageable.GetHealth() <= 0)
            return;

        UpdateBlock();

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
                    collider.enabled = false;
                    GameObject target = hitColliders[i].gameObject;

                    // Check if target is blocking
                    if (target.TryGetComponent<AttackHandler>(out AttackHandler targetAttackHandler))
                    {
                        if (targetAttackHandler.IsBlocking() && IsFacingTarget(target.transform, this.transform))
                        {
                            // Get target's block time
                            float targetBlockTime = targetAttackHandler.GetBlockTime();

                            // Get parried if target blocked precisely
                            if (targetBlockTime < parryTime)
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
                        int finalDamage = _isHeavyAttacking ? stats.damage * 2 : stats.damage;
                        damageable.TakeDamage(finalDamage);
                    }

                    attackHitEvent.Invoke();
                    source.GenerateImpulse(Camera.main.transform.forward);
                }
            }
        }

        // Push velocity
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

        ResetCombo();
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

        foreach (var collider in detectors)
        {
            collider.enabled = false;
        }
        
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

    public void Stagger(GameObject target)
    {
        AttackHandler targetAttackHandler = target.GetComponent<AttackHandler>();

        // If AI, use a specialised stagger function
        if (target.TryGetComponent<EnemyAI>(out EnemyAI ai))
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

    private bool IsFacingTarget(Transform defender, Transform attacker)
    {
        Vector3 toAttacker = (attacker.position - defender.position).normalized;
        toAttacker.y = 0f;

        Vector3 forward = defender.forward;
        forward.y = 0f;

        float dot = Vector3.Dot(forward, toAttacker);
        float dotThreshold = Mathf.Cos(blockAngle * 0.5f * Mathf.Deg2Rad);
        return dot >= dotThreshold;
    }

    public void ApplyBlockPush(Vector3 direction, float force)
    {
        direction.y = 0f;
        externalVelocity = direction.normalized * force;
    }

}
