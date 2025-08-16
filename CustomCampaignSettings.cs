using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomCampaignSettings
{
    public string CampaignID { get; set; }
    private PupSpawnSettings _pupSpawnSettings;
    public PupSpawnSettings PupSpawnSettings { get => _pupSpawnSettings; }
    
    private List<CustomRegionSettings> _campaignRegionSettings = new List<CustomRegionSettings>();
    
    public CustomCampaignSettings(string id, PupSpawnSettings pupSpawnSettings)
    {
        CampaignID = id;
        _pupSpawnSettings = pupSpawnSettings;
    }

    public void AddCampaignRegionSettings(CustomRegionSettings regionSettings)
    {
        _campaignRegionSettings.Add(regionSettings);
    }
    
    public CustomRegionSettings GetCampaignRegion(string acronym)
    {
        foreach (CustomRegionSettings settings in _campaignRegionSettings)
        {
            if (acronym == settings.RegionAcronym)
            {
                return settings;
            }
        }

        return null;
    }
    
    public override string ToString()
    {
        string s = "CustomCampaignSettings Object:\n";
        
        s += "Campaign ID*: " + CampaignID + "\n";
        s += "Spawns Dynamic Pups: " +  PupSpawnSettings.SpawnsDynamicPups + "\n";
        s += "MinPups: " + PupSpawnSettings.MinPups + "\n";
        s += "MaxPups: " + PupSpawnSettings.MaxPups + "\n";
        s += "Campaign Region Settings:\n";
        foreach (CustomRegionSettings regionSettings in _campaignRegionSettings)
        {
            s += regionSettings.ToString();
        }
        
        return s;
    }
}