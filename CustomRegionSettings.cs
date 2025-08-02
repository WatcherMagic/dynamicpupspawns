using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomRegionSettings
{
    public string RegionAcronym { get; }
    public bool SpawnsDynamicPups { get; }
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
    private Dictionary<string, bool> _overriddenRooms = new Dictionary<string, bool>();
    public bool HasOverriddenRooms => _overriddenRooms.Count > 0;

    public CustomRegionSettings(string acronym, bool spawns)
    {
        RegionAcronym = acronym;
        SpawnsDynamicPups = spawns;
    }

    public bool SetMinAndMaxPups(int min, int max)
    {
        if (min > max)
        {
            return false;
        }
        else
        {
            _minPups = min;
            _maxPups = max;
            return true;
        }
    }
    
    public void AddOverriddenRoom(string name, bool spawns)
    {
        _overriddenRooms.Add(name, spawns);
    }

    public override string ToString()
    {
        string s = "CustomRegionSettings Object:\n";
        
        s += "Acronym*: " + RegionAcronym + "\n";
        s += "Spawns Dynamic Pups*: " +  SpawnsDynamicPups + "\n";
        s += "MinPups: " + MinPups + "\n";
        s += "MaxPups: " + MaxPups + "\n";
        s += "Room Spawning Overrides:\n";
        foreach (var kvp in _overriddenRooms)
        {
            s += kvp.Key + ": " + kvp.Value + "\n";
        }
        
        return s;
    }
}