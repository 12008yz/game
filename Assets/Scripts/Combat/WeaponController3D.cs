using UnityEngine;

public class WeaponController3D : MonoBehaviour
{
    [SerializeField] WeaponData3D[] weaponLoadout;
    [SerializeField] int defaultAmmo = 80;
    [SerializeField] float muzzleOffset = 0.8f;
    [SerializeField] bool infiniteAmmo = true;

    float _cooldown;
    float _burstTimer;
    int _burstLeft;
    Vector3 _burstDir;
    int _currentWeaponIndex;
    int _ammoRemaining;
    float _damageBonus = 1f;
    readonly int[] _cachedOffer = new int[3];
    static readonly Collider[] BeamHits = new Collider[48];
    BeamStripFx3D _beamFx;
    bool _beamHeld;

    public int AmmoRemaining => infiniteAmmo ? 9999 : _ammoRemaining;
    public int MaxAmmo => defaultAmmo;
    public bool InfiniteAmmo => infiniteAmmo;
    public string ActiveWeaponName => ActiveWeapon != null ? ActiveWeapon.displayName : "Blaster";
    public int WeaponIndex => _currentWeaponIndex;
    public int WeaponCount => weaponLoadout != null ? weaponLoadout.Length : 0;
    WeaponData3D ActiveWeapon => (weaponLoadout != null && weaponLoadout.Length > 0 && _currentWeaponIndex >= 0 && _currentWeaponIndex < weaponLoadout.Length)
        ? weaponLoadout[_currentWeaponIndex] : null;

    void Awake()
    {
        _ammoRemaining = Mathf.Max(0, defaultAmmo);
        if (weaponLoadout == null || weaponLoadout.Length == 0)
            weaponLoadout = BuildFallbackLoadout();
        _beamFx = BeamStripFx3D.Ensure(transform);
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;
        HandleSwitchInput();
        ProcessBurstFire();
        if (!_beamHeld && _beamFx != null)
            _beamFx.StopBeam();
        _beamHeld = false;
    }

    public bool TryFire(Vector3 aimDirection)
    {
        if (!infiniteAmmo && _ammoRemaining <= 0) return false;
        if (_cooldown > 0f) return false;
        if (aimDirection.sqrMagnitude < 0.0001f) return false;

        var weapon = ActiveWeapon;
        if (weapon == null) return false;

        Vector3 origin = transform.position;
        origin.y = 0.35f;
        Vector3 spawn = origin + aimDirection.normalized * muzzleOffset;
        FireWithWeapon(weapon, spawn, aimDirection.normalized);
        if (!infiniteAmmo)
            _ammoRemaining--;
        _cooldown = Mathf.Max(0.03f, weapon.cooldown);
        return true;
    }

    public bool IsContinuousBeamWeaponActive()
    {
        var w = ActiveWeapon;
        return w != null && w.fireMode == WeaponFireMode3D.Single && w.weaponId == "blaster";
    }

    public void SetBeamHolding(bool hold, Vector3 aimDirection)
    {
        if (!IsContinuousBeamWeaponActive())
        {
            if (_beamFx != null) _beamFx.StopBeam();
            return;
        }

        if (!hold)
        {
            if (_beamFx != null) _beamFx.StopBeam();
            return;
        }

        _beamHeld = true;
        Vector3 origin = transform.position;
        origin.y = 0.35f;
        Vector3 spawn = origin + aimDirection.normalized * muzzleOffset;
        FireBlasterStrip(spawn, aimDirection.normalized, persistent: true);
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

    public string GetWeaponName(int weaponIndex)
    {
        if (weaponLoadout == null || weaponIndex < 0 || weaponIndex >= weaponLoadout.Length) return "Unknown";
        return weaponLoadout[weaponIndex] != null ? weaponLoadout[weaponIndex].displayName : $"Weapon {weaponIndex + 1}";
    }

    public int[] BuildWeaponOffer(int count = 3)
    {
        int weaponCount = WeaponCount;
        if (weaponCount <= 0) return _cachedOffer;
        int safeCount = Mathf.Clamp(count, 1, _cachedOffer.Length);
        for (int i = 0; i < safeCount; i++)
            _cachedOffer[i] = -1;

        _cachedOffer[0] = _currentWeaponIndex;
        for (int i = 1; i < safeCount; i++)
        {
            int candidate = Random.Range(0, weaponCount);
            int guard = 0;
            while (ContainsOffer(candidate, i) && guard++ < 16)
                candidate = Random.Range(0, weaponCount);
            _cachedOffer[i] = candidate;
        }
        return _cachedOffer;
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (weaponLoadout == null || weaponLoadout.Length == 0) return;
        _currentWeaponIndex = Mathf.Clamp(weaponIndex, 0, weaponLoadout.Length - 1);
    }

    void FireWithWeapon(WeaponData3D weapon, Vector3 spawn, Vector3 dir)
    {
        switch (weapon.fireMode)
        {
            case WeaponFireMode3D.ShotgunSpread:
                int pellets = Mathf.Clamp(weapon.pellets + 2, 4, 14);
                for (int i = 0; i < pellets; i++)
                {
                    float yaw = Random.Range(-weapon.spreadDegrees, weapon.spreadDegrees);
                    Vector3 d = Quaternion.Euler(0f, yaw, 0f) * dir;
                    float speedJitter = Random.Range(0.82f, 1.12f);
                    BulletFactory3D.Spawn(spawn, d, weapon.bulletSpeed * speedJitter);
                }
                break;
            case WeaponFireMode3D.Burst:
                BulletFactory3D.Spawn(spawn, dir, weapon.bulletSpeed);
                _burstLeft = Mathf.Max(0, weapon.burstCount - 1);
                _burstDir = dir;
                _burstTimer = weapon.burstInterval;
                break;
            case WeaponFireMode3D.HeavyProjectile:
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                BulletFactory3D.Spawn(spawn + right * 0.16f, dir, weapon.bulletSpeed * 0.62f);
                BulletFactory3D.Spawn(spawn - right * 0.16f, dir, weapon.bulletSpeed * 0.62f);
                break;
            default:
                FireBlasterStrip(spawn, dir, persistent: false);
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
        float sweep = (_burstLeft % 2 == 0) ? -2.5f : 2.5f;
        Vector3 d = Quaternion.Euler(0f, sweep, 0f) * _burstDir;
        BulletFactory3D.Spawn(spawn, d, weapon != null ? weapon.bulletSpeed : 22f);
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
            MakeFallback("blaster", "Blaster Beam", WeaponFireMode3D.Single, 0.12f, 24f),
            MakeFallback("shotgun", "Scatter Shotgun", WeaponFireMode3D.ShotgunSpread, 0.52f, 20f, pellets: 7, spread: 16f),
            MakeFallback("burst", "Burst Carbine", WeaponFireMode3D.Burst, 0.34f, 24f, burstCount: 4, burstInterval: 0.06f),
            MakeFallback("heavy", "Twin Heavy", WeaponFireMode3D.HeavyProjectile, 0.72f, 16f)
        };
    }

    void FireBlasterStrip(Vector3 spawn, Vector3 dir, bool persistent)
    {
        float range = LevelBuilder.Instance != null ? LevelBuilder.Instance.GetRangeByArenaFraction(1f / 5f) : 10f;
        if (Physics.Raycast(spawn, dir, out RaycastHit wallHit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            if (wallHit.collider != null && wallHit.collider.GetComponentInParent<WallTag>() != null)
                range = Mathf.Max(0.2f, wallHit.distance);
        }

        Vector3 center = spawn + dir * (range * 0.5f);
        Vector3 half = new Vector3(0.42f, 0.45f, range * 0.5f);
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        int count = Physics.OverlapBoxNonAlloc(center, half, BeamHits, rot, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var c = BeamHits[i];
            if (c == null) continue;
            var enemy = c.GetComponentInParent<ChaserEnemy3D>();
            if (enemy == null) continue;
            enemy.TakeHit();
        }
        if (_beamFx == null)
            _beamFx = BeamStripFx3D.Ensure(transform);
        if (_beamFx != null)
            _beamFx.SetBeam(spawn, dir, range, 0.46f);
        if (!persistent && _beamFx != null)
            _beamFx.StopBeam();
    }

    bool ContainsOffer(int idx, int upToExclusive)
    {
        for (int i = 0; i < upToExclusive; i++)
            if (_cachedOffer[i] == idx) return true;
        return false;
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
