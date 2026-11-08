using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using MoreSlugcats;
using Watcher;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicSpawnsTests")]

namespace DynamicPupSpawns;

public class DPSMain
{
    public enum OfficialCampaignIDs
    {
        BaseGame,
        DownPour,
        WatcherDLC
    }
    
    internal DPSOptionsMenu Options;

    private readonly ManualLogSource _logger = new ManualLogSource("Dynamic Pup Spawns");
    
    private readonly BaseUnityPlugin _unityPlugin;
    
    private World _world;
    private Dictionary<string, List<PersistentPupData>> _persistentPups;
    private List<CustomSettingsWrapper> _settings;
    internal Dictionary<string, string> ContentToModIDMap;
    private Dictionary<string, string> _currentPupsPerRegion = new ();

    private const string SaveDataDelimiter = "DynamicPupSpawnsData";
    private const string RegxStrSplit = "<WM,DPS>";
    private const string DataDivider = ":";
    private const string CampaignSettingsDelim = "campaigns";
    private const string CampaignSettingsDivide = "campaign";
    private const string RegionSettingsDelim = "regions";
    private const string RegionSettingsDivide = "region";

    
    //private int _parseSettingsRecursed = 0;

    public DPSMain()
    {
        //FOR UNIT TESTS ONLY
    }
    
    public DPSMain(BaseUnityPlugin unityPlugin)
    {
        _unityPlugin = unityPlugin;
    }

    internal int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
    {
        _world = self;
        
        //default spawning settings for campaigns without specified spawn settings
        int minPupsInRegion = Options.MinPups.Value;
        int maxPupsInRegion = Options.MaxPups.Value;
        float spawnChance = Options.SpawnChance.Value * 0.01f;
        
        //turns on or off whether campaigns without specified spawn settings have persistent pups;
        // overriden by specified settings
        bool pupPersistence = Options.Persistence.Value;
        
        //turns on or off whether campaigns without specified spawn settings have dynamic pups;
        // overridden by specified settings
        bool spawnsPups = Options.DynamicSpawnsPossible.Value 
                          && (self.game.GetStorySession.slugPupMaxCount != 0 
                              || Options.OverrideNoPupCampaigns.Value);
        
        //check for settings
        CustomSettingsObject currentSettings = GetRuntimeSettings(_world);
        if (currentSettings != null)
        {
            minPupsInRegion = currentSettings.PupSpawnSettings.MinPups;
            maxPupsInRegion = currentSettings.PupSpawnSettings.MaxPups;
            spawnChance = currentSettings.PupSpawnSettings.SpawnChance;
            spawnsPups = currentSettings.PupSpawnSettings.SpawnsDynamicPups;
            pupPersistence = currentSettings.PupSpawnSettings.PersistentPups;
            
            _logger.LogInfo("Applied returned custom settings!");
        }

        Debug.Log("DynamicPupSpawns: " + minPupsInRegion + " min, " + maxPupsInRegion + " max, " +
                  spawnChance.ToString("P0") + " chance");
        
        //generate number of pups for this cycle
        // + 1 to max to account for rounding down w/ cast to int
        int pupNum = GetRandomNumOfPups(Options.RandomizeUsingGaussian.Value, minPupsInRegion, maxPupsInRegion + 1);

        //respawn pups from save data
        if (pupPersistence)
        {
            //subtract preexisting pups from the number of new pups to be generated
            pupNum = SpawnPersistentPups(_world, pupNum);
        }
        
        if (spawnsPups)
        {
            bool spawnThisCycle = DoPupsSpawn(spawnChance);

            if (spawnThisCycle && pupNum > 0)
            {
                Debug.Log("DynamicPupSpawns: spawning " + pupNum + " new pups this cycle");
                
                //get rooms with unsubmerged den nodes
                Dictionary<AbstractRoom, int> validSpawnRooms = GetSpawnRooms(_world);

                //determine room spawn weight based on number of dens in room
                Dictionary<AbstractRoom, float> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);

                //get dict of rooms and weights in parallel arrays
                Dictionary<AbstractRoom[], float[]> parallelArrays = CreateParallelRoomWeightArrays(roomWeights);
                float[] weightsScale = AssignSortedRoomScaleValues(parallelArrays.ElementAt(0).Value);
            
                //get random room for each pup
                for (int i = 0; i < pupNum; i++)
                {
                    AbstractRoom spawnRoom = PickRandomRoomByWeight(parallelArrays.ElementAt(0).Key, weightsScale);
                    if (self.game.IsStorySession)
                    {
                        _unityPlugin.StartCoroutine(PutPupInRoom(self.game, _world, spawnRoom, null, false, self.game.GetStorySession.characterStats.name));
                    }
                }
            }
            else if (!spawnThisCycle)
            {
                Debug.Log("DynamicPupSpawns: Chance to spawn new pups failed this cycle!");
            }
            else if (pupNum > 0)
            {
                Debug.Log("DynamicPupSpawns: WARNING! The number of possible pups this cycle was greater than zero, but no pups spawned!");
                _logger.LogWarning("The number of possible pups this cycle was greater than zero, but no pups spawned!");
            }
        }
        else if (!Options.DynamicSpawnsPossible.Value)
        {
            Debug.Log("DynamicPupSpawns: Dynamic spawning is turned off in the remix menu!");
        }
        else //!spawnsPups
        {
            Debug.Log("DynamicPupSpawns: Pups cannot spawn in this campaign or region!");
        }

        return orig(self);
    }

    private CustomSettingsObject GetRuntimeSettings(World world)
    {
        CustomSettingsWrapper runtimeSettings = null;
        CustomSettingsObject returnedRuntimeSettings = null;
        
        if (ContentToModIDMap.TryGetValue(world.game.GetStorySession.saveStateNumber.ToString(), out string mod))
        {
            foreach (CustomSettingsWrapper wrapper in _settings)
            {
                if (wrapper.ModID == mod)
                {
                    _logger.LogInfo("Found settings wrapper for " + mod + "!");
                    runtimeSettings = wrapper;
                    break;
                }
            }
        }

        if (runtimeSettings == null)
        {
            return null;
        }
        
        //find specific campaign settings
        CustomSettingsObject trySettings = runtimeSettings.GetSettings(CustomSettingsObject.SettingsType.Campaign,
            world.game.GetStorySession.saveStateNumber.ToString());
        
        //check for save-state-level overrides
        if (trySettings != null)
        {
            bool overridden = CheckForCampaignOverrides(trySettings);

            if (overridden)
            {
                _logger.LogInfo("Custom runtime settings were overridden by generals!");
                return null;
            }
        }
        
        //find region specific settings
        if (trySettings == null)
        {
            trySettings = runtimeSettings.GetSettings(CustomSettingsObject.SettingsType.Region, world.name);
            if (trySettings == null)
            {
                return null;
            }
        }

        //apply campaign- or region-specific settings
        returnedRuntimeSettings = trySettings;

        string appliedSettingsLog = "Found custom settings from " + runtimeSettings.ModID + " for " + trySettings.ID;
        
        if (trySettings.HasOverrides())
        {
            CustomSettingsObject overrideSettings = null;
            
            switch (trySettings.SettingType)
            {
                case CustomSettingsObject.SettingsType.Campaign:
                    overrideSettings = trySettings.GetOverride(world.name.ToLower());
                    break;
                default:
                    _logger.LogInfo("Found invalid override type when looking for overrides");
                    break;
            }
            
            if (overrideSettings != null)
            {
                returnedRuntimeSettings = overrideSettings;

                appliedSettingsLog += "; Found override " + overrideSettings.ID;
            }
        }
        
        _logger.LogInfo(appliedSettingsLog);
        
        return returnedRuntimeSettings;
    }
    
    internal bool CheckForCampaignOverrides(CustomSettingsObject campaign)
    {
        if (Options == null)
        {
            _logger.LogError("Options menu was not initialized in time for checking overrides!");
            return false;
        }
        
        //check base game slugcats
        if (campaign.ID == SlugcatStats.Name.White.ToString() && Options.OverrideSurvivor.Value)
        {
            return true;
        }
        if (campaign.ID == SlugcatStats.Name.Yellow.ToString() && Options.OverrideMonk.Value)
        {
            return true;
        }
        if (campaign.ID == SlugcatStats.Name.Red.ToString() && Options.OverrideHunter.Value)
        {
            return true;
        }

        //check downpour slugcats
        if (ModManager.MSC)
        {
            if (campaign.ID == MoreSlugcatsEnums.SlugcatStatsName.Gourmand.ToString() && Options.OverrideGourmand.Value)
            {
                return true;
            }
            if (campaign.ID == MoreSlugcatsEnums.SlugcatStatsName.Artificer.ToString() && Options.OverrideArtificer.Value)
            {
                return true;
            }
            if (campaign.ID == MoreSlugcatsEnums.SlugcatStatsName.Spear.ToString() && Options.OverrideSpearmaster.Value)
            {
                return true;
            }
            if (campaign.ID == MoreSlugcatsEnums.SlugcatStatsName.Rivulet.ToString() && Options.OverrideRivulet.Value)
            {
                return true;
            }
            if (campaign.ID == MoreSlugcatsEnums.SlugcatStatsName.Saint.ToString() && Options.OverrideSaint.Value)
            {
                return true;
            }
        }

        //check Watcher
        if (ModManager.Watcher)
        {
            if (campaign.ID == WatcherEnums.SlugcatStatsName.Watcher.ToString() && Options.OverrideWatcher.Value)
            {
                return true;
            }
        }

        //mods
        if (Options.OverrideMods.Value)
        {
            return true;
        }
        
        return false;
    }
    
    private int GetRandomNumOfPups(bool gaussian, int min, int max) 
    {
        if (gaussian)
        {
            return RandomPupGaussian(min, max);
        }

        return UnityEngine.Random.Range(min, max);
    }
    
    private int RandomPupGaussian(int min, int max)
    {
        //thanks lancelot18

        float u, v, s;

        do
        {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            s = u * u + v * v;
        } while (s >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(s) / s);

        // clamped following the "three-sigma rule"
        float mean = min + (max - min) * 0.3f;
        float sigma = (max - mean) / 3.0f;
        float result = Mathf.Clamp(std * sigma + mean, min, max);

        return (int)result;
    }
    
    private bool DoPupsSpawn(float spawnChance)
    {
        if (Random.value < spawnChance)
        {
            return true;
        }
        return false;
    }
    
    private Dictionary<AbstractRoom, int> GetSpawnRooms(World world)
    {
        //get all rooms in region with den nodes that are not submerged
        Dictionary<AbstractRoom, int> validRoomsAndDensInRoom = new Dictionary<AbstractRoom, int>();

        int densInRoom = 0;
        foreach (AbstractRoom room in world.abstractRooms)
        {
            if (!room.offScreenDen /*&& !room.gate*/)
            {
                foreach (AbstractRoomNode node in room.nodes)
                {
                    if (node.type == AbstractRoomNode.Type.Den && (!node.submerged || Options.AllowSubmergedDens.Value))
                    {
                        densInRoom++;
                    }
                }

                if (densInRoom != 0 || Options.UseAllRooms.Value)
                {
                    validRoomsAndDensInRoom.Add(room, densInRoom);
                }

                densInRoom = 0;
            }
        }

        return validRoomsAndDensInRoom;
    }

    private Dictionary<AbstractRoom, float> CalculateRoomSpawnWeight(Dictionary<AbstractRoom, int> roomsAndDens)
    {
        //determine chance of pups spawning vs other rooms
        Dictionary<AbstractRoom, float> spawnWeights = new Dictionary<AbstractRoom, float>();

        int totalDens = 0;
        int roomsWithDens = 0;
        foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
        {
            totalDens += pair.Value;
            roomsWithDens++;
        }


        int averageDensPerRoom = -1;
        if (roomsWithDens > 0)
        {
            averageDensPerRoom = Convert.ToInt32(totalDens / roomsWithDens);
        }
        
            
        foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
        {
            if (pair.Value <= 0 && Options.UseAllRooms.Value)
            {
                spawnWeights.Add(pair.Key, averageDensPerRoom / (float)totalDens);
                continue;
            }
            spawnWeights.Add(pair.Key, pair.Value / (float)totalDens);
        }

        return spawnWeights;
    }

    private Dictionary<AbstractRoom[], float[]> CreateParallelRoomWeightArrays(
        Dictionary<AbstractRoom, float> roomWeights)
    {
        //move weights and rooms into parallel arrays
        float[] weights = new float[roomWeights.Count];
        AbstractRoom[] rooms = new AbstractRoom[roomWeights.Count];
        int index = 0;
        foreach (KeyValuePair<AbstractRoom, float> pair in roomWeights)
        {
            weights[index] = pair.Value;
            rooms[index] = pair.Key;
            index++;
        }

        //dict to be returned
        Dictionary<AbstractRoom[], float[]> parallelArrays = new Dictionary<AbstractRoom[], float[]>
            { { rooms, weights } };

        return parallelArrays;
    }

    private float[] AssignSortedRoomScaleValues(float[] weightsArray)
    {
        //change indexes of weightsArray to increment to total weight in ascending order
        float scaleValue = 0f;
        for (int i = 0; i < weightsArray.Length; i++)
        {
            scaleValue += weightsArray[i];
            weightsArray[i] = scaleValue;
        }

        return weightsArray;
    }

    private AbstractRoom PickRandomRoomByWeight(AbstractRoom[] roomsArray, float[] weightsArray)
    {
        if (!Options.WeighRooms.Value)
        {
            int numOfRooms = roomsArray.Length;
            int randomlyChosenRoom = Random.Range(0, numOfRooms);
            /*Unity.Random's int range has an exclusive max value,
             so no need to worry about out of array bounds error here*/
            
            return roomsArray[randomlyChosenRoom];
        }
        
        //pick room that corresponds to the randomly selected number on the weight scale
        float totalWeight = weightsArray[weightsArray.Length - 1];

        int roomIndex;
        float randNum = Random.Range(0f, totalWeight);
        for (roomIndex = 0; roomIndex < weightsArray.Length; roomIndex++)
        {
            if (roomIndex == weightsArray.Length - 1)
            {
                break;
            }

            if (weightsArray[roomIndex] <= randNum && randNum <= weightsArray[roomIndex + 1])
            {
                break;
            }
        }

        return roomsArray[roomIndex];
    }

    private int SpawnPersistentPups(World world, int pupNum)
    {
        if (_persistentPups != null)
        {
            if (_persistentPups.TryGetValue(world.game.GetStorySession.saveStateNumber.ToString().ToLower(),
                    out var campaignPupsList))
            {
                foreach (PersistentPupData pup in campaignPupsList)
                {
                    string[] region = pup.Room.Split('_');
                    if (region.Length >= 1)
                    {
                        if (world.region.name.ToLower() == region[0].ToLower())
                        {
                            AbstractRoom room = world.GetAbstractRoom(pup.Room);
                            if (room != null)
                            {
                                _unityPlugin.StartCoroutine(PutPupInRoom(world.game, world, room, pup.PupID, pup.IsTame,
                                    world.game.GetStorySession.characterStats.name));
                                pupNum--; //persistent pups count towards the total pups spawned per cycle
                            }
                            else
                            {
                                _logger.LogInfo("Room " + pup.Room + " pulled from save data not recognized!");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Region acronym could not be pulled from room name " + pup.Room + "!");
                    }
                }
            }
            else
            {
                _logger.LogInfo("No saved pups were found for " + world.game.GetStorySession.saveStateNumber + " at time of placement.");
            }
        }
        else
        {
            _logger.LogInfo("There was no saved pup data!");
        }

        return pupNum;
    }

    public IEnumerator PutPupInRoom(RainWorldGame game, World world, AbstractRoom room, string pupID, bool isTame,
        SlugcatStats.Name curSlug)
    {
        if (ModManager.MSC && game.IsStorySession)
        {
            bool persistent = false;
            EntityID id;
            if (pupID != null)
            {
                id = EntityID.FromString(pupID);
                persistent = true;
            }
            else
            {
                //spawn new pup with random ID
                id = game.GetNewID();
            }

            //copied from AbstractRoom.RealizeRoom()
            AbstractCreature abstractPup = new AbstractCreature(world,
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC),
                null,
                new WorldCoordinate(room.index, -1, -1, 0),
                id);

            room.AddEntity(abstractPup);
            if (room.realizedRoom != null)
            {
                //thanks pipe cleaner!!
                var gameInstance = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                while (RWCustom.Custom.rainWorld.processManager.currentMainLoop == gameInstance
                       && !abstractPup.slatedForDeletion
                       && !abstractPup.Room.realizedRoom.readyForAI) yield return null;
                if (RWCustom.Custom.rainWorld.processManager.currentMainLoop != gameInstance
                    || abstractPup.slatedForDeletion) yield break;
                
                abstractPup.RealizeInRoom();
            }
            try
            {
                (abstractPup.state as PlayerNPCState).foodInStomach = 1;
                //(abstractPup.abstractAI as SlugNPCAbstractAI).isTamed = isTame;
                /*^ seems to have no effect on actual pup tamed status, removed to avoid undefined behavior
                 until actual pups spawning tamed is implemented*/
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            _logger.LogInfo(abstractPup.creatureTemplate.type + " " + abstractPup.ID + " spawned in " +
                            abstractPup.Room.name + (persistent ? " PERSISTENT" : "")
                            + (isTame ? " TAMED" : ""));
            Debug.Log("DynamicPupSpawns: " + abstractPup.creatureTemplate.type + " " + abstractPup.ID +
                      " spawned in " + abstractPup.Room.name + (persistent ? " PERSISTENT" : "")
                      + (isTame ? " TAMED" : ""));
            
        }
    }

    private string PupsInRegionToString()
    {
        string regionPups = SaveDataDelimiter;
        
        //string message = "Adding pups to save data...\n";
        if (_world != null)
        {
            for (int i = 0; i < _world.abstractRooms.Length; i++)
            {
                if (_world.abstractRooms[i].shelter)
                {
                    bool playersShelter = false;
                    
                    foreach (AbstractCreature abstractCreature in _world.abstractRooms[i].creatures)
                    {
                        if (abstractCreature.ID.number < 1000)
                        {
                            // message += "Found shelter " + _world.abstractRooms[i].name + " for "
                            //            + abstractCreature.type + " " + abstractCreature.ID.number + "; skipping\n";
                            playersShelter = true;
                            break;
                        }
                    }

                    if (playersShelter)
                    {
                        continue;
                    }
                }
                
                foreach (AbstractCreature abstractCreature in _world.abstractRooms[i].creatures)
                {
                    if (abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
                        && !abstractCreature.state.dead)
                    {
                        regionPups += abstractCreature.ID + DataDivider 
                                    + _world.game.GetStorySession.saveStateNumber + DataDivider
                                    + _world.abstractRooms[i].name;
                        try
                        {
                            regionPups += DataDivider + (abstractCreature.abstractAI as SlugNPCAbstractAI).isTamed;
                        }
                        catch (NullReferenceException e)
                        {
                            Debug.LogException(e);
                            _logger.LogError(e.Message);
                        }

                        regionPups += RegxStrSplit;
                    }
                }
            }
            
            //remove trailing split sequence to prevent unrecognized data pair error at end in ExtractSaveValues()
            regionPups = regionPups.Remove(regionPups.Length - RegxStrSplit.Length, RegxStrSplit.Length);
            //message += "Final save string: " + modSaveData;
            //_logger.LogInfo(message);
        }
        else
        {
            _logger.LogError("_world was null, cannot save abstract pups!");
        }

        return regionPups;
    }
    
    internal void SavePupsOnRegionLeave(On.OverWorld.orig_GateRequestsSwitchInitiation orig, OverWorld self, RegionGate reportBackToGate)
    {
        UpdateCurrentPupsPerRegion();

        orig(self, reportBackToGate);
    }

    private void UpdateCurrentPupsPerRegion()
    {
        string regionName = _world.name;

        if (_currentPupsPerRegion.TryGetValue(regionName, out _))
        {
            _currentPupsPerRegion[regionName] = PupsInRegionToString();
        }
        else
        {
            _currentPupsPerRegion.Add(regionName, PupsInRegionToString());
        }
    }

    internal string SaveDataToString(On.SaveState.orig_SaveToString orig, SaveState self)
    {
        UpdateCurrentPupsPerRegion();
        
        string gameSaveData = orig(self);
        string modSaveData = SaveDataDelimiter;
        foreach (KeyValuePair<string, string> pair in _currentPupsPerRegion)
        {
            modSaveData += pair.Value + RegxStrSplit;
        }
        
        gameSaveData = String.Concat(gameSaveData, modSaveData, "<svA>");

        return gameSaveData;
    }

    internal void GetSaveDataFromString(On.SaveState.orig_LoadGame orig, SaveState self, string str,
        RainWorldGame game)
    {
        orig(self, str, game);

        string message = "Looking for mod save string from SaveState... ";

        string modString = null;
        for (int i = 0; i < self.unrecognizedSaveStrings.Count; i++)
        {
            if (self.unrecognizedSaveStrings[i].StartsWith(SaveDataDelimiter))
            {
                modString = self.unrecognizedSaveStrings[i];
                message += "String found!";
                self.unrecognizedSaveStrings.RemoveAt(i);
                modString = modString.Substring(SaveDataDelimiter.Length);
                break;
            }
        }

        if (modString == null)
        {
            message += "Couldn't find mod save data!";
            _logger.LogInfo(message);
        }
        else
        {
            _logger.LogInfo(message);
            _logger.LogInfo(modString);
            ExtractSaveValues(modString);
        }
    }

    private void ExtractSaveValues(string modString)
    {
        string[] pups = Regex.Split(modString, RegxStrSplit);
        
        if (_persistentPups == null)
        {
            _persistentPups = new Dictionary<string, List<PersistentPupData>>();
        }
        else
        {
            _persistentPups.Clear();
        }

        string[] pupValues;
        foreach (string pup in pups)
        {
            pupValues = Regex.Split(pup, DataDivider);
            if (pupValues.Length >= 4)
            {
                string id = pupValues[0];
                string campaign = pupValues[1];
                string room = pupValues[2];
                bool isTame = pupValues[3].ToLower().StartsWith("t");

                if (_persistentPups.TryGetValue(campaign.ToLower(), out var campaignPupsList))
                {
                    campaignPupsList.Add(new PersistentPupData(id, room, isTame));
                }
                else
                {
                    List<PersistentPupData> newCampaignPupList = new List<PersistentPupData> {new (id, room, isTame)};
                    _persistentPups.Add(campaign.ToLower(), newCampaignPupList);
                }
            }
            else
            {
                _logger.LogWarning("Found invalid data set while loading pups from save data! Check the save string is formatted correctly.");
                //_logger.LogInfo("pupValues length: " + pupValues.Length);
            }
        }

        // string debugMessage = "New persistent pups dictionary:\n";
        // foreach (KeyValuePair<string, List<PersistentPupData>> pair in _persistentPups)
        // {
        //     debugMessage += "Campaign id: " + pair.Key + "\n";
        //     foreach (PersistentPupData pup in pair.Value)
        //     {
        //         debugMessage += "\tPup: " + pup.ID + ", " + pup.Room + ", " + pup.IsTame + "\n";
        //     }
        // }
        // _logger.LogInfo(debugMessage);
    }


    internal void LogPupDeath(On.Creature.orig_Die orig, Creature self)
    {
        if (self.GetType() == typeof(Player)
            && (self as Player).isNPC
            && !self.dead)
        {
            string deathMessage =
                self.abstractCreature.creatureTemplate.type + " " + self.abstractCreature.ID + " died in " +
                self.room.abstractRoom.name + "! Cause: ";
            if (self.killTag != null)
            {
                if (self.killTag.realizedCreature != null)
                {
                    deathMessage += self.killTag.realizedCreature.abstractCreature.creatureTemplate.type.ToString();
                }
                else
                {
                    deathMessage += "Unknown Entity";
                }
            }
            else if (ModManager.MSC && self.Hypothermia >= 2f)
            {
                deathMessage += "Hypothermia";
            }
            else if (self.Submersion >= 1f)
            {
                deathMessage += "Drowning";
            }
            else if (ModManager.Watcher && self.injectedPoison > 0)
            {
                deathMessage += "Poison";
            }
            else if (self.abstractCreature.stuckObjects.Count > 0)
            {
                deathMessage += self.abstractCreature.stuckObjects.LastOrDefault();
            }
            else
            {
                deathMessage += "Unknown";
            }

            _logger.LogInfo(deathMessage);
            Debug.Log(deathMessage);
        }

        orig(self);
    }

    internal void SetupModSettings(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        LoadOptionsMenu();
        CreateSettingsObjects();
    }
    
    public void CreateOptionsMenuInstance()
    {
        Options = new DPSOptionsMenu((DynamicPupSpawnsPlugin)_unityPlugin);
    }
    
    private void LoadOptionsMenu()
    {
        try
        {
            MachineConnector.SetRegisteredOI(DynamicPupSpawnsPlugin.ModID, Options);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _logger.LogError(e.Message);
        }
    }
    
    private void CreateSettingsObjects()
    {
        if (ContentToModIDMap == null)
        {
            ContentToModIDMap = new Dictionary<string, string>();
        }
        else
        {
            ContentToModIDMap.Clear();
        }
        
        ProcessCustomData();
        ProcessBuiltInSettings();
        
        //LogAllSettings();
    }

    private void ProcessBuiltInSettings()
    {
        if (Options == null)
        {
            _logger.LogWarning("The options menu was not initialized in time to add official slugcat settings!");
            return;
        }
        
        if (_settings == null)
        {
            _logger.LogInfo("Creating new global settings list");
            _settings = new List<CustomSettingsWrapper>();
        }
        
        //add ids and mod id to _contentToModIDMap for easier setting retrieval at runtime
        if (ContentToModIDMap == null)
        {
            ContentToModIDMap = new Dictionary<string, string>();
        }
        
        AddBaseGameSettings();
        
        if (ModManager.MSC)
        {
            AddDownpourSettings();
        }

        if (ModManager.Watcher)
        {
            AddWatcherSettings();
        }
    }

    private void AddBaseGameSettings()
    {
        CustomSettingsObject fpOverride =
            new CustomSettingsObject(CustomSettingsObject.SettingsType.Region, "ss", new PupSpawnSettings());
        
        string baseGameID = nameof(OfficialCampaignIDs.BaseGame);
        string survivorID = SlugcatStats.Name.White.ToString();
        string monkID = SlugcatStats.Name.Yellow.ToString();
        string hunterID = SlugcatStats.Name.Red.ToString();
        
        CustomSettingsWrapper baseGameSettings = new CustomSettingsWrapper(baseGameID);
        
        CustomSettingsObject survivorSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            survivorID,
            new PupSpawnSettings(Options.SurvivorPupsSpawn.Value,
                Options.SurvivorMinPups.Value,
                Options.SurvivorMaxPups.Value,
                Options.SurvivorSpawnChance.Value * 0.01f,
                Options.SurvivorPersistence.Value));
        survivorSettings.AddOverride(fpOverride);
        
        CustomSettingsObject monkSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            monkID,
            new PupSpawnSettings(Options.MonkPupsSpawn.Value,
                Options.MonkMinPups.Value,
                Options.MonkMaxPups.Value,
                Options.MonkSpawnChance.Value * 0.01f,
                Options.MonkPersistence.Value));
        monkSettings.AddOverride(fpOverride);
        
        CustomSettingsObject hunterSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            hunterID,
            new PupSpawnSettings(Options.HunterPupsSpawn.Value,
                Options.HunterMinPups.Value,
                Options.HunterMaxPups.Value,
                Options.HunterSpawnChance.Value * 0.01f,
                Options.HunterPersistence.Value));
        hunterSettings.AddOverride(fpOverride);

        baseGameSettings.AddSetting(survivorSettings);
        baseGameSettings.AddSetting(monkSettings);
        baseGameSettings.AddSetting(hunterSettings);
        
        ContentToModIDMap.Add(survivorID, baseGameID);
        ContentToModIDMap.Add(monkID, baseGameID);
        ContentToModIDMap.Add(hunterID, baseGameID);
        
        _settings.Add(baseGameSettings);
    }
    
    private void AddDownpourSettings()
    {
        CustomSettingsObject fpOverride =
            new CustomSettingsObject(CustomSettingsObject.SettingsType.Region, "ss", new PupSpawnSettings());
        
        string downpourID = nameof(OfficialCampaignIDs.DownPour);
        string gourmandID = MoreSlugcatsEnums.SlugcatStatsName.Gourmand.ToString();
        string artificerID = MoreSlugcatsEnums.SlugcatStatsName.Artificer.ToString();
        string spearmasterID = MoreSlugcatsEnums.SlugcatStatsName.Spear.ToString();
        string rivuletID = MoreSlugcatsEnums.SlugcatStatsName.Rivulet.ToString();
        string saintID = MoreSlugcatsEnums.SlugcatStatsName.Saint.ToString();
        
        CustomSettingsWrapper downpourSettings = new CustomSettingsWrapper(downpourID);
        
        CustomSettingsObject gourmandSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            gourmandID,
            new PupSpawnSettings(Options.GourmandPupsSpawn.Value,
                Options.GourmandMinPups.Value,
                Options.GourmandMaxPups.Value,
                Options.GourmandSpawnChance.Value * 0.01f,
                Options.GourmandPersistence.Value));
        gourmandSettings.AddOverride(fpOverride);
        
        CustomSettingsObject artificerSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            artificerID,
            new PupSpawnSettings(Options.ArtificerPupsSpawn.Value,
                Options.ArtificerMinPups.Value,
                Options.ArtificerMaxPups.Value,
                Options.ArtificerSpawnChance.Value * 0.01f,
                Options.ArtificerPersistence.Value));
        artificerSettings.AddOverride(fpOverride);
        
        CustomSettingsObject spearmasterSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            spearmasterID,
            new PupSpawnSettings(Options.SpearmasterPupsSpawn.Value,
                Options.SpearmasterMinPups.Value,
                Options.SpearmasterMaxPups.Value,
                Options.SpearmasterSpawnChance.Value * 0.01f,
                Options.SpearmasterPersistence.Value));
        spearmasterSettings.AddOverride(fpOverride);
        spearmasterSettings.AddOverride(new CustomSettingsObject(CustomSettingsObject.SettingsType.Region, "dm", new PupSpawnSettings()));
        
        CustomSettingsObject rivuletSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            rivuletID,
            new PupSpawnSettings(Options.RivuletPupsSpawn.Value,
                Options.RivuletMinPups.Value,
                Options.RivuletMaxPups.Value,
                Options.RivuletSpawnChance.Value * 0.01f,
                Options.RivuletPersistence.Value));
        rivuletSettings.AddOverride(fpOverride);
        
        CustomSettingsObject saintSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            saintID,
            new PupSpawnSettings(Options.SaintPupsSpawn.Value,
                Options.SaintMinPups.Value,
                Options.SaintMaxPups.Value,
                Options.SaintSpawnChance.Value * 0.01f,
                Options.SaintPersistence.Value));
        saintSettings.AddOverride(fpOverride);

        downpourSettings.AddSetting(gourmandSettings);
        downpourSettings.AddSetting(artificerSettings);
        downpourSettings.AddSetting(spearmasterSettings);
        downpourSettings.AddSetting(rivuletSettings);
        downpourSettings.AddSetting(saintSettings);

        string[] idsArray =
        {
            gourmandID,
            artificerID,
            spearmasterID,
            rivuletID,
            saintID
        };

        foreach (string settingID in idsArray)
        {
            if (!ContentToModIDMap.ContainsKey(settingID))
            {
                ContentToModIDMap.Add(settingID, downpourID);
            }
            else
            {
                _logger.LogWarning("The key " + settingID + " is already present in the content map!");
            }
        }
        
        _settings.Add(downpourSettings);
    }

    private void AddWatcherSettings()
    {
        string watcherDLCID = nameof(OfficialCampaignIDs.WatcherDLC);
        string watcherID = WatcherEnums.SlugcatStatsName.Watcher.ToString();
        
        CustomSettingsWrapper watcherDLCSettings = new CustomSettingsWrapper(watcherDLCID);
        
        CustomSettingsObject watcherSettings = new CustomSettingsObject(
            CustomSettingsObject.SettingsType.Campaign,
            watcherID,
            new PupSpawnSettings(Options.WatcherPupsSpawn.Value,
                Options.WatcherMinPups.Value,
                Options.WatcherMaxPups.Value,
                Options.WatcherSpawnChance.Value * 0.01f,
                Options.WatcherPersistence.Value));
        
        watcherDLCSettings.AddSetting(watcherSettings);
        
        ContentToModIDMap.Add(watcherID, watcherDLCID);
        
        _settings.Add(watcherDLCSettings);
    }
    
    private void ProcessCustomData()
    {
        if (_settings == null)
        {
            _logger.LogInfo("Creating new global settings list");
            _settings = new List<CustomSettingsWrapper>();
        }

        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            bool depends = false;
            string filePath = mod.path + "\\dynamicpups\\settings.txt";
            for (int i = 0; i < mod.requirements.Length; i++)
            {
                if (mod.requirements[i] == DynamicPupSpawnsPlugin.ModID)
                {
                    depends = true;
                    _logger.LogInfo("Found dependent!: " + mod.name);
                    ProcessModSettings(filePath, mod.id, false);
                    break;
                }
            }
        
            if (!depends)
            {
                for (int i = 0; i < mod.priorities.Length; i++)
                {
                    if (mod.priorities[i] == DynamicPupSpawnsPlugin.ModID)
                    {
                        _logger.LogInfo("Found priority!: " + mod.name);
                        ProcessModSettings(filePath, mod.id, false);
                        break;
                    }
                }
            }
        }

        // string[] testSettingsArray = SettingTestData();
        // for (int i = 0; i < testSettingsArray.Length; i++)
        // {
        //     ProcessModSettings(testSettingsArray[i], "Test Data " + (i + 1), true);
        // }

        // string message = "Finished processing custom settings for dependents!:";
        // foreach (CustomSettingsWrapper wrapper in _settings)
        // {
        //     message += "\n" + wrapper.ToString();
        // }
        // _logger.LogInfo(message);
    }
    
    private void ProcessModSettings(string filePath, string modID, bool testing)
    {
        _logger.LogInfo("Parsing settings for " + modID + ":");

        CustomSettingsWrapper modSettings = new CustomSettingsWrapper(modID);
        List<string> symbols = new List<string>();
        
        try
        {
            string settings;
            if (!testing)
            {
                settings = File.ReadAllText(filePath);
            }
            else
            {
                settings = filePath;
            }
            settings = Regex.Replace(settings, @"\s+", "");
            symbols = ParseSymbols(settings);
        }
        catch (FileNotFoundException e)
        {
            _logger.LogError("Couldn't find the custom settings file for " + modID + "!");
            Debug.LogException(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            Debug.LogException(e);
        }
        
        modSettings = ParseGeneralSettings(symbols, modSettings);
        if (modSettings != null)
        {
            _settings.Add(modSettings);
        }
        
        _logger.LogInfo("Finished parsing for " + modID + "!");
    }

    internal CustomSettingsWrapper ParseGeneralSettings(List<string> symbols,
        CustomSettingsWrapper settings,
        Dictionary<string, string> contentMap = null)
    {
        //for unit test compatibility
        if (contentMap == null)
        {
            contentMap = ContentToModIDMap;
        }

        ManualLogSource log = new ManualLogSource("Dynamic Pup Spawns : ParseGeneralSettings");
        
        for (int i = 0; i < symbols.Count; i++)
        {
            if (symbols[i].ToLower() == CampaignSettingsDelim)
            {
                List<string> cSettings = new List<string>();
                for (i++; i < symbols.Count; i++)
                {
                    if (symbols[i].ToLower() == CampaignSettingsDivide
                        || symbols[i].ToLower() == RegionSettingsDelim)
                    {
                        settings = AddSetting(cSettings, settings, CustomSettingsObject.SettingsType.Campaign);
                        cSettings.Clear();
                        if (symbols[i].ToLower() == RegionSettingsDelim)
                        {
                            i--;
                            break;
                        }
                        continue;
                    }
                    cSettings.Add(symbols[i]);
                }
                if (cSettings.Count > 0)
                {
                    settings = AddSetting(cSettings, settings, CustomSettingsObject.SettingsType.Campaign);
                }
                continue;
            }
            if (symbols[i].ToLower() == RegionSettingsDelim)
            {
                List<string> rSettings = new List<string>();

                for (i++; i < symbols.Count; i++)
                {
                    if (symbols[i].ToLower() == RegionSettingsDivide
                        || symbols[i].ToLower() == CampaignSettingsDelim)
                    {
                        settings = AddSetting(rSettings, settings, CustomSettingsObject.SettingsType.Region);
                        rSettings.Clear();
                        if (symbols[i].ToLower() == CampaignSettingsDelim)
                        {
                            i--;
                            break;
                        }
                        continue;
                    }
                    rSettings.Add(symbols[i]);
                }
                if (rSettings.Count > 0)
                {
                    settings = AddSetting(rSettings, settings, CustomSettingsObject.SettingsType.Region);
                }
            }
        }

        if (!settings.HasCampaignSettings() && !settings.HasRegionSettings())
        {
            PrintSettingsReturnError("CustomSettingsWrapper", "ParseGeneralSettings()", "contains no setting objects");
            return settings;
        }

        int duplicatePrevention = 2; //starts at 2 due to being appended to id name
        List<string> contentIDs = settings.GetAllSettingsIDs();
        foreach (string contentID in contentIDs)
        {
            if (contentMap.ContainsKey(contentID))
            {
                string newContentID = contentID + duplicatePrevention;
                duplicatePrevention++;
                contentMap.Add(newContentID, settings.ModID);
                log.LogInfo("Added " + newContentID + " | " + settings.ModID + " to content to ID map");
            }
            else
            {
                contentMap.Add(contentID, settings.ModID);
                log.LogInfo("Added " + contentID + " | " + settings.ModID + " to content to ID map");
            }
        }
        return settings;
    }

    private CustomSettingsWrapper AddSetting(List<string> settings, CustomSettingsWrapper wrap,
        CustomSettingsObject.SettingsType t)
    {
        //_parseSettingsRecursed = 0;
        CustomSettingsObject set = ParseSettings(settings, t);
        if (set != null)
        {
            bool succeedAdd = wrap.AddSetting(set);
            if (!succeedAdd)
            {
                _logger.LogWarning("Failed to add " + set.SettingType + " settings for " + set.ID + " to settings wrapper!");
            }
        }

        return wrap;
    }
    
    internal CustomSettingsObject ParseSettings(List<string> symbols, CustomSettingsObject.SettingsType t)
    {
        //_logger.LogInfo("Recursive passes through ParseSettings(): " + _parseSettingsRecursed);
        //_parseSettingsRecursed++;
        
        string id = "";
        PupSpawnSettings pupSettings = null;
        List<CustomSettingsObject> overridesList = null;
        
        for (int i = 0; i < symbols.Count; i++)
        {
            if (symbols[i].ToLower().StartsWith("id"))
            {
                object o = ParseValue(symbols[i]);
                if (o != null)
                {
                    id = Convert.ToString(o);
                }
            }
            else if (symbols[i].ToLower().StartsWith("pup_settings"))
            {
                object o = ParseValue(symbols[i]);
                if (o != null)
                {
                    string pupSettingString = Convert.ToString(o);
                    pupSettingString = pupSettingString.Substring(1, pupSettingString.Length - 2);
                    
                    List<string> pSettings = ParseSymbols(pupSettingString);
                    pupSettings = ParsePupSpawnSettings(pSettings);
                }
            }
            else if (symbols[i].ToLower().StartsWith("overrides"))
            {
                object o = ParseValue(symbols[i]);
                if (o != null)
                {
                    string overrideString = Convert.ToString(o);
                    overrideString = overrideString.Substring(1, overrideString.Length - 2);

                    List<string> allOverrides = ParseSymbols(overrideString, '[', ']');
                    List<string> singleObject = new List<string>();
                    overridesList = new List<CustomSettingsObject>();
                    for (int x = 0; x < allOverrides.Count; x++)
                    {
                        if (t == CustomSettingsObject.SettingsType.Campaign)
                        {
                            if (allOverrides[x].ToLower() == RegionSettingsDivide)
                            {
                                CustomSettingsObject overrideObject = ParseSettings(singleObject, CustomSettingsObject.SettingsType.Region);
                                singleObject.Clear();
                                overridesList.Add(overrideObject);
                                continue;
                            }
                            singleObject.Add(allOverrides[x]);
                        }
                    }
                    if (t == CustomSettingsObject.SettingsType.Campaign)
                    {
                        CustomSettingsObject overrideObject = ParseSettings(singleObject, CustomSettingsObject.SettingsType.Region);
                        overridesList.Add(overrideObject);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unrecognized value for " + t + " setting!: " + symbols[i]);
            }
        }

        if (id == "")
        {
            PrintSettingsReturnError("CustomSettingsObject of type " + t, "ParseSettings()", "id");
            return null;
        }
        CustomSettingsObject result;
        result = new CustomSettingsObject(t, id, pupSettings);
        
        if (overridesList != null)
        {
            foreach (CustomSettingsObject o in overridesList)
            {
                bool succeedAdd = result.AddOverride(o);
                if (!succeedAdd)
                {
                    _logger.LogWarning("You cannot add objects of type " + o.SettingType + " to overrides of objects with type " + result.SettingType);
                }
            }
        }
        
        return result;
    }

    internal PupSpawnSettings ParsePupSpawnSettings(List<string> pSettings)
    {
        if (pSettings.Count == 0)
        {
            PrintSettingsReturnError("PupSpawnSettings Object", "ParsePupSpawnSettings()", "empty values list");
            return null;
        }
        
        bool spawns = false;
        float chance = 0f;
        int min = -1;
        int max = -1;

        foreach (string s in pSettings)
        {
            object o = ParseValue(s);
            if (o != null)
            {
                if (s.ToLower().StartsWith("pupsdynamicspawn"))
                {
                    try
                    {
                        spawns = (bool)o;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);

                        if (e.GetType() == typeof(InvalidCastException))
                        {
                            PrintInvalidCastError("DoPupsSpawn Setting", "bool", e.Message);                                
                        }
                    }
                    if (!spawns)
                    {
                        return new PupSpawnSettings();
                    }
                }
                else if (s.ToLower().StartsWith("spawnchance"))
                {
                    try
                    {
                        chance = (float)o;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);

                        if (e.GetType() == typeof(InvalidCastException))
                        {
                            PrintInvalidCastError("SpawnChance Setting", "float", e.Message);
                        }
                    }
                }
                else if (s.ToLower().StartsWith("min"))
                {
                    try
                    {
                        float f = (float)o;
                        min = (int)f;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);

                        if (e.GetType() == typeof(InvalidCastException))
                        {
                            PrintInvalidCastError("MinPups Setting", "float", e.Message);
                        }
                    }
                }
                else if (s.ToLower().StartsWith("max"))
                {
                    try
                    {
                        float f = (float)o;
                        max = (int)f;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);

                        if (e.GetType() == typeof(InvalidCastException))
                        {
                            PrintInvalidCastError("MaxPups Setting", "float", e.Message);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Unrecognized value in Pup Spawns settings!");
                }
            }
            else
            {
                PrintSettingsReturnError("Setting String", "PupSpawnSettings()", "value");
            }
        }

        PupSpawnSettings result = new PupSpawnSettings(spawns, min, max, chance);
        if (!result.SetMinMaxSucceeded)
        {
            _logger.LogWarning("Failed to set min and max property in PupSpawnSettings! " + min + " > " + max);
        }
        
        return new PupSpawnSettings(spawns, min, max, chance);
    }
    
    internal List<string> ParseSymbols(string settings, char openBracket = '{', char closeBracket = '}')
    {
        StringReader reader = new StringReader(settings);
        List<string> symbols = new List<string>();
        
        string symbol = "";
        while (reader.Peek() >= 0)
        {
            char c = (char)reader.Peek();
            if (c != ';')
            {
                if (c == openBracket)
                {
                    while (reader.Peek() >= 0)
                    {
                        c = (char)reader.Peek();
                        if (c != closeBracket)
                        {
                            symbol += (char)reader.Read();
                        }
                        else
                        {
                            symbol += (char)reader.Read();
                            break;
                        }
                    }

                    continue;
                }
                symbol += (char)reader.Read();
            }
            else
            {
                reader.Read();
                symbols.Add(symbol);
                symbol = "";
            }
        }

        return symbols;
    }
    
    internal object ParseValue(string setting)
    {
        string[] value = setting.Split(":".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
        
        if (value.Length == 2)
        {
            float parsedNum;
            if (float.TryParse(value[1], out parsedNum))
            {
                return parsedNum;
            }

            if (value[1].ToLower().StartsWith("f"))
            {
                return false;
            }
            if (value[1].ToLower().StartsWith("t"))
            {
                return true;
            }

            return value[1];
        }
        return null;
    }

    private void PrintSettingsReturnError(string value, string source, string cause)
    {
        _logger.LogError("A " + value + " returned null in " + source + "! Cause: " + cause);
    }

    private void PrintInvalidCastError(string failedObj, string triedType, string message)
    {
        _logger.LogError("An invalid cast was encountered trying to convert " 
                         + failedObj + " from object to " + triedType + "!:\n" + message);
    }

    private void LogAllSettings()
    {
        if (_settings != null)
        {
            string log = "";
            
            foreach (CustomSettingsWrapper setting in _settings)
            {
                log += setting.ToString();
            }
            
            _logger.LogInfo(log);
        }
    }
}