using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private BoxCollider[] hitColliders;

    public BoxCollider[] GetColliders() => hitColliders;

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
