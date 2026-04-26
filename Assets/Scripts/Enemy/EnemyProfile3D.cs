using UnityEngine;

public enum EnemyRole3D
{
    Grunt,
    Fast,
    Tank,
    Ranged,
    Elite,
    Boss
}

[CreateAssetMenu(menuName = "Game/Enemy Profile 3D", fileName = "EnemyProfile3D")]
public class EnemyProfile3D : ScriptableObject
{
    public string profileId = "enemy";
    public EnemyRole3D role = EnemyRole3D.Grunt;
    public float speed = 2.3f;
    public int hp = 1;
    public float attackDistance = 2.2f;
    public float rangedInterval = 1.4f;
    public float scale = 1f;
}
