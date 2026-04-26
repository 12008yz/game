using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager3D : MonoBehaviour
{
    public static GameManager3D Instance { get; private set; }

    [SerializeField] GameBalance3D balance;
    [SerializeField] int desiredTotalEnemies = 18;
    [SerializeField] int maxAliveAtOnce = 6;
    [SerializeField] int spawnBatchMin = 3;
    [SerializeField] int spawnBatchMax = 6;
    [SerializeField] float introMessageDuration = 7f;
    [SerializeField] int bossWaveIndex = 5;

    bool _gameOver;
    bool _gameStarted;
    bool _win;
    bool _portalSpawned;
    int _kills;
    int _spawnedEnemies;
    int _targetTotalEnemies;
    float _introTimer;
    float _cachedRefsRefreshTimer;
    float _loseCheckTimer;
    PlayerController3D _player;
    LevelBuilder _level;
    RunState3D _runState = RunState3D.Menu;
    int _waveIndex;
    int _waveKills;
    int _waveTarget;
    int _waveSpawned;
    bool _bossSpawned;
    string[] _upgradeChoices = { "fire_rate", "move_speed", "ammo_pack" };
    int _sessionCurrency;
    float _upgradeAutoPickTimer;

    public event System.Action<int> OnWaveStarted;
    public event System.Action<int> OnWaveCleared;
    public event System.Action<string> OnUpgradeChosen;
    public event System.Action<bool, int> OnRunFinished;

    public bool GameOver => _gameOver;
    public bool GameStarted => _gameStarted;
    public bool Win => _win;
    public int Kills => _kills;
    public int SpawnedEnemies => _spawnedEnemies;
    public int TargetTotalEnemies => _targetTotalEnemies;
    public int EnemiesRemaining => _gameStarted ? Mathf.Max(0, _waveTarget - _waveKills) : Mathf.Max(0, _targetTotalEnemies - _kills);
    public float IntroTimer => _introTimer;
    public bool PortalSpawned => _portalSpawned;
    public RunState3D RunState => _runState;
    public int WaveIndex => _waveIndex;
    public int WaveTarget => _waveTarget;
    public int WaveKills => _waveKills;
    public string[] UpgradeChoices => _upgradeChoices;
    public int SessionCurrency => _sessionCurrency;

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
        if (balance == null)
            balance = Resources.Load<GameBalance3D>("GameBalance3D");
        if (balance != null)
        {
            desiredTotalEnemies = balance.desiredTotalEnemies;
            maxAliveAtOnce = balance.maxAliveAtOnce;
        }
        Time.timeScale = 0f;
        _introTimer = introMessageDuration;
        RefreshCachedReferences(force: true);
        ConfigureEnemyBudget();
        _runState = RunState3D.Menu;
    }

    public void NotifyEnemyKilled()
    {
        if (!_gameStarted || _gameOver) return;
        _kills++;
        _waveKills++;
        _sessionCurrency += 1;

        if (_waveKills >= _waveTarget && AliveEnemiesCount() == 0)
        {
            HandleWaveCleared();
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
        _runState = RunState3D.Result;
        FinishRun(win: false);
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

        if (_runState == RunState3D.UpgradeChoice)
        {
            _upgradeAutoPickTimer -= Time.unscaledDeltaTime;
            if (_upgradeAutoPickTimer <= 0f)
                SelectUpgrade(0);
            HandleUpgradeInput();
            return;
        }

        if (_introTimer > 0f)
            _introTimer -= Time.deltaTime;

        _cachedRefsRefreshTimer -= Time.deltaTime;
        if (_cachedRefsRefreshTimer <= 0f)
            RefreshCachedReferences(force: false);

        TrySpawnEnemies();

        _loseCheckTimer -= Time.deltaTime;
        if (_loseCheckTimer <= 0f)
        {
            _loseCheckTimer = 0.15f;
            if (!_portalSpawned && _player != null && _player.AmmoRemaining <= 0 && AliveEnemiesCount() > 0)
            {
                _gameOver = true;
                _win = false;
                _runState = RunState3D.Result;
                FinishRun(win: false);
            }
        }
    }

    public void StartGameFromMenu()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        _kills = 0;
        _spawnedEnemies = 0;
        _sessionCurrency = 0;
        _bossSpawned = false;
        _introTimer = 0f;
        Time.timeScale = 1f;
        _runState = RunState3D.Running;
        _waveIndex = 1;
        BeginWave(_waveIndex);
        TrySpawnEnemies();
    }

    void ConfigureEnemyBudget()
    {
        int ammoCap = _player != null ? _player.MaxAmmo : 20;
        int safeMax = Mathf.Max(1, ammoCap - 2);
        _targetTotalEnemies = Mathf.Clamp(desiredTotalEnemies, 8, safeMax);
    }

    void TrySpawnEnemies()
    {
        if (_runState == RunState3D.UpgradeChoice || _runState == RunState3D.Result) return;
        if (_portalSpawned) return;
        if (_waveSpawned >= _waveTarget) return;

        int alive = AliveEnemiesCount();
        if (alive >= maxAliveAtOnce) return;
        if (alive > 2) return;

        int remaining = _waveTarget - _waveSpawned;
        int batch = Mathf.Clamp(remaining, spawnBatchMin, spawnBatchMax);
        batch = Mathf.Min(batch, maxAliveAtOnce - alive);
        if (batch <= 0) return;

        if (_level == null) return;

        _level.SpawnEnemyWave(batch);
        _spawnedEnemies += batch;
        _waveSpawned += batch;
    }

    void SpawnVictoryPortal()
    {
        if (_portalSpawned) return;
        _portalSpawned = true;

        Vector3 pos = _player != null ? _player.transform.position + _player.transform.forward * 3f : new Vector3(0f, 0f, 0f);
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
        _runState = RunState3D.Result;
        FinishRun(win: true);
    }

    public bool SpawnDebugWave(int count)
    {
        if (!_gameStarted || _gameOver) return false;
        if (_level == null) return false;
        int safeCount = Mathf.Clamp(count, 1, 48);
        _level.SpawnEnemyWave(safeCount);
        return true;
    }

    void BeginWave(int wave)
    {
        _waveIndex = Mathf.Max(1, wave);
        _waveKills = 0;
        _waveSpawned = 0;
        _waveTarget = 6 + _waveIndex * 2;
        if (_waveIndex >= bossWaveIndex)
        {
            _runState = RunState3D.BossWave;
            _waveTarget = Mathf.Max(_waveTarget, 14);
            if (!_bossSpawned && _level != null)
            {
                _bossSpawned = true;
                _level.SpawnEnemyWave(1, EnemyRole3D.Boss);
                _spawnedEnemies += 1;
                _waveSpawned += 1;
            }
        }
        else
        {
            _runState = RunState3D.Running;
        }
        OnWaveStarted?.Invoke(_waveIndex);
    }

    void HandleWaveCleared()
    {
        OnWaveCleared?.Invoke(_waveIndex);
        if (_runState == RunState3D.BossWave)
        {
            SpawnVictoryPortal();
            return;
        }

        RollUpgradeChoices();
        _runState = RunState3D.UpgradeChoice;
        _upgradeAutoPickTimer = 12f;
        Time.timeScale = 0f;
    }

    void RollUpgradeChoices()
    {
        string[] pool = { "fire_rate", "move_speed", "bullet_speed", "ammo_pack", "damage_boost" };
        for (int i = 0; i < _upgradeChoices.Length; i++)
            _upgradeChoices[i] = pool[Random.Range(0, pool.Length)];
    }

    void HandleUpgradeInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectUpgrade(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectUpgrade(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectUpgrade(2);
    }

    void SelectUpgrade(int idx)
    {
        if (idx < 0 || idx >= _upgradeChoices.Length) return;
        string id = _upgradeChoices[idx];
        if (_player != null)
            _player.ApplyUpgrade(id);
        if (_player != null)
        {
            var wc = _player.GetComponent<WeaponController3D>();
            if (wc != null)
                wc.RefillAmmo(8);
        }
        OnUpgradeChosen?.Invoke(id);
        Time.timeScale = 1f;
        BeginWave(_waveIndex + 1);
        TrySpawnEnemies();
    }

    void FinishRun(bool win)
    {
        int reward = win ? (_sessionCurrency + _waveIndex * 6) : (_sessionCurrency / 2 + _waveIndex * 2);
        MetaProgression3D.AddRunReward(reward);
        OnRunFinished?.Invoke(win, reward);
    }

    int AliveEnemiesCount()
    {
        return EnemyRegistry3D.AliveCount;
    }

    void RefreshCachedReferences(bool force)
    {
        _cachedRefsRefreshTimer = force ? 0.75f : 1f;
        if (_player == null)
            _player = FindFirstObjectByType<PlayerController3D>();
        if (_level == null)
            _level = FindFirstObjectByType<LevelBuilder>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
