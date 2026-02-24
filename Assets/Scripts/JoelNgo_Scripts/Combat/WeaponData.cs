using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;

    [Header("Animations")]
    public string[] lightAttackNames;
    public string heavyAttackName;

    [Header("Stats")]
    public int damage = 5;
    public float heavyMultiplier = 2f;

    [Header("WeaponModel")]
    public GameObject modelPrefab;

    [Header("Ranged")]
    public bool isRanged;
    public GameObject projectilePrefab;
    public float fireCooldown = 0.5f;
}
