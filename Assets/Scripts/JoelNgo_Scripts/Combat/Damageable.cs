using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Damageable : MonoBehaviour
{
    // Stats
    [SerializeField] Stats stats;
    private int health;
    private int bonusHealth = 0;

    // For damage effects
    public Color damageColor = Color.red;
    public float damageEffectDuration = 0.5f;
    private Renderer[] renderers;
    private Color[] originalColors;
    [SerializeField] private Animator _animator;

    // Health bar
    [SerializeField] private Healthbar _healthbar;
    [SerializeField] private HealthUI _healthUI;

    // Health text — drag TMP inside healthbar here
    [SerializeField] private TextMeshProUGUI _healthText;

    public UnityEvent deathEvent;

    private void Start()
    {
        health = stats.Health;
        RefreshHealthUI();

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    // ?? Armour bonus ??????????????????????????????????????????????????
    public void AddBonusHealth(int amount)
    {
        bonusHealth += amount;
        health += amount;
        health = Mathf.Max(health, 1);
        RefreshHealthUI();
    }

    public void RemoveBonusHealth(int amount)
    {
        bonusHealth -= amount;
        bonusHealth = Mathf.Max(bonusHealth, 0);
        health = Mathf.Min(health, GetMaxHealth());
        health = Mathf.Max(health, 1);
        RefreshHealthUI();
    }

    // ?? Damage ????????????????????????????????????????????????????????
    public void TakeDamage(int amount)
    {
        StopAllCoroutines();

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null && enemy.IsInvincible()) return;
        if (health <= 0) return;

        health -= amount;
        health = Mathf.Max(health, 0);

        RefreshHealthUI();
        StartCoroutine(DamageEffect());

        if (health <= 0)
        {
            _animator.SetTrigger("Die");
            deathEvent.Invoke();
        }
    }

    public void OnDeathAnimationFinished() => gameObject.SetActive(false);

    public int GetHealth() => health;
    public int GetMaxHealth() => stats.Health + bonusHealth;

    public void ResetHealth()
    {
        health = GetMaxHealth();
        RefreshHealthUI();
    }

    // ?? UI refresh ????????????????????????????????????????????????????
    private void RefreshHealthUI()
    {
        if (_healthbar != null) _healthbar.updateHealthBar(GetMaxHealth(), health);
        if (_healthUI) _healthUI.OnHealthChanged(health);
        if (_healthText != null) _healthText.text = $"{health} / {GetMaxHealth()}";
    }

    // ?? Damage effect ?????????????????????????????????????????????????
    private IEnumerator DamageEffect()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].material.color = damageColor;
        }

        float elapsedTime = 0f;
        while (elapsedTime < damageEffectDuration)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].material.color = Color.Lerp(
                    damageColor, originalColors[i],
                    elapsedTime / damageEffectDuration);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].material.color = originalColors[i];
        }
    }
}