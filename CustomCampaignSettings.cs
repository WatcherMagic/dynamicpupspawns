using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomCampaignSettings
{
    public string CampaignID { get; }
    public bool SpawnsDynamicPups { get; set; }
    public int MinPups { get; set; }
    public int MaxPups { get; set; }
    
    private List<CustomRegionSettings> _campaignRegionSettings = new List<CustomRegionSettings>();

    public CustomCampaignSettings(string id)
    {
        CampaignID = id;
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
        
        s += "Acronym: " + CampaignID + "\n";
        s += "Spawns Dynamic Pups: " +  SpawnsDynamicPups + "\n";
        s += "MinPups: " + MinPups + "\n";
        s += "MaxPups: " + MaxPups + "\n";
        s += "Campaign Region Settings:\n";
        foreach (CustomRegionSettings regionSettings in _campaignRegionSettings)
        {
            s += regionSettings.ToString();
        }
        
        return s;
    }
}