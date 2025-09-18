using System.Collections.Generic;

namespace dynamicpupspawns;

public class CustomSettingsObject
{
    public enum SettingsType
    {
        Campaign,
        Region
    }

    public SettingsType Type { get => _type; }
    private SettingsType _type;
    
    public string ID { get; set; }
    
    private PupSpawnSettings _pupSpawnSettings = new PupSpawnSettings();
    public PupSpawnSettings PupSpawnSettings { get => _pupSpawnSettings; }
    
    private List<CustomSettingsObject> _overrides = new List<CustomSettingsObject>();

    public CustomSettingsObject(SettingsType t, string id)
    {
        _type = t;
        ID = id;
    }

    public CustomSettingsObject(SettingsType t, string id, PupSpawnSettings pupSettings)
    {
        _type = t;
        ID = id;
        _pupSpawnSettings = pupSettings;
    }
    
    public bool AddOverride(CustomSettingsObject over)
    {
        if (Type == SettingsType.Campaign)
        {
            if (over.Type == SettingsType.Region)
            {
                _overrides.Add(over);
                return true;
            }
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
            Type, ID);
        foreach (CustomSettingsObject ob in _overrides)
        {
            s += ob.ToString();
            s += "---------------------------\n";
        }
        
        return s;
    }
}