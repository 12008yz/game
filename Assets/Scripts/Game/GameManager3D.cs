using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager3D : MonoBehaviour
{
    public static GameManager3D Instance { get; private set; }

    bool _gameOver;
    bool _win;
    int _kills;

    public bool GameOver => _gameOver;
    public bool Win => _win;
    public int Kills => _kills;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void NotifyEnemyKilled()
    {
        _kills++;
        if (FindObjectsByType<ChaserEnemy3D>(FindObjectsSortMode.None).Length == 0)
        {
            _gameOver = true;
            _win = true;
        }
    }

    public void NotifyPlayerDied()
    {
        if (_gameOver) return;
        _gameOver = true;
        _win = false;
    }

    void Update()
    {
        if (!_gameOver) return;
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
