using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    [SerializeField] private Transform weaponSocket;

    private Weapon currentWeapon;

    public Weapon EquipWeapon(GameObject prefab)
    {
        if (currentWeapon != null)
            Destroy(currentWeapon.gameObject);

        GameObject obj = Instantiate(prefab, weaponSocket);
        currentWeapon = obj.GetComponent<Weapon>();

        return currentWeapon;
    }
}