using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomSettingsObject
{
    public enum SettingsType
    {
        Campaign,
        Region
    }

    public SettingsType SettingType { get => _settingType; }
    private SettingsType _settingType;
    
    public string ID { get; set; }
    
    private PupSpawnSettings _pupSpawnSettings = new PupSpawnSettings();
    public PupSpawnSettings PupSpawnSettings { get => _pupSpawnSettings; }
    
    private List<CustomSettingsObject> _overrides = new List<CustomSettingsObject>();

    public CustomSettingsObject(SettingsType t, string id)
    {
        _settingType = t;
        ID = id;
    }

    public CustomSettingsObject(SettingsType t, string id, PupSpawnSettings pupSettings)
    {
        _settingType = t;
        ID = id;
        _pupSpawnSettings = pupSettings;
    }
    
    public bool AddOverride(CustomSettingsObject over)
    {
        if (SettingType == SettingsType.Campaign)
        {
            if (over.SettingType == SettingsType.Region)
            {
                _overrides.Add(over);
                return true;
            }
        }

        return false;
    }

    public CustomSettingsObject GetOverride(string id)
    {
        foreach (CustomSettingsObject overrideSettings in _overrides)
        {
            if (overrideSettings.ID == id)
            {
                return overrideSettings;
            }
        }
        
        return null;
    }

    public bool HasOverrides()
    {
        if (_overrides.Count > 0)
        {
            return true;
        }

        return false;
    }
    
    public override string ToString()
    {
        string s = string.Format(
            "Type: {0}\n" +
            "ID: {1}\n" +
            PupSpawnSettings.ToString() + "\n" +
            "Overrides:\n",
            SettingType, ID);
        foreach (CustomSettingsObject ob in _overrides)
        {
            s += ob.ToString();
            s += "---------------------------\n";
        }
        
        return s;
    }
}