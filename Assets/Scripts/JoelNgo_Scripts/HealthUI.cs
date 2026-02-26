using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    public Damageable healthReference; 

    public Image fillImage;
    private int maxHealth;
    private int currHealth;

    private void Start()
    {
        maxHealth = healthReference.GetMaxHealth();
        currHealth = healthReference.GetHealth();

        OnHealthChanged(0, currHealth);
    }

    private void OnHealthChanged(int previous, int current)
    {
        float percent = (float)current / maxHealth;
        fillImage.fillAmount = percent;
    }
}
