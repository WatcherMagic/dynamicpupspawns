using BepInEx;


namespace DynamicPupSpawns
{
    [BepInPlugin(ModID, "Dynamic Pup Spawns", "0.1")]
    
    public class DynamicPupSpawnsPlugin : BaseUnityPlugin
    {
         public const string ModID = "dynamicpupspawns";

         private DPSMain _dpsMain;
            
         private void OnEnable()
         {
             _dpsMain = new DPSMain(this);
             
             On.World.SpawnPupNPCs += _dpsMain.SpawnPups;
             On.OverWorld.GateRequestsSwitchInitiation += _dpsMain.SavePupsOnRegionLeave;
                
             On.SaveState.SaveToString += _dpsMain.SaveDataToString;
             On.SaveState.LoadGame += _dpsMain.GetSaveDataFromString;

             On.Creature.Die += _dpsMain.LogPupDeath;

             _dpsMain.CreateOptionsMenuInstance();
             On.RainWorld.OnModsInit += _dpsMain.SetupModSettings;
         }
    }
}
