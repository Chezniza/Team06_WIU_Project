using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    // Stats
    [SerializeField] Stats stats;
    private int health;
    // For damage effects
    public Color damageColor = Color.red;
    public float damageEffectDuration = 0.5f;
    private Renderer[] renderers;
    private Color[] originalColors;
    [SerializeField] private Animator _animator;
    // Health bar
    [SerializeField] private Healthbar _healthbar;
    [SerializeField] private HealthUI _healthUI;

    public UnityEvent deathEvent;
    private IEnumerator DamageEffect()
    {
        // Set all renderers to damage color instantly
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

                renderers[i].material.color = Color.Lerp(damageColor,
                    originalColors[i], elapsedTime / damageEffectDuration);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final colors are reset
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].material.color = originalColors[i];
        }
    }
    private void Start()
    {
        health = stats.Health;
        if (_healthbar != null) _healthbar.updateHealthBar(health, health);
        if(_healthUI) _healthUI.OnHealthChanged(health);
        // Get all Renderer components in this object and children
        renderers = GetComponentsInChildren<Renderer>();

        // Store original colors for each renderer
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    public void TakeDamage(int amount)
    {
        // Stop other damage effects
        StopAllCoroutines();

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null && enemy.IsInvincible()) return;

        if (health <= 0) return;

        health -= amount;
        health = Mathf.Max(health, 0);
        _healthbar.updateHealthBar(stats.Health, health);

       
        
        if (_healthUI)
        {

            _healthUI.OnHealthChanged(health);

        }

        StartCoroutine(DamageEffect());

        if (health <= 0)
        {
            _animator.SetTrigger("Die");
            deathEvent.Invoke();
        }
    }
  
    public void OnDeathAnimationFinished()
    {
        gameObject.SetActive(false);
    }

    public int GetHealth() { return health; }
    public int GetMaxHealth() { return stats.Health; }

    // Called by RespawnAltar to restore full HP on respawn
    public void ResetHealth()
    {
        
        health = stats.Health;
        if (_healthbar != null) _healthbar.updateHealthBar(health, health);
        if (_healthUI)
        {

            _healthUI.OnHealthChanged(health);

        }
    }
}
