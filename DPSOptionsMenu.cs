using Menu.Remix.MixedUI;
using RWCustom;

namespace dynamicpupspawns;

public class DPSOptionsMenu : OptionInterface
{
    public readonly Configurable<bool> DynamicSpawnsPossible;
    public readonly Configurable<bool> RandomizeUsingGaussian;
    public readonly Configurable<bool> Persistence;
    public readonly Configurable<int> MinPups;
    public readonly Configurable<int> MaxPups;
    public readonly Configurable<float> SpawnChance;
    public readonly Configurable<bool> UseAllRooms;
    public readonly Configurable<bool> WeighRooms;
    //public readonly Configurable<bool> AllowSpawnInRoomsWithSubmergedDens;
    
    public readonly Configurable<bool> SurvivorPupsSpawn;
    public readonly Configurable<bool> SurvivorPersistence;
    //public readonly Configurable<bool> SurvivorUseGeneralExtraSpawnsSettings;
    public readonly Configurable<float> SurvivorSpawnChance;
    public readonly Configurable<int> SurvivorMinPups;
    public readonly Configurable<int> SurvivorMaxPups;
    
    public readonly Configurable<bool> MonkPupsSpawn;
    public readonly Configurable<bool> MonkPersistence;
    //public readonly Configurable<bool> MonkUseGeneralExtraSpawnsSettings;
    public readonly Configurable<float> MonkSpawnChance;
    public readonly Configurable<int> MonkMinPups;
    public readonly Configurable<int> MonkMaxPups;
    
    public readonly Configurable<bool> HunterPupsSpawn;
    public readonly Configurable<bool> HunterPersistence;
    public readonly Configurable<float> HunterSpawnChance;
    public readonly Configurable<int> HunterMinPups;
    public readonly Configurable<int> HunterMaxPups;

    public readonly Configurable<bool> GourmandPupsSpawn;
    public readonly Configurable<bool> GourmandPersistence;
    public readonly Configurable<float> GourmandSpawnChance;
    public readonly Configurable<int> GourmandMinPups;
    public readonly Configurable<int> GourmandMaxPups;
    
    public readonly Configurable<bool> ArtificerPupsSpawn;
    public readonly Configurable<bool> ArtificerPersistence;
    public readonly Configurable<float> ArtificerSpawnChance;
    public readonly Configurable<int> ArtificerMinPups;
    public readonly Configurable<int> ArtificerMaxPups;
    
    public readonly Configurable<bool> SpearmasterPupsSpawn;
    public readonly Configurable<bool> SpearmasterPersistence;
    public readonly Configurable<float> SpearmasterSpawnChance;
    public readonly Configurable<int> SpearmasterMinPups;
    public readonly Configurable<int> SpearmasterMaxPups;
    
    public readonly Configurable<bool> RivuletPupsSpawn;
    public readonly Configurable<bool> RivuletPersistence;
    public readonly Configurable<float> RivuletSpawnChance;
    public readonly Configurable<int> RivuletMinPups;
    public readonly Configurable<int> RivuletMaxPups;
    
    public readonly Configurable<bool> SaintPupsSpawn;
    public readonly Configurable<bool> SaintPersistence;
    public readonly Configurable<float> SaintSpawnChance;
    public readonly Configurable<int> SaintMinPups;
    public readonly Configurable<int> SaintMaxPups;
    
    public readonly Configurable<bool> WatcherPupsSpawn;
    public readonly Configurable<bool> WatcherPersistence;
    public readonly Configurable<float> WatcherSpawnChance;
    public readonly Configurable<int> WatcherMinPups;
    public readonly Configurable<int> WatcherMaxPups;

    private float _defaultChance = 0.05f; //percent
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
        //allowSpawnInRoomsWithSubmergedDens = config.Bind<bool>("AllowSpawnInRoomsWithSubmergedDens", false);
        
        SurvivorPupsSpawn = config.Bind("SurvivorPupsSpawn", true);
        SurvivorPersistence = config.Bind("SurvivorPersistence", true);
        //survivorUseGeneralExtraSpawnsSettings = config.Bind<bool>("SurvivorUseGeneralExtraSpawnsSettings", false);
        SurvivorSpawnChance = config.Bind("SurvivorSpawnChance", _defaultChance);
        SurvivorMinPups = config.Bind("SurvivorMinPups", _defaultMin);
        SurvivorMaxPups = config.Bind("SurvivorMaxPups", _defaultMax);
        
        MonkPupsSpawn = config.Bind("MonkPupsSpawn", false);
        MonkPersistence = config.Bind("MonkPersistence", true);
        //monkUseGeneralExtraSpawnsSettings = config.Bind<bool>("MonkUseGeneralExtraSpawnsSettings", false);
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
            // new OpCheckBox(DynamicSpawnsPossible, 10f, 500f),
            // new OpLabel(50f, 500f, "Extra pup spawns"),
            // new OpCheckBox(Persistence, 10f, 470f),
            // new OpLabel(50f, 470f, "Living pups respawn"),
            // new OpCheckBox(useAllRooms, 200f, 500f),
            // new OpLabel(240f, 500f, "Spawn Pups in Rooms Without Dens"),
            // new OpCheckBox(weighRooms, 200f, 470f),
            // new OpLabel(240f, 470f, "Weigh Spawn Chance for Room based on Den Number"),
            //
            // new OpLabel(10f, 420f, "Extra pup spawns settings:"),
            // new OpSlider(SpawnChance, new Vector2(10f, 380f), 100),
            // new OpDragger(MinPups, 10f, 350f),
            // new OpLabel(50f, 350f, "Min Pups Spawned"),
            // new OpDragger(MaxPups, 10f, 320f),
            // new OpLabel(50f, 320f, "Max Pups Spawned"),
            //
            // new OpLabel(10f, 270f, "Base Game Spawn Settings", true),
            // new OpLabel(70f, 250f, "Spawn Pups"),
            // new OpLabel(150f, 250f, "Persistence"),
            // //new OpLabel(230f, 250f, "Use General Settings"),
            // new OpLabel(368f, 250f, "Spawn Chance"),
            // new OpLabel(470f, 250f, "Min Pups"),
            // new OpLabel(540f, 250f, "Max Pups"),
            //
            // new OpLabel(10f, 220f, "Survivor:"),
            // new OpCheckBox(SurvivorPupsSpawn,90f, 220f),
            // new OpCheckBox(SurvivorPersistence,167f, 220f),
            // //new OpCheckBox(survivorUseGeneralExtraSpawnsSettings,270f, 220f),
            // new OpSlider(SurvivorSpawnChance, new Vector2(360f, 220f), 100),
            // new OpDragger(SurvivorMinPups, 483f, 220f),
            // new OpDragger(SurvivorMaxPups, 551f, 220f),
            //
            // new OpLabel(10f, 190f, "Monk:"),
            // new OpCheckBox(MonkPupsSpawn,90f, 190f),
            // new OpCheckBox(MonkPersistence,167f, 190f),
            // //new OpCheckBox(monkUseGeneralExtraSpawnsSettings,270f, 220f),
            // new OpSlider(MonkSpawnChance, new Vector2(360f, 190f), 100),
            // new OpDragger(MonkMinPups, 483f, 190f),
            // new OpDragger(MonkMaxPups, 551f, 190f),
        };
        generalSettings.AddItems(optionsUI);
    }
}