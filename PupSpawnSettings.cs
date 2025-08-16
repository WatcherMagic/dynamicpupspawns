namespace dynamicpupspawns;

public class PupSpawnSettings
{
    public bool SpawnsDynamicPups { get; set; }

    private bool _setSpawnChanceSucceeded = true;

    public bool SetSpawnChanceSucceeded
    {
        get => _setSpawnChanceSucceeded;
    }
    private float _spawnChance = 1f;
    public float SpawnChance { get => _spawnChance; }

    private bool _setMinMaxSucceeded = true;
    public bool SetMinMaxSucceeded
    {
        get => _setMinMaxSucceeded;
    }
    
    private int _minPups = -1;
    public int MinPups
    {
        get => _minPups;
    }
    private int _maxPups = -1;
    public int MaxPups
    {
        get => _maxPups;
    }
    
    public PupSpawnSettings(bool spawns)
    {
        SpawnsDynamicPups = spawns;
    }
    
    public PupSpawnSettings(bool spawns, float chance)
    {
        SpawnsDynamicPups = spawns;
        SetSpawnChance(chance);
    }
    
    public PupSpawnSettings(bool spawns, int min, int max)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
    }
    
    public PupSpawnSettings(bool spawns, int min, int max, float chance)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
        SetSpawnChance(chance);
    }
    
    public bool SetMinAndMaxPups(int min, int max)
    {
        if (min > max)
        {
            _setMinMaxSucceeded = false;
            return SetMinMaxSucceeded;
        }
        _minPups = min;
        _maxPups = max;
        _setMinMaxSucceeded = true;
        return SetMinMaxSucceeded;
    }

    public bool SetSpawnChance(float chance)
    {
        if (0f <= chance && chance <= 1f)
        {
            _setSpawnChanceSucceeded = true;
            return SetSpawnChanceSucceeded;
        }

        _setSpawnChanceSucceeded = false;
        return SetSpawnChanceSucceeded;
    }
}