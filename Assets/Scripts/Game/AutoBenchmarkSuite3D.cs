using UnityEngine;

public class AutoBenchmarkSuite3D : MonoBehaviour
{
    [SerializeField] KeyCode suiteKey = KeyCode.F10;
    [SerializeField] float intervalBetweenRuns = 2f;
    int _step = -1;
    float _timer;
    bool _running;

    void Update()
    {
        if (!_running)
        {
            if (Input.GetKeyDown(suiteKey))
                StartSuite();
            return;
        }

        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        Advance();
    }

    void StartSuite()
    {
        var gm = GameManager3D.Instance;
        if (gm == null || !gm.GameStarted || gm.GameOver)
        {
            Debug.Log("[AutoBenchmarkSuite3D] Start a run first.");
            return;
        }
        _running = true;
        _step = -1;
        _timer = 0f;
        Debug.Log("[AutoBenchmarkSuite3D] Starting Low/Medium/High benchmark suite.");
    }

    void Advance()
    {
        _step++;
        if (_step == 0)
            LaunchTier(Bootstrap3D.RuntimeQualityTier.Low);
        else if (_step == 1)
            LaunchTier(Bootstrap3D.RuntimeQualityTier.Medium);
        else if (_step == 2)
            LaunchTier(Bootstrap3D.RuntimeQualityTier.High);
        else
        {
            _running = false;
            Debug.Log("[AutoBenchmarkSuite3D] Suite complete.");
        }
    }

    void LaunchTier(Bootstrap3D.RuntimeQualityTier tier)
    {
        Bootstrap3D.SetRuntimeQualityTier(tier);
        var probe = PerformanceProbe3D.Instance;
        if (probe != null)
            probe.StartBenchmark(15f);
        var gm = GameManager3D.Instance;
        if (gm != null)
            gm.SpawnDebugWave(6);
        _timer = 15f + intervalBetweenRuns;
    }
}
