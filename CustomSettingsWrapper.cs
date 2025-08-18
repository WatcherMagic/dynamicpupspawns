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
            if (id.ToLower() == settings.CampaignID.ToLower())
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
            if (acronym.ToLower() == settings.RegionAcronym.ToLower())
            {
                return settings;
            }
        }

        return null;
    }
    
    public override string ToString()
    {
        string s = "CustomSettingsWrapper Object:\n";
        
        s += "Mod ID: " + ModID + "\n";
        s += "Campaign-specific settings:\n";
        foreach (CustomCampaignSettings campaignSettings in _campaignSettings)
        {
            s += campaignSettings.ToString();
        }
        s += "Standalone region settings:\n";
        foreach (CustomRegionSettings regionSettings in _regionSettings)
        {
            s += regionSettings.ToString();
        }
        
        return s;
    }
}