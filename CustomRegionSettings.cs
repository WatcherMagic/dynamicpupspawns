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
        string s = string.Format(
            "Acronym: {0}\n" +
            PupSpawnSettings.ToString() + "\n", RegionAcronym);
        
        return s;
    }
}