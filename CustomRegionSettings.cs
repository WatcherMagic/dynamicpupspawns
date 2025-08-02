using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomRegionSettings
{
    public string RegionAcronym { get; }
    
    private PupSpawnSettings _pupSpawnSettings;
    public PupSpawnSettings PupSpawnSettings { get => _pupSpawnSettings; }
    
    private Dictionary<string, bool> _overriddenRooms = new Dictionary<string, bool>();
    public bool HasOverriddenRooms => _overriddenRooms.Count > 0;

    public CustomRegionSettings(string acronym, PupSpawnSettings pupSpawnSettings)
    {
        RegionAcronym = acronym;
        _pupSpawnSettings = pupSpawnSettings;
    }
    
    public void AddOverriddenRoom(string name, bool spawns)
    {
        _overriddenRooms.Add(name, spawns);
    }

    public override string ToString()
    {
        string s = "CustomRegionSettings Object:\n";
        
        s += "Acronym*: " + RegionAcronym + "\n";
        s += "Spawns Dynamic Pups*: " +  PupSpawnSettings.SpawnsDynamicPups + "\n";
        s += "MinPups: " + PupSpawnSettings.MinPups + "\n";
        s += "MaxPups: " + PupSpawnSettings.MaxPups + "\n";
        s += "Room Spawning Overrides:\n";
        foreach (var kvp in _overriddenRooms)
        {
            s += kvp.Key + ": " + kvp.Value + "\n";
        }
        
        return s;
    }
}