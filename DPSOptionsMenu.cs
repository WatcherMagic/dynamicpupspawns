using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace dynamicpupspawns;

public class DPSOptionsMenu : OptionInterface
{
    public readonly Configurable<bool> DynamicSpawnsPossible;
    public readonly Configurable<bool> RandomizeUsingGaussian;
    public readonly Configurable<bool> Persistence;
    public readonly Configurable<int> MinPups;
    public readonly Configurable<int> MaxPups;
    public readonly Configurable<int> SpawnChance;
    public readonly Configurable<bool> UseAllRooms;
    public readonly Configurable<bool> WeighRooms;
    public readonly Configurable<bool> AllowSubmergedDens;
    
    public readonly Configurable<bool> SurvivorPupsSpawn;
    public readonly Configurable<bool> SurvivorPersistence;
    //public readonly Configurable<bool> SurvivorUseGeneralExtraSpawnsSettings;
    public readonly Configurable<int> SurvivorSpawnChance;
    public readonly Configurable<int> SurvivorMinPups;
    public readonly Configurable<int> SurvivorMaxPups;
    
    public readonly Configurable<bool> MonkPupsSpawn;
    public readonly Configurable<bool> MonkPersistence;
    //public readonly Configurable<bool> MonkUseGeneralExtraSpawnsSettings;
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
        Persistence = config.Bind("Persistence", true);
        MinPups = config.Bind("MinPups", _defaultMin);
        MaxPups = config.Bind("MaxPups", _defaultMax);
        SpawnChance = config.Bind("SpawnChance", _defaultChance);
        UseAllRooms = config.Bind("UseAllRooms", false);
        WeighRooms = config.Bind("WeighRooms", true);
        AllowSubmergedDens = config.Bind<bool>("AllowSubmergedDens", false);
        
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
        Tabs = new []
        {
            generalSettings
        };
        
        UIelement[] optionsUI =
        {
            new OpLabel(10f, 550f, "General Settings", true),
            
            new OpCheckBox(DynamicSpawnsPossible, 10f, 500f),
            new OpLabel(50f, 497f, "Dynamic Pups Spawn"),
            new OpLabel(190f, 497f, "Check this box for extra pups to be placed in random rooms"),
            
            new OpCheckBox(Persistence, 10f, 457f),
            new OpLabel(50f, 457f, "Persistence"),
            new OpLabel(190f, 457f, "Check this box for living pups to be respawned each cycle"),
            
            new OpCheckBox(UseAllRooms, 10f, 417f),
            new OpLabel(50f, 417f, "Use All Rooms"),
            new OpLabel(190f, 417f, "Check to allow spawning in any room in the region"),
            
            new OpCheckBox(WeighRooms, 10f, 377f),
            new OpLabel(50f, 377f, "Weight Rooms"),
            new OpLabel(190f, 377f, "Rooms with more dens are more likely to spawn pups"),
            
            new OpCheckBox(AllowSubmergedDens, 10f, 337f),
            new OpLabel(50f, 337f, "Allow Submerged"),
            new OpLabel(190f, 337f, "Check to let submerged dens be factored into room weight"),
            
            new OpSlider(SpawnChance, new Vector2(10f, 290f), 100),
            new OpLabel(120f, 290f, "Spawn Chance (%)"),
            
            new OpDragger(MinPups, 250f, 290f),
            new OpLabel(280f, 290f, "Min Pups Possible"),
            
            new OpDragger(MaxPups, 400f, 290f),
            new OpLabel(430f, 290f, "Max Pups Possible"),
            
            new OpLabel(10f, 230f, "Base Game Settings Override", true),
            new OpLabel(10f, 160f, "Pups Spawn"),
            new OpLabel(10f, 130f, "Persistent"),
            new OpLabel(10f, 100f, "Spawn Chance"),
            new OpLabel(10f, 70f, "Min"),
            new OpLabel(10f, 40f, "Max"),
            
            new OpLabel(120f, 190f, "Monk"),
            new OpCheckBox(MonkPupsSpawn, 120f, 157f),
            new OpCheckBox(MonkPersistence, 120f, 127f),
            new OpSlider(MonkSpawnChance, new Vector2(120f, 100f), 100),
            new OpDragger(MonkMinPups, 120f, 67f),
            new OpDragger(MonkMaxPups, 120f, 37f),
            
            new OpLabel(260f, 190f, "Survivor"),
            new OpCheckBox(SurvivorPupsSpawn, 260f, 157f),
            new OpCheckBox(SurvivorPersistence, 260f, 127f),
            new OpSlider(SurvivorSpawnChance, new Vector2(260f, 100f), 100),
            new OpDragger(SurvivorMinPups, 260f, 67f),
            new OpDragger(SurvivorMaxPups, 260f, 37f),
            
            new OpLabel(400f, 190f, "Hunter"),
            new OpCheckBox(HunterPupsSpawn, 400f, 157f),
            new OpCheckBox(HunterPersistence, 400f, 127f),
            new OpSlider(HunterSpawnChance, new Vector2(400f, 100f), 100),
            new OpDragger(HunterMinPups, 400f, 67f),
            new OpDragger(HunterMaxPups, 400f, 37f),
        };
        generalSettings.AddItems(optionsUI);
    }
}