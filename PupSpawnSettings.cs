namespace dynamicpupspawns;

public class PupSpawnSettings
{
    public bool SpawnsDynamicPups { get; }

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
    
    public PupSpawnSettings(bool spawns, int min, int max, float chance)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
        SpawnChance = chance > 1f ? 1f : chance;
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
}