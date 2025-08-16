namespace dynamicpupspawns;

public class PupSpawnSettings
{
    public bool SpawnsDynamicPups { get; set; }

    public float SpawnChance { get; set; }

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
    public PupSpawnSettings(bool spawns, int min, int max, float chance)
    {
        SpawnsDynamicPups = spawns;
        SetMinAndMaxPups(min, max);
        if (chance > 1f)
        {
            SpawnChance = 1f;
        }
        else
        {
            SpawnChance = chance;
        }
    }
    
    public void SetMinAndMaxPups(int min, int max)
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