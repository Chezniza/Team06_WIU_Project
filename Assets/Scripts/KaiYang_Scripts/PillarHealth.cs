using UnityEngine;



public class PillarHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 50;

    private int currentHealth;
    private BossAI boss;

    public void Init(BossAI bossRef)
    {
        currentHealth = maxHealth;
        boss = bossRef;
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        boss?.OnPillarDestroyed(this);
        Destroy(gameObject);
    }
}
