using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Damageable healthReference; 

    public Image fillImage;
    private int maxHealth;
    private int currHealth;

    private void Start()
    {
        maxHealth = healthReference.GetMaxHealth();
        currHealth = healthReference.GetHealth();

        OnHealthChanged(currHealth);
    }

    public void OnHealthChanged(int current)
    {
        float percent = (float)current / maxHealth;
        fillImage.fillAmount = percent;
    }
}
