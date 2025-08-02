namespace dynamicpupspawns;

public struct PupSpawnSettings
{
    public bool SpawnsDynamicPups { get; set; }

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
    
    public PupSpawnSettings(bool spawns, int min, int max)
    {
        SpawnsDynamicPups = spawns;
        _setMinMaxSucceeded = SetMinAndMaxPups(min, max);
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
}