using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class AttackHandler : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    //[SerializeField] private PlayerInput _playerInput;
    //private InputActionAsset _inputActions;
    //private InputAction _heavyAttack;

    [SerializeField] private BoxCollider[] detectors;
    [SerializeField] private CinemachineImpulseSource source;
    [SerializeField] private LayerMask m_LayerMask;
    // Stats
    [SerializeField] Stats stats;
    private int damage;
    [SerializeField] private Damageable damageable;

    public UnityEvent attackEvent;
    public UnityEvent heavyAttackEvent;
    public UnityEvent attackHitEvent;
    public UnityEvent blockHitEvent;
    public UnityEvent staggerEvent;

    bool _isAttack;
    public bool _isBlock;
    int _attackStep;
    bool _isHeavyAttacking;

    private int _queuedAttacks = 0;
    private bool _heavyQueued = false; // flag for heavy attack
    private Coroutine _comboCoroutine;

    private string[] _attackNames = new string[] { "LightAttack1", "LightAttack2", "LightAttack3" };
    private string _heavyAttackName = "HeavyAttack";
    int maxAttacks = 3;

    private void Start()
    {
        //_inputActions = _playerInput.actions;
        //_heavyAttack = _inputActions["HeavyAttack"];

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
        //Block();

        //// Queue light attack
        //if (_inputActions["Attack"].WasPressedThisFrame())
        //{
        //    _queuedAttacks = Mathf.Min(maxAttacks, _queuedAttacks + 1);

        //    if (!_isAttack)
        //        StartCombo();
        //}
        //// Queue heavy attack
        //if (_heavyAttack.WasPressedThisFrame())
        //{
        //    _queuedAttacks = 0; // clear remaining light attacks
        //    _heavyQueued = true; // queue heavy

        //    if (!_isAttack)
        //        StartCombo(); // start immediately if idle
        //}

        if (damageable.GetHealth() <= 0)
            return;

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

                for (int i = 0; i < hitColliders.Length; i++)
                {
                    collider.enabled = false;
                    GameObject target = hitColliders[i].gameObject;

                    // Check if target is blocking
                    if (target.TryGetComponent<AttackHandler>(out AttackHandler targetAttackHandler))
                    {
                        if (targetAttackHandler.IsBlocking())
                        {
                            // Light attack
                            if (!_isHeavyAttacking)
                            {
                                blockHitEvent.Invoke();
                                source.GenerateImpulse(Camera.main.transform.forward);
                                return; // stop here to prevent damage
                            }
                            // Heavy attack
                            else
                            {
                                // If AI, use a specialised stagger function
                                if (target.TryGetComponent<EnemyAI>(out EnemyAI ai))
                                {
                                    ai.BreakBlockAndStagger();
                                    staggerEvent.Invoke();
                                }
                                else
                                {
                                    targetAttackHandler.Stagger();
                                    targetAttackHandler.StopBlock();
                                    staggerEvent.Invoke();
                                }

                                // continue, because heavy attack can bypass block
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
    }

    private void StartCombo()
    {
        if (_isAttack) return;

        _isAttack = true;
        _animator.SetBool("IsAttack", true);

        // start single coroutine to run the queued attacks
        _comboCoroutine = StartCoroutine(PerformCombo());
    }

    private IEnumerator PerformCombo()
    {
        // Decide which attack to play
        while (_queuedAttacks > 0 || _heavyQueued)
        {
            string animName;

            if (_heavyQueued)
            {
                animName = _heavyAttackName;
                _heavyQueued = false;
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

            yield return null; // wait a frame for animator

            int safetyFrames = 0;
            while (!IsCurrentAnimationReadyForNextStep(animName) && safetyFrames < 300)
            {
                safetyFrames++;
                yield return null;
            }

            // Reduce queued attacks if it was light
            if (!_heavyQueued)
                _queuedAttacks--;
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

        _isAttack = false;
        _isHeavyAttacking = false;
        _attackStep = 0;
        _queuedAttacks = 0;
        _heavyQueued = false;

        _animator.SetInteger("AttackStep", 0);
        _animator.SetBool("IsAttack", false);
        _animator.SetBool("IsHeavyAttack", false);
    }


    public bool IsAttacking() { return _isAttack; }

    public bool IsBlocking() { return _isBlock; }

    //private void Block()
    //{
    //    if (_inputActions["Block"].IsPressed())
    //    {
    //        _isBlock = true;
    //        _animator.SetBool("IsBlocking", true);
    //    }
    //    else
    //    {
    //        _isBlock = false;
    //        _animator.SetBool("IsBlocking", false);
    //    }
    //}

    public void RequestLightAttack()
    {
        _queuedAttacks = Mathf.Min(maxAttacks, _queuedAttacks + 1);

        if (!_isAttack)
            StartCombo();
    }

    public void RequestHeavyAttack()
    {
        _queuedAttacks = 0;
        _heavyQueued = true;

        if (!_isAttack)
            StartCombo();
    }

    public void StartBlock()
    {
        if (_isAttack) return; // optional: prevent blocking mid-attack

        _isBlock = true;
        _animator.SetBool("IsBlocking", true);
    }

    public void StopBlock()
    {
        _isBlock = false;
        _animator.SetBool("IsBlocking", false);
    }

    public void Stagger()
    {
        _animator.SetTrigger("Stagger");
    }
}
