using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomSettingsWrapper
{
    public string ModID { get; }

    private List<CustomSettingsObject> _campaignSettings = new List<CustomSettingsObject>();
    private List<CustomSettingsObject> _regionSettings = new List<CustomSettingsObject>();

    public CustomSettingsWrapper(string id)
    {
        ModID = id;
    }

    public bool AddSetting(CustomSettingsObject settings)
    {
        if (settings.SettingType == CustomSettingsObject.SettingsType.Campaign)
        {
            _campaignSettings.Add(settings);
            return true;
        }
        
        if (settings.SettingType == CustomSettingsObject.SettingsType.Region)
        {
            _regionSettings.Add(settings);
            return true;
        }
        
        return false;
    }

    public CustomSettingsObject GetSettings(CustomSettingsObject.SettingsType t, string id)
    {
        if (t == CustomSettingsObject.SettingsType.Campaign)
        {
            foreach (CustomSettingsObject campaign in _campaignSettings)
            {
                if (campaign.ID == id)
                {
                    return campaign;
                }
            }
        }

        if (t == CustomSettingsObject.SettingsType.Region)
        {
            foreach (CustomSettingsObject region in _regionSettings)
            {
                if (region.ID == id)
                {
                    return region;
                }
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

    public List<string> GetAllSettingsIDs()
    {
        List<string> ids = new List<string>();

        foreach (CustomSettingsObject campaignSetting in _campaignSettings)
        {
            ids.Add(campaignSetting.ID);
        }

        foreach (CustomSettingsObject regionSetting in _regionSettings)
        {
            ids.Add(regionSetting.ID);
        }

        return ids;
    }
    
    public override string ToString()
    {
        string s = string.Format("Mod ID: {0}\n\n", ModID);
        
        s += "CAMPAIGNS:\n";
        foreach (CustomSettingsObject campaignSettings in _campaignSettings)
        {
            s += campaignSettings.ToString();
            s += "___________________________\n\n";
        }
        s += "REGIONS:\n";
        foreach (CustomSettingsObject regionSettings in _regionSettings)
        {
            s += regionSettings.ToString();
            s += "___________________________\n\n";
        }
        
        return s;
    }
}