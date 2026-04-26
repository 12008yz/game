using UnityEngine;
using System.IO;

public class PerformanceProbe3D : MonoBehaviour
{
    public static PerformanceProbe3D Instance { get; private set; }
    public bool IsBenchmarkRunning => _benchRunning;

    [SerializeField] bool enableOverlay = true;
    [SerializeField] bool logEvery10Seconds = true;

    float _timeAccum;
    int _frames;
    float _worstFrameMs;
    float _logTimer;
    long _gc0Start;
    long _gc1Start;
    long _gc2Start;
    string _cachedText = string.Empty;
    float _benchTimer;
    float _benchAccumFps;
    float _benchWorstMs;
    int _benchSamples;
    long _benchGc0Start;
    long _benchGc1Start;
    long _benchGc2Start;
    bool _benchRunning;
    string _benchCsvPath;

    void Start()
    {
        Instance = this;
        _gc0Start = System.GC.CollectionCount(0);
        _gc1Start = System.GC.CollectionCount(1);
        _gc2Start = System.GC.CollectionCount(2);
        _benchCsvPath = Path.Combine(Application.persistentDataPath, "benchmark-results.csv");
        EnsureCsvHeader();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _frames++;
        _timeAccum += dt;
        float frameMs = dt * 1000f;
        if (frameMs > _worstFrameMs) _worstFrameMs = frameMs;

        if (_timeAccum >= 1f)
        {
            float fps = _frames / _timeAccum;
            long memMb = System.GC.GetTotalMemory(false) / (1024 * 1024);
            long gc0 = System.GC.CollectionCount(0) - _gc0Start;
            long gc1 = System.GC.CollectionCount(1) - _gc1Start;
            long gc2 = System.GC.CollectionCount(2) - _gc2Start;
            _cachedText = $"FPS {fps:0.0} | Worst {_worstFrameMs:0.0}ms | GC[{gc0}/{gc1}/{gc2}] | Mem {memMb}MB";
            _timeAccum = 0f;
            _frames = 0;
            _worstFrameMs = 0f;
        }

        if (_benchRunning && dt > 0f)
        {
            float fps = 1f / dt;
            _benchAccumFps += fps;
            _benchSamples++;
            if (frameMs > _benchWorstMs) _benchWorstMs = frameMs;
            _benchTimer -= dt;
            if (_benchTimer <= 0f)
                FinishBenchmark();
        }

        if (!logEvery10Seconds) return;
        _logTimer += dt;
        if (_logTimer >= 10f)
        {
            _logTimer = 0f;
            Debug.Log($"[PerformanceProbe3D] {_cachedText}");
        }
    }

    void OnGUI()
    {
        if (!enableOverlay || string.IsNullOrEmpty(_cachedText)) return;
        GUI.Label(new Rect(10f, 10f, 800f, 26f), _cachedText);
    }

    public void StartBenchmark(float seconds)
    {
        _benchTimer = Mathf.Max(1f, seconds);
        _benchAccumFps = 0f;
        _benchWorstMs = 0f;
        _benchSamples = 0;
        _benchGc0Start = System.GC.CollectionCount(0);
        _benchGc1Start = System.GC.CollectionCount(1);
        _benchGc2Start = System.GC.CollectionCount(2);
        _benchRunning = true;
        Debug.Log($"[PerformanceProbe3D] Benchmark started for {_benchTimer:0.0}s");
    }

    void FinishBenchmark()
    {
        _benchRunning = false;
        float avgFps = _benchSamples > 0 ? _benchAccumFps / _benchSamples : 0f;
        long gc0 = System.GC.CollectionCount(0) - _benchGc0Start;
        long gc1 = System.GC.CollectionCount(1) - _benchGc1Start;
        long gc2 = System.GC.CollectionCount(2) - _benchGc2Start;
        string tier = Bootstrap3D.CurrentTier.ToString();
        Debug.Log($"[PerformanceProbe3D] Benchmark done | Tier {tier} | AvgFPS {avgFps:0.0} | Worst {_benchWorstMs:0.0}ms | GC[{gc0}/{gc1}/{gc2}]");
        AppendCsvRow(avgFps, _benchWorstMs, gc0, gc1, gc2, tier);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureCsvHeader()
    {
        if (string.IsNullOrEmpty(_benchCsvPath)) return;
        if (File.Exists(_benchCsvPath)) return;
        File.WriteAllText(_benchCsvPath, "utc_time,quality_tier,avg_fps,worst_ms,gc0,gc1,gc2\n");
    }

    void AppendCsvRow(float avgFps, float worstMs, long gc0, long gc1, long gc2, string tier)
    {
        if (string.IsNullOrEmpty(_benchCsvPath)) return;
        string row = $"{System.DateTime.UtcNow:O},{tier},{avgFps:0.0},{worstMs:0.0},{gc0},{gc1},{gc2}\n";
        File.AppendAllText(_benchCsvPath, row);
        Debug.Log($"[PerformanceProbe3D] Benchmark saved: {_benchCsvPath}");
    }
}
