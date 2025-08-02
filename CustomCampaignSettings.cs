using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomCampaignSettings
{
    public string CampaignID { get; }
    public bool SpawnsDynamicPups { get; set; }
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
    
    private List<CustomRegionSettings> _campaignRegionSettings = new List<CustomRegionSettings>();

    public CustomCampaignSettings(string id)
    {
        CampaignID = id;
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