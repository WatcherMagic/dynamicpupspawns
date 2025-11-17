using BepInEx.Logging;

namespace dynamicpupspawns;

public class PupSpawnSettings
{
    private ManualLogSource logger = new ManualLogSource("PupSpawnSettings_Logger");
    
    public bool SpawnsDynamicPups { get; }

    private bool _persistence = true;

    public bool PersistentPups
    {
        get => _persistence;
    }
    
    public float SpawnChance { get; }

    private bool _setMinMaxSucceeded = true;
    public bool SetMinMaxSucceeded
    {
        get => _setMinMaxSucceeded;
    }
    
    private int _minPups;
    public int MinPups
    {
        get => _minPups;
    }
    private int _maxPups;
    public int MaxPups
    {
        get => _maxPups;
    }

    public PupSpawnSettings()
    {
        SpawnsDynamicPups = false;
        SetMinAndMaxPups(-1, -1);
        SpawnChance = 0f;
    }
    
    public PupSpawnSettings(bool spawns, int min, int max, float chance)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
        logger.LogInfo("chance: " + chance + "; calculating... " + "chance > 1f is " + (chance > 1f));
        SpawnChance = chance > 1f ? 1f : chance;
        logger.LogInfo("result of equation 'chance > 1f ? 1f : chance' is " + SpawnChance);
    }
    
    public PupSpawnSettings(bool spawns, int min, int max, float chance, bool persistence)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
        logger.LogInfo("chance: " + chance + "; calculating... " + "chance > 1f is " + (chance > 1f));
        SpawnChance = chance > 1f ? 1f : chance;
        logger.LogInfo("result of equation 'chance > 1f ? 1f : chance' is " + SpawnChance);
        _persistence = persistence;
    }
    
    private void SetMinAndMaxPups(int min, int max)
    {
        if (min > max)
        {
            _setMinMaxSucceeded = false;
        }
        _minPups = min;
        _maxPups = max;
        _setMinMaxSucceeded = true;
    }

    public override string ToString()
    {
        string s = string.Format(
            "Spawns Dynamic Pups: {0}\n" +
            "MinPups: {1}\n" +
            "MaxPups: {2}\n" +
            "SpawnChance: {3:P0}", SpawnsDynamicPups, MinPups, MaxPups, SpawnChance);
        
        return s;
    }
}