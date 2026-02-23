using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;

    [Header("Animations")]
    public string[] lightAttackNames;
    public string heavyAttackName;

    [Header("Damage")]
    public int damage;
    public float heavyMultiplier = 2f;

    [Header("WeaponModel")]
    public GameObject modelPrefab;

    [Header("Ranged")]
    public bool isRanged;
    public GameObject projectilePrefab;
}
