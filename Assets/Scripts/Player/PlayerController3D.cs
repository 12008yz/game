using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    [SerializeField] GameBalance3D balance;
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float rotateLerp = 20f;
    [SerializeField] float fireCooldown = 0.18f;
    [SerializeField] float bulletSpeed = 22f;
    [SerializeField] float muzzleOffset = 0.8f;
    [SerializeField] int maxAmmo = 20;

    CharacterController _cc;
    CharacterPlayableAnimator3D _visualAnimator;
    WeaponKickAnimator3D _weaponKick;
    float _cooldown;
    Vector3 _aimDirection = Vector3.forward;
    int _ammoRemaining;

    public Vector3 AimDirection => _aimDirection;
    public float MoveSpeed => moveSpeed;
    public float FireCooldown => fireCooldown;
    public float BulletSpeed => bulletSpeed;
    public int MaxAmmo => maxAmmo;
    public int AmmoRemaining => _ammoRemaining;

    void Awake()
    {
        if (balance == null)
            balance = Resources.Load<GameBalance3D>("GameBalance3D");
        if (balance != null)
        {
            moveSpeed = balance.playerMoveSpeed;
            fireCooldown = balance.playerFireCooldown;
            bulletSpeed = balance.playerBulletSpeed;
            maxAmmo = balance.playerMaxAmmo;
        }
        _cc = GetComponent<CharacterController>();
        _ammoRemaining = Mathf.Max(0, maxAmmo);
    }

    void Start()
    {
        _visualAnimator = GetComponentInChildren<CharacterPlayableAnimator3D>();
        _weaponKick = GetComponentInChildren<WeaponKickAnimator3D>();
    }

    void Update()
    {
        if (GameManager3D.Instance != null && (!GameManager3D.Instance.GameStarted || GameManager3D.Instance.GameOver))
            return;

        Move();
        AimToMouse();
        Shoot();
    }

    void Move()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector3 delta = new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
        _cc.Move(delta);
        if (_visualAnimator != null)
            _visualAnimator.SetMoveAmount(input.magnitude);
    }

    void AimToMouse()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane floor = new Plane(Vector3.up, Vector3.zero);
        if (!floor.Raycast(ray, out float d)) return;
        Vector3 hit = ray.GetPoint(d);
        Vector3 from = transform.position;
        from.y = 0f;
        Vector3 dir = hit - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        _aimDirection = dir.normalized;
        Quaternion targetRot = Quaternion.LookRotation(_aimDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateLerp * 720f * Time.deltaTime);
    }

    void Shoot()
    {
        _cooldown -= Time.deltaTime;
        bool fire = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (!fire || _cooldown > 0f) return;
        if (_ammoRemaining <= 0) return;

        Vector3 origin = transform.position;
        origin.y = 0.35f;
        Vector3 spawn = origin + _aimDirection * muzzleOffset;
        BulletFactory3D.Spawn(spawn, _aimDirection, bulletSpeed);
        _cooldown = fireCooldown;
        _ammoRemaining--;
        if (_visualAnimator != null)
            _visualAnimator.TriggerAttack();
        if (_weaponKick != null)
            _weaponKick.TriggerKick();
    }

    public void ApplyUpgrade(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;
        switch (upgradeId)
        {
            case "move_speed":
                moveSpeed = Mathf.Min(moveSpeed * 1.15f, 14f);
                break;
            case "fire_rate":
                fireCooldown = Mathf.Max(fireCooldown * 0.82f, 0.06f);
                break;
            case "bullet_speed":
                bulletSpeed = Mathf.Min(bulletSpeed * 1.2f, 48f);
                break;
        }
    }
}
