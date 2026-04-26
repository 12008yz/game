using UnityEngine;

public class StressBenchmark3D : MonoBehaviour
{
    [SerializeField] KeyCode startKey = KeyCode.F6;
    [SerializeField] KeyCode lowQualityKey = KeyCode.F7;
    [SerializeField] KeyCode mediumQualityKey = KeyCode.F8;
    [SerializeField] KeyCode highQualityKey = KeyCode.F9;
    [SerializeField] float benchmarkDuration = 25f;
    [SerializeField] float spawnInterval = 1.5f;
    [SerializeField] int spawnPerTick = 3;

    float _timer;
    float _spawnTimer;
    bool _running;

    void Update()
    {
        HandleQualityHotkeys();

        if (!_running)
        {
            if (Input.GetKeyDown(startKey))
                StartRun();
            return;
        }

        _timer -= Time.deltaTime;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = spawnInterval;
            var gm = GameManager3D.Instance;
            if (gm != null)
                gm.SpawnDebugWave(spawnPerTick);
        }

        if (_timer <= 0f)
        {
            _running = false;
            Debug.Log("[StressBenchmark3D] Stress benchmark finished.");
        }
    }

    void StartRun()
    {
        var gm = GameManager3D.Instance;
        if (gm == null || !gm.GameStarted || gm.GameOver)
        {
            Debug.Log("[StressBenchmark3D] Start game first, then run benchmark.");
            return;
        }

        _running = true;
        _timer = Mathf.Max(3f, benchmarkDuration);
        _spawnTimer = 0f;
        var probe = PerformanceProbe3D.Instance;
        if (probe != null)
            probe.StartBenchmark(_timer);
        Debug.Log($"[StressBenchmark3D] Running for {_timer:0.0}s. Press {startKey} to repeat after finish.");
    }

    void HandleQualityHotkeys()
    {
        if (Input.GetKeyDown(lowQualityKey))
            Bootstrap3D.SetRuntimeQualityTier(Bootstrap3D.RuntimeQualityTier.Low);
        else if (Input.GetKeyDown(mediumQualityKey))
            Bootstrap3D.SetRuntimeQualityTier(Bootstrap3D.RuntimeQualityTier.Medium);
        else if (Input.GetKeyDown(highQualityKey))
            Bootstrap3D.SetRuntimeQualityTier(Bootstrap3D.RuntimeQualityTier.High);
    }
}
