using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 15f;

    public bool isExhausted { get; private set; }
    private float currentStamina;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        Regenerate();

        if (!isExhausted && currentStamina <= 0.01f)
            isExhausted = true;

        if (isExhausted && currentStamina >= maxStamina)
            isExhausted = false;
    }

    public bool UseStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        return currentStamina > 0;
    }

    private void Regenerate()
    {
        if (currentStamina < maxStamina)
            currentStamina += regenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public float GetStamina() => currentStamina;
    public float GetMaxStamina() => maxStamina;
}