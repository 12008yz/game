using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry3D : MonoBehaviour
{
    static EnemyRegistry3D _instance;
    readonly HashSet<ChaserEnemy3D> _alive = new HashSet<ChaserEnemy3D>();

    public static int AliveCount => _instance != null ? _instance._alive.Count : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetStatic()
    {
        _instance = null;
    }

    public static void EnsureExists()
    {
        if (_instance != null) return;
        var go = new GameObject("EnemyRegistry3D");
        _instance = go.AddComponent<EnemyRegistry3D>();
    }

    public static void Register(ChaserEnemy3D enemy)
    {
        if (enemy == null) return;
        EnsureExists();
        _instance._alive.Add(enemy);
    }

    public static void Unregister(ChaserEnemy3D enemy)
    {
        if (_instance == null || enemy == null) return;
        _instance._alive.Remove(enemy);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        _alive.Clear();
    }
}
