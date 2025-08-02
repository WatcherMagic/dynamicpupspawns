using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomRegionSettings
{
    public string RegionAcronym { get; }
    public bool SpawnsDynamicPups { get; }
    public int MinPups { get; set; }
    public int MaxPups { get; set; }
    private Dictionary<string, bool> _overriddenRooms = new Dictionary<string, bool>();
    public bool HasOverriddenRooms => _overriddenRooms.Count > 0;

    public CustomRegionSettings(string acronym, bool spawns)
    {
        RegionAcronym = acronym;
        SpawnsDynamicPups = spawns;
        MinPups = -1;
        MaxPups = -1;
    }

    public void AddOverriddenRoom(string name, bool spawns)
    {
        _overriddenRooms.Add(name, spawns);
    }

    public override string ToString()
    {
        string s = "CustomRegionSettings Object:\n";
        
        s += "Acronym: " + RegionAcronym + "\n";
        s += "Spawns Dynamic Pups: " +  SpawnsDynamicPups + "\n";
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