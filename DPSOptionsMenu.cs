using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace dynamicpupspawns;

public class DPSOptionsMenu : OptionInterface
{
    public readonly Configurable<bool> pupsSpawn;
    public readonly Configurable<bool> randomizeUsingGaussian;
    public readonly Configurable<bool> persistence;
    public readonly Configurable<int> minPups;
    public readonly Configurable<int> maxPups;
    public readonly Configurable<int> spawnChance;
    public readonly Configurable<bool> useAllRooms;
    public readonly Configurable<bool> weighRooms;
    //public readonly Configurable<bool> allowSpawnInRoomsWithSubmergedDens;
    
    public readonly Configurable<bool> survivorPupsSpawn;
    public readonly Configurable<bool> survivorPersistence;
    //public readonly Configurable<bool> survivorUseGeneralExtraSpawnsSettings;
    public readonly Configurable<int> survivorSpawnChance;
    public readonly Configurable<int> survivorMinPups;
    public readonly Configurable<int> survivorMaxPups;
    
    public readonly Configurable<bool> monkPupsSpawn;
    public readonly Configurable<bool> monkPersistence;
    //public readonly Configurable<bool> monkUseGeneralExtraSpawnsSettings;
    public readonly Configurable<int> monkSpawnChance;
    public readonly Configurable<int> monkMinPups;
    public readonly Configurable<int> monkMaxPups;

    private int _defaultChance = 5;
    private int _defaultMin = 1;
    private int _defaultMax = 4;
    
    public DPSOptionsMenu(DynamicPupSpawns plugin)
    {
        pupsSpawn = config.Bind<bool>("PupsSpawn", true);
        randomizeUsingGaussian = config.Bind<bool>("GausssianRandomize", true);
        persistence = config.Bind<bool>("Persistence", true);
        minPups = config.Bind<int>("MinPups", _defaultMin);
        maxPups = config.Bind<int>("MaxPups", _defaultMax);
        spawnChance = config.Bind<int>("SpawnChance", _defaultChance);
        useAllRooms = config.Bind<bool>("UseAllRooms", false);
        weighRooms = config.Bind<bool>("WeighRooms", true);
        //allowSpawnInRoomsWithSubmergedDens = config.Bind<bool>("AllowSpawnInRoomsWithSubmergedDens", false);
        
        survivorPupsSpawn = config.Bind<bool>("SurvivorPupsSpawn", true);
        survivorPersistence = config.Bind<bool>("SurvivorPersistence", true);
        //survivorUseGeneralExtraSpawnsSettings = config.Bind<bool>("SurvivorUseGeneralExtraSpawnsSettings", false);
        survivorSpawnChance = config.Bind<int>("SurvivorSpawnChance", _defaultChance);
        survivorMinPups = config.Bind<int>("SurvivorMinPups", _defaultMin);
        survivorMaxPups = config.Bind<int>("SurvivorMaxPups", _defaultMax);
        
        monkPupsSpawn = config.Bind<bool>("MonkPupsSpawn", true);
        monkPersistence = config.Bind<bool>("MonkPersistence", true);
        //monkUseGeneralExtraSpawnsSettings = config.Bind<bool>("MonkUseGeneralExtraSpawnsSettings", false);
        monkSpawnChance = config.Bind<int>("MonkSpawnChance", _defaultChance);
        monkMinPups = config.Bind<int>("MonkMinPups", _defaultMin);
        monkMaxPups = config.Bind<int>("MonkMaxPups", _defaultMax);
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
            new OpCheckBox(pupsSpawn, 10f, 500f),
            new OpLabel(50f, 500f, "Extra pup spawns"),
            new OpCheckBox(persistence, 10f, 470f),
            new OpLabel(50f, 470f, "Living pups respawn"),
            new OpCheckBox(useAllRooms, 200f, 500f),
            new OpLabel(240f, 500f, "Spawn Pups in Rooms Without Dens"),
            new OpCheckBox(weighRooms, 200f, 470f),
            new OpLabel(240f, 470f, "Weigh Spawn Chance for Room based on Den Number"),
            
            new OpLabel(10f, 420f, "Extra pup spawns settings:"),
            new OpSlider(spawnChance, new Vector2(10f, 380f), 100),
            new OpDragger(minPups, 10f, 350f),
            new OpLabel(50f, 350f, "Min Pups Spawned"),
            new OpDragger(maxPups, 10f, 320f),
            new OpLabel(50f, 320f, "Max Pups Spawned"),
            
            new OpLabel(10f, 270f, "Base Game Spawn Settings", true),
            new OpLabel(70f, 250f, "Spawn Pups"),
            new OpLabel(150f, 250f, "Persistence"),
            //new OpLabel(230f, 250f, "Use General Settings"),
            new OpLabel(368f, 250f, "Spawn Chance"),
            new OpLabel(470f, 250f, "Min Pups"),
            new OpLabel(540f, 250f, "Max Pups"),
            
            new OpLabel(10f, 220f, "Survivor:"),
            new OpCheckBox(survivorPupsSpawn,90f, 220f),
            new OpCheckBox(survivorPersistence,167f, 220f),
            //new OpCheckBox(survivorUseGeneralExtraSpawnsSettings,270f, 220f),
            new OpSlider(survivorSpawnChance, new Vector2(360f, 220f), 100),
            new OpDragger(survivorMinPups, 483f, 220f),
            new OpDragger(survivorMaxPups, 551f, 220f),
            
            new OpLabel(10f, 190f, "Monk:"),
            new OpCheckBox(monkPupsSpawn,90f, 190f),
            new OpCheckBox(monkPersistence,167f, 190f),
            //new OpCheckBox(monkUseGeneralExtraSpawnsSettings,270f, 220f),
            new OpSlider(monkSpawnChance, new Vector2(360f, 190f), 100),
            new OpDragger(monkMinPups, 483f, 190f),
            new OpDragger(monkMaxPups, 551f, 190f),
        };
        generalSettings.AddItems(optionsUI);
    }
}