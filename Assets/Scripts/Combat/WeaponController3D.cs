using UnityEngine;

public class WeaponController3D : MonoBehaviour
{
    [SerializeField] WeaponData3D[] weaponLoadout;
    [SerializeField] int defaultAmmo = 80;
    [SerializeField] float muzzleOffset = 0.8f;

    float _cooldown;
    float _burstTimer;
    int _burstLeft;
    Vector3 _burstDir;
    int _currentWeaponIndex;
    int _ammoRemaining;
    float _damageBonus = 1f;

    public int AmmoRemaining => _ammoRemaining;
    public int MaxAmmo => defaultAmmo;
    public string ActiveWeaponName => ActiveWeapon != null ? ActiveWeapon.displayName : "Blaster";
    public int WeaponIndex => _currentWeaponIndex;
    WeaponData3D ActiveWeapon => (weaponLoadout != null && weaponLoadout.Length > 0 && _currentWeaponIndex >= 0 && _currentWeaponIndex < weaponLoadout.Length)
        ? weaponLoadout[_currentWeaponIndex] : null;

    void Awake()
    {
        _ammoRemaining = Mathf.Max(0, defaultAmmo);
        if (weaponLoadout == null || weaponLoadout.Length == 0)
            weaponLoadout = BuildFallbackLoadout();
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;
        HandleSwitchInput();
        ProcessBurstFire();
    }

    public bool TryFire(Vector3 aimDirection)
    {
        if (_ammoRemaining <= 0) return false;
        if (_cooldown > 0f) return false;
        if (aimDirection.sqrMagnitude < 0.0001f) return false;

        var weapon = ActiveWeapon;
        if (weapon == null) return false;

        Vector3 origin = transform.position;
        origin.y = 0.35f;
        Vector3 spawn = origin + aimDirection.normalized * muzzleOffset;
        FireWithWeapon(weapon, spawn, aimDirection.normalized);
        _ammoRemaining--;
        _cooldown = Mathf.Max(0.03f, weapon.cooldown);
        return true;
    }

    public void ApplyModifier(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        switch (id)
        {
            case "ammo_pack":
                _ammoRemaining = Mathf.Min(MaxAmmo, _ammoRemaining + 4);
                break;
            case "damage_boost":
                _damageBonus = Mathf.Min(2.5f, _damageBonus * 1.16f);
                break;
            case "fire_rate":
                if (weaponLoadout == null) return;
                for (int i = 0; i < weaponLoadout.Length; i++)
                    weaponLoadout[i].cooldown = Mathf.Max(0.04f, weaponLoadout[i].cooldown * 0.9f);
                break;
        }
    }

    public void SetAmmoCapacity(int maxAmmo)
    {
        defaultAmmo = Mathf.Clamp(maxAmmo, 10, 999);
        _ammoRemaining = Mathf.Clamp(_ammoRemaining, 0, defaultAmmo);
    }

    public void RefillAmmo(int amount)
    {
        _ammoRemaining = Mathf.Clamp(_ammoRemaining + Mathf.Max(0, amount), 0, defaultAmmo);
    }

    void FireWithWeapon(WeaponData3D weapon, Vector3 spawn, Vector3 dir)
    {
        switch (weapon.fireMode)
        {
            case WeaponFireMode3D.ShotgunSpread:
                int pellets = Mathf.Clamp(weapon.pellets, 3, 12);
                for (int i = 0; i < pellets; i++)
                {
                    float yaw = Random.Range(-weapon.spreadDegrees, weapon.spreadDegrees);
                    Vector3 d = Quaternion.Euler(0f, yaw, 0f) * dir;
                    BulletFactory3D.Spawn(spawn, d, weapon.bulletSpeed);
                }
                break;
            case WeaponFireMode3D.Burst:
                BulletFactory3D.Spawn(spawn, dir, weapon.bulletSpeed);
                _burstLeft = Mathf.Max(0, weapon.burstCount - 1);
                _burstDir = dir;
                _burstTimer = weapon.burstInterval;
                break;
            case WeaponFireMode3D.HeavyProjectile:
                BulletFactory3D.Spawn(spawn, dir, weapon.bulletSpeed * 0.65f);
                break;
            default:
                BulletFactory3D.Spawn(spawn, dir, weapon.bulletSpeed);
                break;
        }
    }

    void ProcessBurstFire()
    {
        if (_burstLeft <= 0) return;
        _burstTimer -= Time.deltaTime;
        if (_burstTimer > 0f) return;
        var weapon = ActiveWeapon;
        Vector3 origin = transform.position;
        origin.y = 0.35f;
        Vector3 spawn = origin + _burstDir * muzzleOffset;
        BulletFactory3D.Spawn(spawn, _burstDir, weapon != null ? weapon.bulletSpeed : 22f);
        _burstLeft--;
        _burstTimer = weapon != null ? weapon.burstInterval : 0.08f;
    }

    void HandleSwitchInput()
    {
        if (weaponLoadout == null || weaponLoadout.Length == 0) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) _currentWeaponIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _currentWeaponIndex = Mathf.Min(1, weaponLoadout.Length - 1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) _currentWeaponIndex = Mathf.Min(2, weaponLoadout.Length - 1);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) _currentWeaponIndex = Mathf.Min(3, weaponLoadout.Length - 1);
    }

    WeaponData3D[] BuildFallbackLoadout()
    {
        return new[]
        {
            MakeFallback("sidearm", "Sidearm", WeaponFireMode3D.Single, 0.16f, 24f),
            MakeFallback("shotgun", "Shotgun", WeaponFireMode3D.ShotgunSpread, 0.44f, 20f, pellets: 6, spread: 13f),
            MakeFallback("burst", "Burst Rifle", WeaponFireMode3D.Burst, 0.36f, 24f, burstCount: 3, burstInterval: 0.07f),
            MakeFallback("heavy", "Heavy Blaster", WeaponFireMode3D.HeavyProjectile, 0.65f, 16f)
        };
    }

    static WeaponData3D MakeFallback(string id, string name, WeaponFireMode3D mode, float cooldown, float speed, int burstCount = 3, float burstInterval = 0.08f, int pellets = 6, float spread = 11f)
    {
        var w = ScriptableObject.CreateInstance<WeaponData3D>();
        w.weaponId = id;
        w.displayName = name;
        w.fireMode = mode;
        w.cooldown = cooldown;
        w.bulletSpeed = speed;
        w.burstCount = burstCount;
        w.burstInterval = burstInterval;
        w.pellets = pellets;
        w.spreadDegrees = spread;
        return w;
    }
}
