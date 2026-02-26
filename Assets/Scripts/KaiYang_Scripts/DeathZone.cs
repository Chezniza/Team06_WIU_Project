using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private int damage = 9999; 

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Damageable damageable = other.GetComponent<Damageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);
    }
}
