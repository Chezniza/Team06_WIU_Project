using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private StaminaSystem staminaSystem;

    void Update()
    {
        float current = staminaSystem.GetStamina();
        float max = staminaSystem.GetMaxStamina();

        fillImage.fillAmount = current / max;
    }
}