using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponVisual weaponVisual;
    [SerializeField] private GameObject crosshair;
    private HitDetector hitDetector;

    [Header("Weapons")]
    [SerializeField] private WeaponData[] weapons;

    private WeaponData currentWeapon;
    private Weapon equippedWeapon;
    private int currentWeaponIndex = 0;

    private BoxCollider[] detectors;
    private Transform projectileSpawn;

    public WeaponData CurrentWeapon => currentWeapon;
    public BoxCollider[] Detectors => detectors;
    public Transform ProjectileSpawn => projectileSpawn;

    private void Start()
    {
        hitDetector = GetComponent<HitDetector>();

        // Equip first weapon by default
        if (weapons.Length > 0)
        {
            EquipWeapon(weapons[0]);
        }
    }

    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;

        // Enable / disable crosshair
        if (this.GetComponent<PlayerController>())
        {
            bool v = currentWeapon.isRanged ? true : false;
            crosshair.SetActive(v);
        }

        equippedWeapon = weaponVisual.EquipWeapon(data.modelPrefab);

        detectors = equippedWeapon.GetColliders();
        projectileSpawn = equippedWeapon.GetProjectileSpawn();
    }

    public void CycleWeapon()
    {
        if (weapons == null || weapons.Length == 0) return;

        currentWeaponIndex++;

        if (currentWeaponIndex >= weapons.Length)
            currentWeaponIndex = 0;

        EquipWeapon(weapons[currentWeaponIndex]);
    }

    public void EnableCollider(int index)
    {
        equippedWeapon?.EnableCollider(index);

        hitDetector.clearHitTargets(); // reset hit targets for this attack
    }

    public void DisableColliders()
    {
        equippedWeapon?.DisableColliders();
    }

    public void FireProjectile(Camera cam)
    {
        if (currentWeapon.projectilePrefab == null || projectileSpawn == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Vector3 targetPoint = ray.origin + ray.direction * 50f;
        Vector3 direction = (targetPoint - projectileSpawn.position).normalized;

        GameObject proj = Instantiate(
            currentWeapon.projectilePrefab,
            projectileSpawn.position,
            Quaternion.LookRotation(direction)
        );

        if (proj.TryGetComponent<Projectiles>(out var p))
        {
            p.Init(currentWeapon.damage, direction);
        }
    }
}