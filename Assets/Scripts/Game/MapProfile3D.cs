using UnityEngine;

public enum MapBiome3D
{
    Urban,
    Industrial,
    Ruins
}

[CreateAssetMenu(menuName = "Game/Map Profile 3D", fileName = "MapProfile3D")]
public class MapProfile3D : ScriptableObject
{
    public string profileId = "map";
    public MapBiome3D biome = MapBiome3D.Urban;
    public int obstacleCount = 11;
    public int buildingCount = 4;
    public int chestCount = 7;
    public Color floorColor = new Color(0.1f, 0.12f, 0.18f);
    public Color wallColor = new Color(0.32f, 0.34f, 0.42f);
}
