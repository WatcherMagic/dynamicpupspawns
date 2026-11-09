using System.Text.RegularExpressions;
using DynamicPupSpawns;

namespace DynamicSpawnsTests;

public class DPSMainTests
{
    private DPSMain _dpsMain;
    private Dictionary<string, string> _dummyContentMap;
    
    [SetUp]
    public void Setup()
    {
        _dpsMain = new();
        _dummyContentMap = new();
    }

    [Test]
    public void Parsing_ValueReturnsFloat()
    {
        string value = "value:1";

        Object o = _dpsMain.ParseValue(value);
        
        Assert.That(o is float, Is.True);
    }
    
    [Test]
    public void Parsing_ValueReturnsFalse()
    {
        string value = "value:false";
        string value2 = "value:False";
        string value3 = "value:F";
        string value4 = "value:f";

        Object o = _dpsMain.ParseValue(value);
        Object o2 = _dpsMain.ParseValue(value2);
        Object o3 = _dpsMain.ParseValue(value3);
        Object o4 = _dpsMain.ParseValue(value4);
        
        Assert.That(o is bool, Is.True, $"Returned object 'o' {value} was not a bool");
        Assert.That(o2 is bool, Is.True, $"Returned object 'o2' {value2} was not a bool");
        Assert.That(o3 is bool, Is.True, $"Returned object 'o3' {value3} was not a bool");
        Assert.That(o4 is bool, Is.True, $"Returned object 'o4' {value4} was not a bool");
        bool b = (bool)o;
        bool b2 = (bool)o2;
        bool b3 = (bool)o3;
        bool b4 = (bool)o4;
        
        Assert.That(b, Is.False, $"'{value}' failed");
        Assert.That(b2, Is.False, $"'{value2}' failed");
        Assert.That(b3, Is.False, $"'{value3}' failed");
        Assert.That(b4, Is.False, $"'{value4}' failed");
    }
    
    [Test]
    public void Parsing_ValueReturnsTrue()
    {
        string value = "value:true";
        string value2 = "value:True";
        string value3 = "value:T";
        string value4 = "value:t";

        Object o = _dpsMain.ParseValue(value);
        Object o2 = _dpsMain.ParseValue(value2);
        Object o3 = _dpsMain.ParseValue(value3);
        Object o4 = _dpsMain.ParseValue(value4);
        
        Assert.That(o is bool, Is.True, $"Returned object 'o' {value} was not a bool");
        Assert.That(o2 is bool, Is.True, $"Returned object 'o2' {value2} was not a bool");
        Assert.That(o3 is bool, Is.True, $"Returned object 'o3' {value3} was not a bool");
        Assert.That(o4 is bool, Is.True, $"Returned object 'o4' {value4} was not a bool");
        bool b = (bool)o;
        bool b2 = (bool)o2;
        bool b3 = (bool)o3;
        bool b4 = (bool)o4;
        
        Assert.That(b, Is.True, $"'{value}' failed");
        Assert.That(b2, Is.True, $"'{value2}' failed");
        Assert.That(b3, Is.True, $"'{value3}' failed");
        Assert.That(b4, Is.True, $"'{value4}' failed");
    }

    [Test]
    public void Parsing_ValueReturnsString()
    {
        string value = "value:coolID";

        Object o = _dpsMain.ParseValue(value);
        
        Assert.That(o is string, Is.True);
    }

    [Test]
    public void Parsing_UnlabeledValueReturnsNull()
    {
        string value = ":coolID";

        Object o = _dpsMain.ParseValue(value);
        
        Assert.That(o, Is.Null);
    }
    
    [Test]
    public void Settings_IsolatedPupSettingsParsed()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyPupSpawns.txt"
        );

        string settings = GetSettingsStringFromFile(path);
        List<string> symbols = _dpsMain.ParseSymbols(settings);

        var pups = _dpsMain.ParsePupSpawnSettings(symbols);
        
        Assert.That(pups.SpawnsDynamicPups, Is.True, "Dynamic spawns incorrectly parsed as false!");
        Assert.That(pups.MinPups, Is.EqualTo(100), "Min dynamic pups was parsed incorrectly");
        Assert.That(pups.MaxPups, Is.EqualTo(500), "Max dynamic pups was parsed incorrectly");
        Assert.That(pups.SpawnChance, Is.EqualTo(1f), "Dynamic spawn chance was parsed incorrectly");
    }

    [Test]
    public void Settings_ModCampaignFound()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyCampaign.txt"
        );

        var wrapper = ParseTestSettings(path);
        
        Assert.That(wrapper.HasCampaignSettings, Is.True, "Couldn't find the mod campaign in the settings object");
    }

    [Test]
    public void Settings_ModCampaignHasCorrectID()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyCampaign.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "mycoolscug"
        );
        
        Assert.That(campaign, Is.Not.Null, "Couldn't find the right campaign ID");
    }

    [Test]
    public void Settings_ModCampaignHasCorrectPupSettings()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyCampaign.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "mycoolscug"
        );
        
        Assert.That(campaign.PupSpawnSettings, Is.Not.Null, "PupSpawnSettings object was null");

        var pups = campaign.PupSpawnSettings;
        
        Assert.That(pups.SpawnsDynamicPups, Is.True, "Dynamic spawns incorrectly parsed as false!");
        Assert.That(pups.MinPups, Is.EqualTo(0), $"Min dynamic pups was parsed incorrectly: {pups.MinPups}");
        Assert.That(pups.MaxPups, Is.EqualTo(99), $"Max dynamic pups was parsed incorrectly: {pups.MaxPups}");
        Assert.That(pups.SpawnChance, Is.EqualTo(0.2f), $"Dynamic spawn chance was parsed incorrectly: {pups.SpawnChance}");
    }
    
    [Test]
    public void Settings_ModRegionFound()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyRegion.txt"
        );

        var wrapper = ParseTestSettings(path);
        
        Assert.That(wrapper.HasRegionSettings(), Is.True, "Couldn't find the mod region in the settings object");
    }

    [Test]
    public void Settings_ModRegionHasCorrectID()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyRegion.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "myawesomeregion"
        );
        
        Assert.That(campaign, Is.Not.Null, "Couldn't find the right region ID");
    }

    [Test]
    public void Settings_ModRegionHasCorrectPupSettings()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyRegion.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "myawesomeregion"
        );
        
        Assert.That(campaign.PupSpawnSettings, Is.Not.Null, "PupSpawnSettings object was null");

        var pups = campaign.PupSpawnSettings;
        
        Assert.That(pups.SpawnsDynamicPups, Is.False, "Dynamic spawns incorrectly parsed as true!");
    }

    [Test]
    public void Settings_MultiCampaignsAndRegions()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyMulti.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign1 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "mycoolscug"
        );
        var campaign2 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "evencoolerscug"
        );
        var region1 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "myawesomeregion"
        );
        var region2 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "evenbetterregion"
        );
        
        Assert.That(campaign1, Is.Not.Null, "Couldn't find the right ID for campaign1");
        Assert.That(campaign2, Is.Not.Null, "Couldn't find the right ID for campaign2");
        Assert.That(region1, Is.Not.Null, "Couldn't find the right ID for region1");
        Assert.That(region2, Is.Not.Null, "Couldn't find the right ID for region2");
    }
    
    [Test]
    public void Settings_MultiCorrectPupSettings()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "DummyMulti.txt"
        );
        
        var wrapper = ParseTestSettings(path);

        var campaign1 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "mycoolscug"
        );
        var campaign2 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Campaign,
            "evencoolerscug"
        );
        var region1 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "myawesomeregion"
        );
        var region2 = wrapper.GetSettings(
            CustomSettingsObject.SettingsType.Region,
            "evenbetterregion"
        );

        var c1Pups = campaign1.PupSpawnSettings;
        var c2Pups = campaign2.PupSpawnSettings;
        var r1Pups = region1.PupSpawnSettings;
        var r2Pups = region2.PupSpawnSettings;
        
        Assert.That(c1Pups, Is.Not.Null, "Campaign1's PupSpawnSettings object was null");
        Assert.That(c2Pups, Is.Not.Null, "Campaign2's PupSpawnSettings object was null");
        Assert.That(r1Pups, Is.Not.Null, "Region1's PupSpawnSettings object was null");
        Assert.That(r2Pups, Is.Not.Null, "Region2's PupSpawnSettings object was null");
        
        Assert.That(c1Pups.SpawnsDynamicPups, Is.True, "Campaign1's dynamic spawns incorrectly parsed as false!");
        Assert.That(c1Pups.MinPups, Is.EqualTo(1), $"Campaign1's min dynamic pups was parsed incorrectly: {c1Pups.MinPups}");
        Assert.That(c1Pups.MaxPups, Is.EqualTo(1), $"Campaign1's max dynamic pups was parsed incorrectly: {c1Pups.MaxPups}");
        Assert.That(c1Pups.SpawnChance, Is.EqualTo(0.7f), $"Campaign1's dynamic spawn chance was parsed incorrectly: {c1Pups.SpawnChance}");
        
        Assert.That(c2Pups.SpawnsDynamicPups, Is.False, "Campaign2's dynamic spawns incorrectly parsed as true!");
        Assert.That(r1Pups.SpawnsDynamicPups, Is.False, "Region2's dynamic spawns incorrectly parsed as true!");
        
        Assert.That(r2Pups.SpawnsDynamicPups, Is.True, "Region2's dynamic spawns incorrectly parsed as false!");
        Assert.That(r2Pups.MinPups, Is.EqualTo(3), $"Region2's min dynamic pups was parsed incorrectly: {r2Pups.MinPups}");
        Assert.That(r2Pups.MaxPups, Is.EqualTo(7), $"Region2's max dynamic pups was parsed incorrectly: {r2Pups.MaxPups}");
        Assert.That(r2Pups.SpawnChance, Is.EqualTo(1f), $"Region2's dynamic spawn chance was parsed incorrectly: {r2Pups.SpawnChance}");
    }
    
    private CustomSettingsWrapper ParseTestSettings(string filePath)
    {
        string settings = GetSettingsStringFromFile(filePath);
        List<string> symbols = _dpsMain.ParseSymbols(settings);
        
        CustomSettingsWrapper modWrapper = new CustomSettingsWrapper("mycoolmod");
        modWrapper = _dpsMain.ParseGeneralSettings(symbols, modWrapper, _dummyContentMap);

        return modWrapper;
    }
    
    private string GetSettingsStringFromFile(string path)
    {
        string settings;
        using (FileStream stream = File.OpenRead(path))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                settings = reader.ReadToEnd();
            }
        }

        settings = Regex.Replace(settings, @"\s+", "");
        return settings;
    }
}