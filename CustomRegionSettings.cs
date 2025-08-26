using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomRegionSettings
{
    public string RegionAcronym { get; }
    
    private PupSpawnSettings _pupSpawnSettings;
    public PupSpawnSettings PupSpawnSettings { get => _pupSpawnSettings; }
    
    public CustomRegionSettings(string acronym, PupSpawnSettings pupSpawnSettings)
    {
        RegionAcronym = acronym;
        _pupSpawnSettings = pupSpawnSettings;
    }

    public override string ToString()
    {
        string s = "CustomRegionSettings Object:\n";
        
        s += "Acronym: " + RegionAcronym + "\n";
        s += "Spawns Dynamic Pups: " +  PupSpawnSettings.SpawnsDynamicPups + "\n";
        s += "MinPups: " + PupSpawnSettings.MinPups + "\n";
        s += "MaxPups: " + PupSpawnSettings.MaxPups + "\n";
        s += "SpawnChance: " + PupSpawnSettings.SpawnChance.ToString("P") + "\n";
        
        return s;
    }
}