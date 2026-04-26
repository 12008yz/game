using UnityEngine;

[CreateAssetMenu(menuName = "Game/Balance 3D", fileName = "GameBalance3D")]
public class GameBalance3D : ScriptableObject
{
    [Header("Player")]
    public float playerMoveSpeed = 7f;
    public float playerFireCooldown = 0.18f;
    public float playerBulletSpeed = 22f;
    public int playerMaxAmmo = 80;

    [Header("Enemies")]
    public float enemyMoveSpeed = 2.3f;
    public int enemyHp = 1;
    public int desiredTotalEnemies = 18;
    public int maxAliveAtOnce = 6;
}
