using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Melee Weapon")]
    [SerializeField] private BoxCollider[] hitColliders;

    [Header("Projectile Weapon")]
    [SerializeField] private Transform projectileSpawnPoint;

    public BoxCollider[] GetColliders() => hitColliders;
    public Transform GetProjectileSpawn() => projectileSpawnPoint;

    private void Awake()
    {
        // Auto find if not assigned
        if (hitColliders == null || hitColliders.Length == 0)
            hitColliders = GetComponentsInChildren<BoxCollider>(true);

        DisableColliders();
    }

    public void EnableCollider(int index)
    {
        if (index >= 0 && index < hitColliders.Length)
            hitColliders[index].enabled = true;
    }

    public void DisableColliders()
    {
        foreach (var c in hitColliders)
            c.enabled = false;
    }
}
