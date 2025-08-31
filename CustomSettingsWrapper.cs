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

    public bool AddNewSettings(CustomSettingsObject settings)
    {
        if (settings.Type == CustomSettingsObject.ObjectType.Campaign)
        {
            _campaignSettings.Add(settings);
            return true;
        }
        
        if (settings.Type == CustomSettingsObject.ObjectType.Region)
        {
            _regionSettings.Add(settings);
            return true;
        }
        
        return false;
    }

    public CustomSettingsObject GetSettings(CustomSettingsObject.ObjectType t, string id)
    {
        if (t == CustomSettingsObject.ObjectType.Campaign)
        {
            foreach (CustomSettingsObject campaign in _campaignSettings)
            {
                if (campaign.ID == id)
                {
                    return campaign;
                }
            }
        }

        if (t == CustomSettingsObject.ObjectType.Region)
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