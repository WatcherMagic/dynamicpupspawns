using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace dynamicpupspawns;

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
    
    public readonly Configurable<bool> MonkPupsSpawn;
    public readonly Configurable<bool> MonkPersistence;
    public readonly Configurable<int> MonkSpawnChance;
    public readonly Configurable<int> MonkMinPups;
    public readonly Configurable<int> MonkMaxPups;
    
    public readonly Configurable<bool> HunterPupsSpawn;
    public readonly Configurable<bool> HunterPersistence;
    public readonly Configurable<int> HunterSpawnChance;
    public readonly Configurable<int> HunterMinPups;
    public readonly Configurable<int> HunterMaxPups;

    public readonly Configurable<bool> GourmandPupsSpawn;
    public readonly Configurable<bool> GourmandPersistence;
    public readonly Configurable<int> GourmandSpawnChance;
    public readonly Configurable<int> GourmandMinPups;
    public readonly Configurable<int> GourmandMaxPups;
    
    public readonly Configurable<bool> ArtificerPupsSpawn;
    public readonly Configurable<bool> ArtificerPersistence;
    public readonly Configurable<int> ArtificerSpawnChance;
    public readonly Configurable<int> ArtificerMinPups;
    public readonly Configurable<int> ArtificerMaxPups;
    
    public readonly Configurable<bool> SpearmasterPupsSpawn;
    public readonly Configurable<bool> SpearmasterPersistence;
    public readonly Configurable<int> SpearmasterSpawnChance;
    public readonly Configurable<int> SpearmasterMinPups;
    public readonly Configurable<int> SpearmasterMaxPups;
    
    public readonly Configurable<bool> RivuletPupsSpawn;
    public readonly Configurable<bool> RivuletPersistence;
    public readonly Configurable<int> RivuletSpawnChance;
    public readonly Configurable<int> RivuletMinPups;
    public readonly Configurable<int> RivuletMaxPups;
    
    public readonly Configurable<bool> SaintPupsSpawn;
    public readonly Configurable<bool> SaintPersistence;
    public readonly Configurable<int> SaintSpawnChance;
    public readonly Configurable<int> SaintMinPups;
    public readonly Configurable<int> SaintMaxPups;
    
    public readonly Configurable<bool> WatcherPupsSpawn;
    public readonly Configurable<bool> WatcherPersistence;
    public readonly Configurable<int> WatcherSpawnChance;
    public readonly Configurable<int> WatcherMinPups;
    public readonly Configurable<int> WatcherMaxPups;

    private readonly int _defaultChance = 5; //percent
    private readonly int _defaultMin = 1;
    private readonly int _defaultMax = 3;
    
    public DPSOptionsMenu(DynamicPupSpawns plugin)
    {
        DynamicSpawnsPossible = config.Bind("DynamicSpawnsPossible", true);
        RandomizeUsingGaussian = config.Bind("GausssianRandomize", true);
        OverrideNoPupCampaigns = config.Bind("OverrideNoPups", false);
        Persistence = config.Bind("Persistence", true);
        MinPups = config.Bind("MinPups", _defaultMin);
        MaxPups = config.Bind("MaxPups", _defaultMax);
        SpawnChance = config.Bind("SpawnChance", _defaultChance);
        UseAllRooms = config.Bind("UseAllRooms", false);
        WeighRooms = config.Bind("WeighRooms", true);
        AllowSubmergedDens = config.Bind("AllowSubmergedDens", false);
        
        SurvivorPupsSpawn = config.Bind("SurvivorPupsSpawn", true);
        SurvivorPersistence = config.Bind("SurvivorPersistence", true);
        SurvivorSpawnChance = config.Bind("SurvivorSpawnChance", _defaultChance);
        SurvivorMinPups = config.Bind("SurvivorMinPups", _defaultMin);
        SurvivorMaxPups = config.Bind("SurvivorMaxPups", _defaultMax);
        
        MonkPupsSpawn = config.Bind("MonkPupsSpawn", false);
        MonkPersistence = config.Bind("MonkPersistence", true);
        MonkSpawnChance = config.Bind("MonkSpawnChance", _defaultChance);
        MonkMinPups = config.Bind("MonkMinPups", _defaultMin);
        MonkMaxPups = config.Bind("MonkMaxPups", _defaultMax);
        
        HunterPupsSpawn = config.Bind("HunterPupsSpawn", true);
        HunterPersistence = config.Bind("HunterPersistence", true);
        HunterSpawnChance = config.Bind("HunterSpawnChance", _defaultChance);
        HunterMinPups = config.Bind("HunterMinPups", _defaultMin);
        HunterMaxPups = config.Bind("HunterMaxPups", _defaultMax);
        
        GourmandPupsSpawn = config.Bind("GourmandPupsSpawn", true);
        GourmandPersistence = config.Bind("GourmandPersistence", true);
        GourmandSpawnChance = config.Bind("GourmandSpawnChance", _defaultChance);
        GourmandMinPups = config.Bind("GourmandMinPups", _defaultMin);
        GourmandMaxPups = config.Bind("GourmandMaxPups", _defaultMax);
        
        ArtificerPupsSpawn = config.Bind("ArtificerPupsSpawn", false);
        ArtificerPersistence = config.Bind("ArtificerPersistence", true);
        ArtificerSpawnChance = config.Bind("ArtificerSpawnChance", _defaultChance);
        ArtificerMinPups = config.Bind("ArtificerMinPups", _defaultMin);
        ArtificerMaxPups = config.Bind("ArtificerMaxPups", _defaultMax);
        
        SpearmasterPupsSpawn = config.Bind("SpearmasterPupsSpawn", false);
        SpearmasterPersistence = config.Bind("SpearmasterPersistence", true);
        SpearmasterSpawnChance = config.Bind("SpearmasterSpawnChance", _defaultChance);
        SpearmasterMinPups = config.Bind("SpearmasterMinPups", _defaultMin);
        SpearmasterMaxPups = config.Bind("SpearmasterMaxPups", _defaultMax);
        
        RivuletPupsSpawn = config.Bind("RivuletPupsSpawn", false);
        RivuletPersistence = config.Bind("RivuletPersistence", true);
        RivuletSpawnChance = config.Bind("RivuletSpawnChance", _defaultChance);
        RivuletMinPups = config.Bind("RivuletMinPups", _defaultMin);
        RivuletMaxPups = config.Bind("RivuletMaxPups", _defaultMax);
        
        SaintPupsSpawn = config.Bind("SaintPupsSpawn", false);
        SaintPersistence = config.Bind("SaintPersistence", true);
        SaintSpawnChance = config.Bind("SaintSpawnChance", _defaultChance);
        SaintMinPups = config.Bind("SaintMinPups", _defaultMin);
        SaintMaxPups = config.Bind("SaintMaxPups", _defaultMax);
        
        WatcherPupsSpawn = config.Bind("WatcherPupsSpawn", false);
        WatcherPersistence = config.Bind("WatcherPersistence", true);
        WatcherSpawnChance = config.Bind("WatcherSpawnChance", _defaultChance);
        WatcherMinPups = config.Bind("WatcherMinPups", _defaultMin);
        WatcherMaxPups = config.Bind("WatcherMaxPups", _defaultMax);
    }

    public override void Initialize()
    {
        base.Initialize();
        OpTab generalSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("General"));
        OpTab baseGameSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Base Game"));
        OpTab downpourSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Downpour"));
        OpTab watcherSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("Watcher"));
        Tabs = new []
        {
            generalSettings,
            baseGameSettings,
            downpourSettings,
            watcherSettings
        };
        
        UIelement[] generalSettingsUI =
        {
            new OpLabel(10f, 550f, "General Settings", true),
            
            
        };
        generalSettings.AddItems(generalSettingsUI);
        
        UIelement[] baseGameSettingsUI =
        {
            new OpLabel(10f, 550f, "Base Game Settings", true),
            
            
        };
        baseGameSettings.AddItems(baseGameSettingsUI);
        
        UIelement[] downpourSettingsUI =
        {
            new OpLabel(10f, 550f, "Downpour Settings", true),
            
            
        };
        downpourSettings.AddItems(downpourSettingsUI);
        
        UIelement[] watcherSettingsUI =
        {
            new OpLabel(10f, 550f, "Watcher Settings", true),
            
            
        };
        watcherSettings.AddItems(watcherSettingsUI);
    }
}