using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace dynamicpupspawns;

public class DPSOptionsMenu : OptionInterface
{
    public readonly Configurable<bool> pupsSpawn;
    public readonly Configurable<bool> persistence;
    public readonly Configurable<int> minPups;
    public readonly Configurable<int> maxPups;
    public readonly Configurable<int> spawnChance;

    public DPSOptionsMenu(DynamicPupSpawns plugin)
    {
        
        pupsSpawn = config.Bind<bool>("DPS_PupsSpawn", true);
        persistence = config.Bind<bool>("DPS_Persistence", true);
        minPups = config.Bind<int>("DPS_MinPups", 1);
        maxPups = config.Bind<int>("DPS_MaxPups", 4);
        spawnChance = config.Bind<int>("DPS_SpawnChance", 5);
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
            new OpLabel(10f, 420f, "Extra pup spawns settings:"),
            new OpSlider(spawnChance, new Vector2(10f, 380f), 100),
            new OpDragger(minPups, 10f, 350f),
            new OpLabel(50f, 350f, "Min Pups Spawned"),
            new OpDragger(maxPups, 10f, 320f),
            new OpLabel(50f, 320f, "Max Pups Spawned")
        };
        generalSettings.AddItems(optionsUI);
    }
}