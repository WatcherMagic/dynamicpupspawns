using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace DynamicPupSpawns;

public class DPSOptionsMenu : OptionInterface
{
    public readonly Configurable<bool> DynamicSpawnsPossible;
    public readonly Configurable<bool> RandomizeUsingGaussian;
    public readonly Configurable<bool> OverrideNoPupCampaigns;
    public readonly Configurable<bool> Persistence;
    public readonly Configurable<int> MinPups;
    public readonly Configurable<int> MaxPups;
    public readonly Configurable<int> SpawnChance;
    public readonly Configurable<bool> UseAllRooms;
    public readonly Configurable<bool> WeighRooms;
    public readonly Configurable<bool> AllowSubmergedDens;
    
    public readonly Configurable<bool> SurvivorPupsSpawn;
    public readonly Configurable<bool> SurvivorPersistence;
    public readonly Configurable<int> SurvivorSpawnChance;
    public readonly Configurable<int> SurvivorMinPups;
    public readonly Configurable<int> SurvivorMaxPups;
    public readonly Configurable<bool> OverrideSurvivor;
    
    public readonly Configurable<bool> MonkPupsSpawn;
    public readonly Configurable<bool> MonkPersistence;
    public readonly Configurable<int> MonkSpawnChance;
    public readonly Configurable<int> MonkMinPups;
    public readonly Configurable<int> MonkMaxPups;
    public readonly Configurable<bool> OverrideMonk;
    
    public readonly Configurable<bool> HunterPupsSpawn;
    public readonly Configurable<bool> HunterPersistence;
    public readonly Configurable<int> HunterSpawnChance;
    public readonly Configurable<int> HunterMinPups;
    public readonly Configurable<int> HunterMaxPups;
    public readonly Configurable<bool> OverrideHunter;

    public readonly Configurable<bool> GourmandPupsSpawn;
    public readonly Configurable<bool> GourmandPersistence;
    public readonly Configurable<int> GourmandSpawnChance;
    public readonly Configurable<int> GourmandMinPups;
    public readonly Configurable<int> GourmandMaxPups;
    public readonly Configurable<bool> OverrideGourmand;
    
    public readonly Configurable<bool> ArtificerPupsSpawn;
    public readonly Configurable<bool> ArtificerPersistence;
    public readonly Configurable<int> ArtificerSpawnChance;
    public readonly Configurable<int> ArtificerMinPups;
    public readonly Configurable<int> ArtificerMaxPups;
    public readonly Configurable<bool> OverrideArtificer;
    
    public readonly Configurable<bool> SpearmasterPupsSpawn;
    public readonly Configurable<bool> SpearmasterPersistence;
    public readonly Configurable<int> SpearmasterSpawnChance;
    public readonly Configurable<int> SpearmasterMinPups;
    public readonly Configurable<int> SpearmasterMaxPups;
    public readonly Configurable<bool> OverrideSpearmaster;
    
    public readonly Configurable<bool> RivuletPupsSpawn;
    public readonly Configurable<bool> RivuletPersistence;
    public readonly Configurable<int> RivuletSpawnChance;
    public readonly Configurable<int> RivuletMinPups;
    public readonly Configurable<int> RivuletMaxPups;
    public readonly Configurable<bool> OverrideRivulet;
    
    public readonly Configurable<bool> SaintPupsSpawn;
    public readonly Configurable<bool> SaintPersistence;
    public readonly Configurable<int> SaintSpawnChance;
    public readonly Configurable<int> SaintMinPups;
    public readonly Configurable<int> SaintMaxPups;
    public readonly Configurable<bool> OverrideSaint;
    
    public readonly Configurable<bool> WatcherPupsSpawn;
    public readonly Configurable<bool> WatcherPersistence;
    public readonly Configurable<int> WatcherSpawnChance;
    public readonly Configurable<int> WatcherMinPups;
    public readonly Configurable<int> WatcherMaxPups;
    public readonly Configurable<bool> OverrideWatcher;
    
    public readonly Configurable<bool> OverrideMods;

    private readonly int _defaultChancePercent = 5;
    private readonly int _defaultMin = 1;
    private readonly int _defaultMax = 3;
    
    public DPSOptionsMenu(DynamicPupSpawnsPlugin plugin)
    {
        DynamicSpawnsPossible = config.Bind("DynamicSpawnsPossible", true);
        RandomizeUsingGaussian = config.Bind("GausssianRandomize", true);
        OverrideNoPupCampaigns = config.Bind("OverrideNoPups", false);
        Persistence = config.Bind("Persistence", true);
        MinPups = config.Bind("MinPups", _defaultMin);
        MaxPups = config.Bind("MaxPups", _defaultMax);
        SpawnChance = config.Bind("SpawnChance", _defaultChancePercent);
        UseAllRooms = config.Bind("UseAllRooms", false);
        WeighRooms = config.Bind("WeighRooms", true);
        AllowSubmergedDens = config.Bind("AllowSubmergedDens", false);
        
        SurvivorPupsSpawn = config.Bind("SurvivorPupsSpawn", true);
        SurvivorPersistence = config.Bind("SurvivorPersistence", true);
        SurvivorSpawnChance = config.Bind("SurvivorSpawnChance", _defaultChancePercent);
        SurvivorMinPups = config.Bind("SurvivorMinPups", _defaultMin);
        SurvivorMaxPups = config.Bind("SurvivorMaxPups", _defaultMax);
        OverrideSurvivor = config.Bind("OverrideSurvivor", true);
        
        MonkPupsSpawn = config.Bind("MonkPupsSpawn", false);
        MonkPersistence = config.Bind("MonkPersistence", true);
        MonkSpawnChance = config.Bind("MonkSpawnChance", _defaultChancePercent);
        MonkMinPups = config.Bind("MonkMinPups", _defaultMin);
        MonkMaxPups = config.Bind("MonkMaxPups", _defaultMax);
        OverrideMonk = config.Bind("OverrideMonk", true);
        
        HunterPupsSpawn = config.Bind("HunterPupsSpawn", true);
        HunterPersistence = config.Bind("HunterPersistence", true);
        HunterSpawnChance = config.Bind("HunterSpawnChance", _defaultChancePercent);
        HunterMinPups = config.Bind("HunterMinPups", _defaultMin);
        HunterMaxPups = config.Bind("HunterMaxPups", _defaultMax);
        OverrideHunter = config.Bind("OverrideHunter", true);
        
        GourmandPupsSpawn = config.Bind("GourmandPupsSpawn", true);
        GourmandPersistence = config.Bind("GourmandPersistence", true);
        GourmandSpawnChance = config.Bind("GourmandSpawnChance", _defaultChancePercent);
        GourmandMinPups = config.Bind("GourmandMinPups", _defaultMin);
        GourmandMaxPups = config.Bind("GourmandMaxPups", _defaultMax);
        OverrideGourmand = config.Bind("OverrideGourmand", true);
        
        ArtificerPupsSpawn = config.Bind("ArtificerPupsSpawn", false);
        ArtificerPersistence = config.Bind("ArtificerPersistence", true);
        ArtificerSpawnChance = config.Bind("ArtificerSpawnChance", _defaultChancePercent);
        ArtificerMinPups = config.Bind("ArtificerMinPups", _defaultMin);
        ArtificerMaxPups = config.Bind("ArtificerMaxPups", _defaultMax);
        OverrideArtificer = config.Bind("OverrideArtificer", false);
        
        SpearmasterPupsSpawn = config.Bind("SpearmasterPupsSpawn", false);
        SpearmasterPersistence = config.Bind("SpearmasterPersistence", true);
        SpearmasterSpawnChance = config.Bind("SpearmasterSpawnChance", _defaultChancePercent);
        SpearmasterMinPups = config.Bind("SpearmasterMinPups", _defaultMin);
        SpearmasterMaxPups = config.Bind("SpearmasterMaxPups", _defaultMax);
        OverrideSpearmaster = config.Bind("OverrideSpearmaster", false);
        
        RivuletPupsSpawn = config.Bind("RivuletPupsSpawn", false);
        RivuletPersistence = config.Bind("RivuletPersistence", true);
        RivuletSpawnChance = config.Bind("RivuletSpawnChance", _defaultChancePercent);
        RivuletMinPups = config.Bind("RivuletMinPups", _defaultMin);
        RivuletMaxPups = config.Bind("RivuletMaxPups", _defaultMax);
        OverrideRivulet = config.Bind("OverrideRivulet", false);
        
        SaintPupsSpawn = config.Bind("SaintPupsSpawn", false);
        SaintPersistence = config.Bind("SaintPersistence", true);
        SaintSpawnChance = config.Bind("SaintSpawnChance", _defaultChancePercent);
        SaintMinPups = config.Bind("SaintMinPups", _defaultMin);
        SaintMaxPups = config.Bind("SaintMaxPups", _defaultMax);
        OverrideSaint = config.Bind("OverrideSaint", false);
        
        WatcherPupsSpawn = config.Bind("WatcherPupsSpawn", false);
        WatcherPersistence = config.Bind("WatcherPersistence", true);
        WatcherSpawnChance = config.Bind("WatcherSpawnChance", _defaultChancePercent);
        WatcherMinPups = config.Bind("WatcherMinPups", _defaultMin);
        WatcherMaxPups = config.Bind("WatcherMaxPups", _defaultMax);
        OverrideWatcher = config.Bind("OverrideWatcher", false);

        OverrideMods = config.Bind("OverrideMods", false);
    }

    public override void Initialize()
    {
        base.Initialize();
        OpTab generalSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("General"));
        // OpTab baseGameSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Base Game"));
        // OpTab downpourSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Downpour"));
        // OpTab watcherSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Watcher"));
        Tabs = new []
        {
            generalSettings,
            // baseGameSettings,
            // downpourSettings,
            // watcherSettings
        };
        
        UIelement[] generalSettingsUI =
        {
            new OpLabel(10f, 550f, "General Settings", true),
            
            new OpCheckBox(OverrideNoPupCampaigns, 10, 477),
            new OpLabel(50f, 480f, "Override No Pups"),
            new OpLabel(190f, 480f, "Campaigns that don't allow pups to spawn will spawn dynamic pups"),
            
            new OpCheckBox(UseAllRooms, 10, 437),
            new OpLabel(50f, 440f, "Use All Rooms"),
            new OpLabel(190f, 440f, "Check to allow spawning in any room in the region, except shelters"),
            
            new OpCheckBox(WeighRooms, 10, 397),
            new OpLabel(50f, 400f, "Weigh Rooms"),
            new OpLabel(190f, 400f, "Rooms with more dens are more likely to spawn pups"),
            
            new OpCheckBox(AllowSubmergedDens, 10, 357),
            new OpLabel(50f, 360f, "Allow Submerged Dens"),
            new OpLabel(190f, 360f, "Check to let submerged dens be factored into room weight"),
            
            new OpCheckBox(RandomizeUsingGaussian, 10, 317),
            new OpLabel(50f, 320f, "Gaussian Random"),
            new OpLabelLong(new Vector2(190f, 285f), new Vector2(400, 50), 
                "\"Gaussian Distribution\" modifies random rates to fall within a specific zone of values more often than not; in this case if Min is 1 and Max is 5, values 2-3 are usually chosen, value 1 is less often, value 4 is rare, and value 5 is more rare. Uncheck for maximum chaos",
                true, FLabelAlignment.Left),
            
            new OpLabel(10, 230, "These settings will be applied to mod campaigns that don't specify their own dynamic pup settings"),
            
            new OpCheckBox(DynamicSpawnsPossible, 10, 197),
            new OpLabel(50f, 200f, "Dynamic Pups Spawn"),
            new OpLabel(190f, 200f, "Check this box for extra pups to be placed in random rooms"),
            
            new OpCheckBox(Persistence, 10, 157),
            new OpLabel(50f, 160f, "Persistence"),
            new OpLabel(190f, 160f, "Check this box for living pups to be respawned each cycle"),
            
            new OpLabel(10, 120, "Spawn Chance (%)"),
            new OpSlider(SpawnChance, new Vector2(130f, 117f), 4.6f, false),
            
            new OpDragger(MinPups, 10, 77),
            new OpLabel(50, 80, "Min Pups Possible"),
            
            new OpDragger(MaxPups, 10, 47),
            new OpLabel(50, 50, "Max Pups Possible"),
            
            new OpHoldButton(new Vector2(170, 43), new Vector2(410, 60), "Reset All Values", 80f),
        };
        generalSettings.AddItems(generalSettingsUI);
        
        // UIelement[] baseGameSettingsUI =
        // {
        //     new OpLabel(10f, 550f, "Base Game Settings", true),
        //     
        //     
        // };
        // baseGameSettings.AddItems(baseGameSettingsUI);
        //
        // UIelement[] downpourSettingsUI =
        // {
        //     new OpLabel(10f, 550f, "Downpour Settings", true),
        //     
        //     
        // };
        // downpourSettings.AddItems(downpourSettingsUI);
        //
        // UIelement[] watcherSettingsUI =
        // {
        //     new OpLabel(10f, 550f, "Watcher Settings", true),
        //     
        //     
        // };
        // watcherSettings.AddItems(watcherSettingsUI);
    }
}