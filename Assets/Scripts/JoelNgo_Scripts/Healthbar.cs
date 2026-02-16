using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Image _healthbarSprite;

    public void updateHealthBar(int maxHealth, int currentHealth)
    {
        _healthbarSprite.fillAmount = (float)currentHealth / maxHealth;
    }
}
