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
    
    public readonly Configurable<bool> OverrideSurvivor;
    public readonly Configurable<bool> OverrideMonk;
    public readonly Configurable<bool> OverrideHunter;
    public readonly Configurable<bool> OverrideGourmand;
    public readonly Configurable<bool> OverrideArtificer;
    public readonly Configurable<bool> OverrideSpearmaster;
    public readonly Configurable<bool> OverrideRivulet;
    public readonly Configurable<bool> OverrideSaint;
    public readonly Configurable<bool> OverrideWatcher;
    
    public readonly Configurable<bool> OverrideMods;
    
    public DPSOptionsMenu(DynamicPupSpawnsPlugin plugin)
    {
        DynamicSpawnsPossible = config.Bind("DynamicSpawnsPossible", DPSOptions.DynamicSpawnsPossible);
        RandomizeUsingGaussian = config.Bind("GaussianRandomize", DPSOptions.RandomizeUsingGaussian);
        OverrideNoPupCampaigns = config.Bind("OverrideNoPups", DPSOptions.OverrideNoPupCampaigns);
        Persistence = config.Bind("Persistence", DPSOptions.Persistence);
        MinPups = config.Bind("MinPups", DPSOptions.MinPups);
        MaxPups = config.Bind("MaxPups", DPSOptions.MaxPups);
        SpawnChance = config.Bind("SpawnChance", DPSOptions.SpawnChance);
        UseAllRooms = config.Bind("UseAllRooms", DPSOptions.UseAllRooms);
        WeighRooms = config.Bind("WeighRooms", DPSOptions.WeighRooms);
        AllowSubmergedDens = config.Bind("AllowSubmergedDens", DPSOptions.AllowSubmergedDens);
        
        OverrideSurvivor = config.Bind("OverrideSurvivor", DPSOptions.OverrideSurvivor);
        OverrideMonk = config.Bind("OverrideMonk", DPSOptions.OverrideMonk);
        OverrideHunter = config.Bind("OverrideHunter", DPSOptions.OverrideHunter);
        OverrideGourmand = config.Bind("OverrideGourmand", DPSOptions.OverrideGourmand);
        OverrideArtificer = config.Bind("OverrideArtificer", DPSOptions.OverrideArtificer);
        OverrideSpearmaster = config.Bind("OverrideSpearmaster", DPSOptions.OverrideSpearmaster);
        OverrideRivulet = config.Bind("OverrideRivulet", DPSOptions.OverrideRivulet);
        OverrideSaint = config.Bind("OverrideSaint", DPSOptions.OverrideSaint);
        OverrideWatcher = config.Bind("OverrideWatcher", DPSOptions.OverrideWatcher);

        OverrideMods = config.Bind("OverrideMods", DPSOptions.OverrideMods);
    }

    public override void Initialize()
    {
        base.Initialize();
        OpTab generalSettings = new OpTab(this, Custom.rainWorld.inGameTranslator.Translate("General"));
        Tabs = new []
        {
            generalSettings
        };
        
        UIelement[] generalSettingsUI =
        {
            new OpLabel(10f, 550f, "General Settings", true),
            
            
            //===============SPAWN RULESs===================
            new OpLabel(10f, 505f, "Spawn Dynamic Pups"),
            new OpCheckBox(DynamicSpawnsPossible, 150, 502),
            new OpLabel(190f, 505f, "Pups will be placed in random rooms across the region"),
            
            new OpLabelLong(new Vector2(10f, 445), new Vector2(130f, 30f), "Use Gaussian Randomization"),
            new OpCheckBox(RandomizeUsingGaussian, 150, 442),
            new OpLabelLong(new Vector2(190f, 420f), new Vector2(435f, 60f), 
                "\"Gaussian Distribution\" modifies random rates to fall within a specific zone of values more often than not; in this case if Min is 1 and Max is 5, values 2-3 are usually chosen, value 1 is less often, value 4 is rare, and value 5 is more rare. Uncheck for maximum chaos"),
            
            new OpLabel(10f, 385f, "Allow Submerged Dens"),
            new OpCheckBox(AllowSubmergedDens, 150, 382),
            new OpLabelLong(new Vector2(190f, 382), new Vector2(400f, 30f), "If checked, rooms with underwater dens count as viable spawn rooms, INCLUDING rooms with no air at all!"),
            
            new OpLabel(10f, 335f, "Use All Rooms"),
            new OpCheckBox(UseAllRooms, 150, 332),
            new OpLabelLong(new Vector2(190f, 329f), new Vector2(400f, 30f), "Normally, only rooms with dens will spawn pups; check to remove this constraint"),
            
            new OpLabel(10f, 285f, "Weigh Rooms"),
            new OpCheckBox(WeighRooms, 150, 282),
            new OpLabel(190f, 285f, "Rooms with more dens are more likely to spawn pups"),
            
            new OpLabel(10f, 241f, "Persistent Spawns"),
            new OpCheckBox(Persistence, 150, 238),
            new OpLabel(190f, 241f, "Living pups are respawned at the start of the next cycle"),
            
            
            //============SPAWN VALUES====================
            new OpLabel(10, 195, "Spawn Chance (%)"),
            new OpSlider(SpawnChance, new Vector2(130f, 195f), 4.6f, false),
            
            new OpDragger(MinPups, 10, 142),
            new OpLabel(42, 145, "Min Pups Possible"),
            
            new OpDragger(MaxPups, 10, 112),
            new OpLabel(42, 115, "Max Pups Possible"),
            
            
            //==============SPAWN OVERRIDES===================
            new OpCheckBox(OverrideSurvivor, 160f, 145f),
            new OpLabel(190f, 145f, "Override Survivor"),
            
            new OpCheckBox(OverrideMonk, 160f, 112f),
            new OpLabel(190f, 112f, "Override Monk"),
            
            new OpCheckBox(OverrideHunter, 160f, 82f),
            new OpLabel(190f, 82f, "Override Hunter"),
            
            new OpCheckBox(OverrideGourmand, 160f, 52f),
            new OpLabel(190f, 52f, "Override Gourmand"),
            
            new OpCheckBox(OverrideArtificer, 310f, 145f),
            new OpLabel(340f, 145f, "Override Artificer"),
            
            new OpCheckBox(OverrideSpearmaster, 310f, 112f),
            new OpLabel(340f, 112f, "Override Spearmaster"),
            
            new OpCheckBox(OverrideRivulet, 310f, 82f),
            new OpLabel(340f, 82f, "Override Rivulet"),
            
            new OpCheckBox(OverrideSaint, 310f, 52f),
            new OpLabel(340f, 52f, "Override Saint"),
            
            //inv
            // new OpCheckBox(OverrideInv, 470f, 145f),
            // new OpLabel(500f, 145f, "Override Inv"),
            
            new OpCheckBox(OverrideWatcher, 470f, 112f),
            new OpLabel(500f, 112f, "Override Watcher"),
            
            new OpCheckBox(OverrideMods, 470f, 82f),
            new OpLabel(500f, 82f, "Override Mods")
        };
        generalSettings.AddItems(generalSettingsUI);
    }
}