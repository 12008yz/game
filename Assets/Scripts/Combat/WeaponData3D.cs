using UnityEngine;

public enum WeaponFireMode3D
{
    Single,
    ShotgunSpread,
    Burst,
    HeavyProjectile
}

[CreateAssetMenu(menuName = "Game/Weapon Data 3D", fileName = "WeaponData3D")]
public class WeaponData3D : ScriptableObject
{
    public string weaponId = "weapon";
    public string displayName = "Weapon";
    public WeaponFireMode3D fireMode = WeaponFireMode3D.Single;
    public float cooldown = 0.2f;
    public float bulletSpeed = 22f;
    public int burstCount = 3;
    public float burstInterval = 0.08f;
    public int pellets = 6;
    public float spreadDegrees = 11f;
    public float damageMultiplier = 1f;
}
