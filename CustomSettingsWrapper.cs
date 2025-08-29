using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomSettingsWrapper
{
    public string ModID { get; }

    private List<CustomCampaignSettings> _campaignSettings = new List<CustomCampaignSettings>();
    private List<CustomRegionSettings> _regionSettings = new List<CustomRegionSettings>();

    public CustomSettingsWrapper(string id)
    {
        ModID = id;
    }

    public void AddCampaignSettings(CustomCampaignSettings campaignSettings)
    {
        _campaignSettings.Add(campaignSettings);
    }

    public void AddRegionSettings(CustomRegionSettings regionSettings)
    {
        _regionSettings.Add(regionSettings);
    }

    public CustomCampaignSettings GetCampaign(string id)
    {
        foreach (CustomCampaignSettings settings in _campaignSettings)
        {
            if (id == settings.CampaignID)
            {
                return settings;
            }
        }

        return null;
    }

    public CustomRegionSettings GetRegion(string acronym)
    {
        foreach (CustomRegionSettings settings in _regionSettings)
        {
            if (acronym == settings.RegionAcronym)
            {
                return settings;
            }
        }

        return null;
    }

    public bool HasRegionSettings()
    {
        if (_regionSettings.Count > 0)
        {
            return true;
        }
        return false;
    }
    
    public bool HasCampaignSettings()
    {
        if (_campaignSettings.Count > 0)
        {
            return true;
        }
        return false;
    }
    
    public override string ToString()
    {
        string s = string.Format("Mod ID: {0}\n\n", ModID);
        
        s += "Campaign Settings:\n";
        foreach (CustomCampaignSettings campaignSettings in _campaignSettings)
        {
            s += campaignSettings.ToString();
            s += "___________________________\n\n";
        }
        s += "Standalone Region Settings:\n";
        foreach (CustomRegionSettings regionSettings in _regionSettings)
        {
            s += regionSettings.ToString();
            s += "___________________________\n\n";
        }
        
        return s;
    }
}