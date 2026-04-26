using System;
using System.IO;
using UnityEngine;

[Serializable]
public class MetaProgressionData3D
{
    public int totalCurrency;
    public int runCount;
    public int damageTier;
    public int survivabilityTier;
}

public static class MetaProgression3D
{
    static MetaProgressionData3D _cached;
    static string SavePath => Path.Combine(Application.persistentDataPath, "meta-progression-3d.json");

    public static MetaProgressionData3D Data
    {
        get
        {
            if (_cached == null)
                _cached = Load();
            return _cached;
        }
    }

    public static void AddRunReward(int amount)
    {
        var d = Data;
        d.totalCurrency += Mathf.Max(0, amount);
        d.runCount += 1;
        Save(d);
    }

    public static bool TrySpend(int amount)
    {
        var d = Data;
        if (amount <= 0 || d.totalCurrency < amount) return false;
        d.totalCurrency -= amount;
        Save(d);
        return true;
    }

    static MetaProgressionData3D Load()
    {
        try
        {
            if (!File.Exists(SavePath))
                return new MetaProgressionData3D();
            string json = File.ReadAllText(SavePath);
            var loaded = JsonUtility.FromJson<MetaProgressionData3D>(json);
            return loaded ?? new MetaProgressionData3D();
        }
        catch
        {
            return new MetaProgressionData3D();
        }
    }

    static void Save(MetaProgressionData3D data)
    {
        if (data == null) return;
        _cached = data;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch
        {
            // keep runtime going even if disk write fails
        }
    }
}
