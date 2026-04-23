using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager3D : MonoBehaviour
{
    public static GameManager3D Instance { get; private set; }

    [SerializeField] int desiredTotalEnemies = 18;
    [SerializeField] int maxAliveAtOnce = 6;
    [SerializeField] int spawnBatchMin = 3;
    [SerializeField] int spawnBatchMax = 6;
    [SerializeField] float introMessageDuration = 7f;

    bool _gameOver;
    bool _gameStarted;
    bool _win;
    bool _portalSpawned;
    int _kills;
    int _spawnedEnemies;
    int _targetTotalEnemies;
    float _introTimer;

    public bool GameOver => _gameOver;
    public bool GameStarted => _gameStarted;
    public bool Win => _win;
    public int Kills => _kills;
    public int SpawnedEnemies => _spawnedEnemies;
    public int TargetTotalEnemies => _targetTotalEnemies;
    public int EnemiesRemaining => Mathf.Max(0, _targetTotalEnemies - _kills);
    public float IntroTimer => _introTimer;
    public bool PortalSpawned => _portalSpawned;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 0f;
        _introTimer = introMessageDuration;
        ConfigureEnemyBudget();
    }

    public void NotifyEnemyKilled()
    {
        if (!_gameStarted || _gameOver) return;
        _kills++;
        if (_kills >= _targetTotalEnemies && AliveEnemiesCount() == 0)
        {
            SpawnVictoryPortal();
            return;
        }

        TrySpawnEnemies();
    }

    public void NotifyPlayerDied()
    {
        if (!_gameStarted || _gameOver) return;
        Time.timeScale = 1f;
        _gameOver = true;
        _win = false;
    }

    void Update()
    {
        if (!_gameStarted)
            return;

        if (_gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (_introTimer > 0f)
            _introTimer -= Time.deltaTime;

        TrySpawnEnemies();

        var player = FindFirstObjectByType<PlayerController3D>();
        if (!_portalSpawned && player != null && player.AmmoRemaining <= 0 && AliveEnemiesCount() > 0)
        {
            _gameOver = true;
            _win = false;
        }
    }

    public void StartGameFromMenu()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        _introTimer = 0f;
        Time.timeScale = 1f;
        TrySpawnEnemies();
    }

    void ConfigureEnemyBudget()
    {
        var player = FindFirstObjectByType<PlayerController3D>();
        int ammoCap = player != null ? player.MaxAmmo : 20;
        int safeMax = Mathf.Max(1, ammoCap - 2);
        _targetTotalEnemies = Mathf.Clamp(desiredTotalEnemies, 8, safeMax);
    }

    void TrySpawnEnemies()
    {
        if (_portalSpawned) return;
        if (_spawnedEnemies >= _targetTotalEnemies) return;

        int alive = AliveEnemiesCount();
        if (alive >= maxAliveAtOnce) return;
        if (alive > 2) return;

        int remaining = _targetTotalEnemies - _spawnedEnemies;
        int batch = Mathf.Clamp(remaining, spawnBatchMin, spawnBatchMax);
        batch = Mathf.Min(batch, maxAliveAtOnce - alive);
        if (batch <= 0) return;

        var level = FindFirstObjectByType<LevelBuilder>();
        if (level == null) return;

        level.SpawnEnemyWave(batch);
        _spawnedEnemies += batch;
    }

    void SpawnVictoryPortal()
    {
        if (_portalSpawned) return;
        _portalSpawned = true;

        var player = FindFirstObjectByType<PlayerController3D>();
        Vector3 pos = player != null ? player.transform.position + player.transform.forward * 3f : new Vector3(0f, 0f, 0f);
        pos.y = 0f;

        var go = new GameObject("VictoryPortal");
        go.transform.position = pos;
        go.AddComponent<VictoryPortal3D>();
    }

    public void NotifyPortalReached()
    {
        if (!_gameStarted || _gameOver) return;
        _gameOver = true;
        _win = true;
    }

    int AliveEnemiesCount()
    {
        return FindObjectsByType<ChaserEnemy3D>(FindObjectsSortMode.None).Length;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
