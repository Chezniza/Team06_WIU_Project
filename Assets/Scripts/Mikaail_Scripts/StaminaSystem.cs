using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 15f;

    public bool isExhausted { get; private set; }
    public bool isRecovering { get; private set; }
    private bool inRecoveryPhase = false;
    private float currentStamina;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        Regenerate();

        // Exit recovery ONLY when full
        if (inRecoveryPhase && currentStamina >= maxStamina)
        {
            inRecoveryPhase = false;
            isExhausted = false;
            Debug.Log("EXITED RECOVERY PHASE");
        }
    }

    public bool UseStamina(float amount)
    {
        // HARD LOCK during recovery
        if (inRecoveryPhase)
        {
            Debug.Log("Cannot use stamina, in recovery phase!");
            return false;
        }

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        if (currentStamina <= 0)
        {
            currentStamina = 0;
            EnterRecoveryPhase();
            return false;
        }

        return true;
    }

    private void EnterRecoveryPhase()
    {
        if (inRecoveryPhase) return;

        inRecoveryPhase = true;
        isExhausted = true;

        Debug.Log("ENTERED RECOVERY PHASE");
    }

    private void Regenerate()
    {
        if (currentStamina < maxStamina)
            currentStamina += regenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public bool IsInRecovery()
    {
        return inRecoveryPhase;
    }



    public float GetStamina() => currentStamina;
    public float GetMaxStamina() => maxStamina;
}