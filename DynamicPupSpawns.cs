using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using MoreSlugcats;

namespace dynamicpupspawns
{

    [BepInPlugin(_MOD_ID, "Dynamic Pup Spawns", "0.1")]
    public class DynamicPupSpawns : BaseUnityPlugin
    {
        private const string _MOD_ID = "dynamicpupspawns";
        private DPSOptionsMenu _options;
        
        private World _world;
        private Dictionary<string, List<PersistentPupData>> _persistentPups;
        private List<CustomSettingsWrapper> _settings;
        private Dictionary<string, string> _modContentToModIDMap;
        private CustomSettingsWrapper _runtimeSettings;

        private const string _SAVE_DATA_DELIMITER = "DynamicPupSpawnsData";
        private const string _REGX_STR_SPLIT = "<WM,DPS>";
        private const string _DATA_DIVIDER = ":";
        private const string _CAMPAIGN_SETTINGS_DELIM = "campaigns";
        private const string _CAMPAIGN_SETTINGS_DIVIDE = "campaign";
        private const string _REGION_SETTINGS_DELIM = "regions";
        private const string _REGION_SETTINGS_DIVIDE = "region";

        //private int _parseSettingsRecursed = 0;
        
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;
            
            On.SaveState.SaveToString += SaveDataToString;
            On.SaveState.LoadGame += GetSaveDataFromString;

            On.Creature.Die += LogPupDeath;

            On.ModManager.WrapPostModsInit += WrapPostInit;

            CreateOptionsMenuInstance();
            On.ModManager.WrapModsInit += LoadOptionsMenu;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            _world = self;
            
            int minPupsInRegion = _options.minPups.Value;
            int maxPupsInRegion = _options.maxPups.Value;
            float spawnChance = _options.spawnChance.Value * 0.01f;
            bool pupPersistence = true;
            bool spawnsPups;
            
            //check whether campaign allows pups (i.e, surv vs saint); this value can later be overridden by custom settings
            if (self.game.GetStorySession.slugPupMaxCount != 0)
            {
                spawnsPups = true; //survivor, hunter, gourmand
            }
            else
            {
                spawnsPups = false; //monk, artificer, spearmaster, rivulet, saint, watcher
            }
            
            //check for settings
            if (_runtimeSettings == null)
            {
                if (_modContentToModIDMap.TryGetValue(self.game.GetStorySession.saveStateNumber.ToString(), out string mod))
                {
                    foreach (CustomSettingsWrapper wrapper in _settings)
                    {
                        if (wrapper.ModID == mod)
                        {
                            Logger.LogInfo("Found settings wrapper for " + mod + "!");
                            _runtimeSettings = wrapper;
                            break;
                        }
                    }
                }
            }

            //apply campaign- or region-specific settings
            if (_runtimeSettings != null)
            {
                CustomSettingsObject trySettings = null;
                
                trySettings = _runtimeSettings.GetSettings(CustomSettingsObject.SettingsType.Campaign,
                    self.game.GetStorySession.saveStateNumber.ToString());
                if (trySettings == null)
                {
                    trySettings = _runtimeSettings.GetSettings(CustomSettingsObject.SettingsType.Region, self.name);
                }

                if (trySettings != null)
                {
                    minPupsInRegion = trySettings.PupSpawnSettings.MinPups;
                    maxPupsInRegion = trySettings.PupSpawnSettings.MaxPups;
                    spawnChance = trySettings.PupSpawnSettings.SpawnChance;
                    spawnsPups = trySettings.PupSpawnSettings.SpawnsDynamicPups;

                    string appliedSettingsLog = "Applied custom settings from " + _runtimeSettings.ModID + " for " + trySettings.ID;
                    
                    if (trySettings.HasOverrides())
                    {
                        CustomSettingsObject overrideSettings = null;
                        
                        switch (trySettings.SettingType)
                        {
                            case CustomSettingsObject.SettingsType.Campaign:
                                overrideSettings = trySettings.GetOverride(self.name.ToLower());
                                break;
                            default:
                                Logger.LogInfo("Found invalid override type when looking for overrides");
                                break;
                        }
                        
                        if (overrideSettings != null) 
                        {
                            minPupsInRegion = overrideSettings.PupSpawnSettings.MinPups;
                            maxPupsInRegion = overrideSettings.PupSpawnSettings.MaxPups;
                            spawnChance = overrideSettings.PupSpawnSettings.SpawnChance;
                            spawnsPups = overrideSettings.PupSpawnSettings.SpawnsDynamicPups;

                            appliedSettingsLog += "; Applied override " + overrideSettings.ID;
                        }
                    }
                    
                    Logger.LogInfo(appliedSettingsLog);
                }
            }

            //generate number of pups for this cycle
            // + 1 to max to account for rounding down w/ cast to int
            int pupNum = RandomPupGaussian(minPupsInRegion, maxPupsInRegion + 1);

            //respawn pups from save data
            pupNum = SpawnPersistentPups(self, pupNum);
            
            if (spawnsPups)
            {
                bool spawnThisCycle = DoPupsSpawn(spawnChance);

                if (spawnThisCycle && pupNum > 0)
                {
                    Debug.Log("DynamicPupSpawns: spawning " + pupNum + " new pups this cycle");
                
                    //get rooms with unsubmerged den nodes
                    Dictionary<AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);

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
                            PutPupInRoom(self.game, self, spawnRoom, null, false, self.game.GetStorySession.characterStats.name);
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
                    Logger.LogWarning("The number of possible pups this cycle was greater than zero, but no pups spawned!");
                }
            }
            else
            {
                Debug.Log("DynamicPupSpawns: Pups cannot spawn in this campaign or region!");
            }
            
            return orig(self);
        }
        
        private int RandomPupGaussian(float min, float max)
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
        
        private Dictionary<AbstractRoom, int> GetRoomsWithViableDens(World world)
        {
            //get all rooms in region with den nodes that are not submerged
            Dictionary<AbstractRoom, int> roomsWithDens = new Dictionary<AbstractRoom, int>();

            int densInRoom = 0;
            foreach (AbstractRoom room in world.abstractRooms)
            {
                if (!room.offScreenDen /*&& !room.gate*/)
                {
                    foreach (AbstractRoomNode node in room.nodes)
                    {
                        if (node.type == AbstractRoomNode.Type.Den && !node.submerged)
                        {
                            densInRoom++;
                        }
                    }

                    if (densInRoom != 0)
                    {
                        roomsWithDens.Add(room, densInRoom);
                    }

                    densInRoom = 0;
                }
            }

            return roomsWithDens;
        }

        private Dictionary<AbstractRoom, float> CalculateRoomSpawnWeight(Dictionary<AbstractRoom, int> roomsAndDens)
        {
            //determine chance of pups spawning vs other rooms
            Dictionary<AbstractRoom, float> spawnWeights = new Dictionary<AbstractRoom, float>();

            int totalDens = 0;
            foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
            {
                totalDens += pair.Value;
            }

            foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
            {
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
                                    PutPupInRoom(world.game, world, room, pup.ID, pup.IsTame,
                                        world.game.GetStorySession.characterStats.name);
                                    pupNum--; //persistent pups count towards the total pups spawned per cycle
                                }
                                else
                                {
                                    Logger.LogInfo("Room " + pup.Room + " pulled from save data not recognized!");
                                }
                            }
                        }
                        else
                        {
                            Logger.LogError("Region acronym could not be pulled from room name " + pup.Room + "!");
                        }
                    }
                }
                else
                {
                    Logger.LogInfo("No saved pups were found for " + world.game.GetStorySession.saveStateNumber + " at time of placement.");
                }
            }
            else
            {
                Logger.LogInfo("There was no saved pup data!");
            }

            return pupNum;
        }

        public void PutPupInRoom(RainWorldGame game, World world, AbstractRoom room, string pupID, bool isTame,
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
                    abstractPup.RealizeInRoom();
                }
                try
                {
                    (abstractPup.state as PlayerNPCState).foodInStomach = 1;
                    (abstractPup.abstractAI as SlugNPCAbstractAI).isTamed = isTame;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                }

                Logger.LogInfo(abstractPup.creatureTemplate.type + " " + abstractPup.ID + " spawned in " +
                               abstractPup.Room.name + (persistent ? " PERSISTENT" : "")
                               + (isTame ? " TAMED" : ""));
                Debug.Log("DynamicPupSpawns: " + abstractPup.creatureTemplate.type + " " + abstractPup.ID +
                          " spawned in " + abstractPup.Room.name + (persistent ? " PERSISTENT" : "")
                          + (isTame ? " TAMED" : ""));
                
            }
        }

        private string SaveDataToString(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            string gameSaveData = orig(self);

            string modSaveData = _SAVE_DATA_DELIMITER;
            
            string message = "Adding pups to save data...\n";
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
                                message += "Found shelter " + _world.abstractRooms[i].name + " for "
                                           + abstractCreature.type + " " + abstractCreature.ID.number + "; skipping\n";
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
                            modSaveData += abstractCreature.ID + _DATA_DIVIDER 
                                        + _world.game.GetStorySession.saveStateNumber + _DATA_DIVIDER
                                        + _world.abstractRooms[i].name;
                            try
                            {
                                modSaveData += _DATA_DIVIDER + (abstractCreature.abstractAI as SlugNPCAbstractAI).isTamed;
                            }
                            catch (NullReferenceException e)
                            {
                                Debug.LogException(e);
                                Logger.LogError(e.Message);
                            }

                            modSaveData += _REGX_STR_SPLIT;
                        }
                    }
                }
                
                //remove trailing split sequence to prevent unrecognized data pair error at end in ExtractSaveValues()
                modSaveData = modSaveData.Remove(modSaveData.Length - _REGX_STR_SPLIT.Length, _REGX_STR_SPLIT.Length);
                message += "Final save string: " + modSaveData;
                Logger.LogInfo(message);
            }
            else
            {
                Logger.LogError("_world was null, cannot save abstract pups!");
            }

            gameSaveData = String.Concat(gameSaveData, modSaveData, "<svA>");

            return gameSaveData;
        }

        private void GetSaveDataFromString(On.SaveState.orig_LoadGame orig, SaveState self, string str,
            RainWorldGame game)
        {
            orig(self, str, game);

            string message = "Looking for mod save string from SaveState... ";

            string modString = null;
            for (int i = 0; i < self.unrecognizedSaveStrings.Count; i++)
            {
                if (self.unrecognizedSaveStrings[i].StartsWith(_SAVE_DATA_DELIMITER))
                {
                    modString = self.unrecognizedSaveStrings[i];
                    message += "String found!";
                    self.unrecognizedSaveStrings.RemoveAt(i);
                    modString = modString.Substring(_SAVE_DATA_DELIMITER.Length);
                    break;
                }
            }

            if (modString == null)
            {
                message += "Couldn't find mod save data!";
                Logger.LogInfo(message);
            }
            else
            {
                Logger.LogInfo(message);
                Logger.LogInfo(modString);
                ExtractSaveValues(modString);
            }
        }

        private void ExtractSaveValues(string modString)
        {
            string[] pups = Regex.Split(modString, _REGX_STR_SPLIT);
            
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
                pupValues = Regex.Split(pup, _DATA_DIVIDER);
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
                    Logger.LogWarning("Found invalid data set while loading pups from save data! Check the save string is formatted correctly.");
                    Logger.LogInfo("pupValues length: " + pupValues.Length);
                }
            }

            string debugMessage = "New persistent pups dictionary:\n";
            foreach (KeyValuePair<string, List<PersistentPupData>> pair in _persistentPups)
            {
                debugMessage += "Campaign id: " + pair.Key + "\n";
                foreach (PersistentPupData pup in pair.Value)
                {
                    debugMessage += "\tPup: " + pup.ID + ", " + pup.Room + ", " + pup.IsTame + "\n";
                }
            }
            Logger.LogInfo(debugMessage);
        }


        private void LogPupDeath(On.Creature.orig_Die orig, Creature self)
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

                Logger.LogInfo(deathMessage);
                Debug.Log(deathMessage);
            }

            orig(self);
        }

        private void WrapPostInit(On.ModManager.orig_WrapPostModsInit orig)
        {
            orig();
            ProcessCustomData();
            //ProcessBuiltInSettings();
        }
        
        private void ProcessCustomData()
        {
            if (_settings == null)
            {
                Logger.LogInfo("Creating new global settings list");
                _settings = new List<CustomSettingsWrapper>();
            }

            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                bool depends = false;
                string filePath = mod.path + "\\dynamicpups\\settings.txt";
                for (int i = 0; i < mod.requirements.Length; i++)
                {
                    if (mod.requirements[i] == _MOD_ID)
                    {
                        depends = true;
                        Logger.LogInfo("Found dependent!: " + mod.name);
                        ProcessSettings(filePath, mod.id, false);
                        break;
                    }
                }
            
                if (!depends)
                {
                    for (int i = 0; i < mod.priorities.Length; i++)
                    {
                        if (mod.priorities[i] == _MOD_ID)
                        {
                            Logger.LogInfo("Found priority!: " + mod.name);
                            ProcessSettings(filePath, mod.id, false);
                            break;
                        }
                    }
                }
            }

            // string[] testSettingsArray = SettingTestData();
            // for (int i = 0; i < testSettingsArray.Length; i++)
            // {
            //     ProcessSettings(testSettingsArray[i], "Test Data " + (i + 1), true);
            // }

            string message = "Finished processing custom settings for dependents!:";
            foreach (CustomSettingsWrapper wrapper in _settings)
            {
                message += "\n" + wrapper.ToString();
            }
            Logger.LogInfo(message);
        }
        
        private void ProcessSettings(string filePath, string modID, bool testing)
        {
            Logger.LogInfo("Parsing settings for " + modID + ":");

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
                Logger.LogError("Couldn't find the custom settings file for " + modID + "!");
                Debug.LogException(e);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                Debug.LogException(e);
            }
            
            modSettings = ParseGeneralSettings(symbols, modSettings);
            if (modSettings != null)
            {
                _settings.Add(modSettings);
            }
            
            Logger.LogInfo("Finished parsing for " + modID + "!");
        }

        private CustomSettingsWrapper ParseGeneralSettings(List<string> symbols, CustomSettingsWrapper settings)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i].ToLower() == _CAMPAIGN_SETTINGS_DELIM)
                {
                    List<string> cSettings = new List<string>();
                    for (i++; i < symbols.Count; i++)
                    {
                        if (symbols[i].ToLower() == _CAMPAIGN_SETTINGS_DIVIDE
                            || symbols[i].ToLower() == _REGION_SETTINGS_DELIM)
                        {
                            settings = AddSetting(cSettings, settings, CustomSettingsObject.SettingsType.Campaign);
                            cSettings.Clear();
                            if (symbols[i].ToLower() == _REGION_SETTINGS_DELIM)
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
                if (symbols[i].ToLower() == _REGION_SETTINGS_DELIM)
                {
                    List<string> rSettings = new List<string>();

                    for (i++; i < symbols.Count; i++)
                    {
                        if (symbols[i].ToLower() == _REGION_SETTINGS_DIVIDE
                            || symbols[i].ToLower() == _CAMPAIGN_SETTINGS_DELIM)
                        {
                            settings = AddSetting(rSettings, settings, CustomSettingsObject.SettingsType.Region);
                            rSettings.Clear();
                            if (symbols[i].ToLower() == _CAMPAIGN_SETTINGS_DELIM)
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
                PrintNullReturnError("CustomSettingsWrapper", "ParseGeneralSettings()", "contains no setting objects");
                return null;
            }

            //add ids and mod id to _modContentToModIDMap for easier setting retrieval at runtime
            if (_modContentToModIDMap == null)
            {
                _modContentToModIDMap = new Dictionary<string, string>();
            }
            
            List<string> contentIDs = settings.GetAllSettingsIDs();
            foreach (string contentID in contentIDs)
            {
                Logger.LogInfo("Adding " + contentID + " | " + settings.ModID + " to content to ID map");
                _modContentToModIDMap.Add(contentID, settings.ModID);
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
                bool succeedAdd = wrap.AddNewSettings(set);
                if (!succeedAdd)
                {
                    Logger.LogWarning("Failed to add " + set.SettingType + " settings for " + set.ID + " to settings wrapper!");
                }
            }

            return wrap;
        }
        
        private CustomSettingsObject ParseSettings(List<string> symbols, CustomSettingsObject.SettingsType t)
        {
            //Logger.LogInfo("Recursive passes through ParseSettings(): " + _parseSettingsRecursed);
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
                                if (allOverrides[x].ToLower() == _REGION_SETTINGS_DIVIDE)
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
                    Logger.LogWarning("Unrecognized value for " + t + " setting!: " + symbols[i]);
                }
            }

            if (id == "")
            {
                PrintNullReturnError("CustomSettingsObject of type " + t, "ParseSettings()", "id");
                return null;
            }
            CustomSettingsObject result;
            if (pupSettings == null)
            {
                result = new CustomSettingsObject(t, id);
            }
            else
            {
                result = new CustomSettingsObject(t, id, pupSettings);
            }
            if (overridesList != null)
            {
                foreach (CustomSettingsObject o in overridesList)
                {
                    bool succeedAdd = result.AddOverride(o);
                    if (!succeedAdd)
                    {
                        Logger.LogWarning("You cannot add objects of type " + o.SettingType + " to overrides of objects with type " + result.SettingType);
                    }
                }
            }
            
            return result;
        }

        private PupSpawnSettings ParsePupSpawnSettings(List<string> pSettings)
        {
            if (pSettings.Count == 0)
            {
                PrintNullReturnError("PupSpawnSettings Object", "ParsePupSpawnSettings()", "empty values list");
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
                    if (s.StartsWith("pupsDynamicSpawn"))
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
                    else if (s.StartsWith("spawnChance"))
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
                    else if (s.StartsWith("min"))
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
                    else if (s.StartsWith("max"))
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
                        Logger.LogWarning("Unrecognized value in Pup Spawns settings!");
                    }
                }
                else
                {
                    PrintNullReturnError("Setting String", "PupSpawnSettings()", "value");
                }
            }

            PupSpawnSettings result = new PupSpawnSettings(spawns, min, max, chance);
            if (!result.SetMinMaxSucceeded)
            {
                Logger.LogWarning("Failed to set min and max property in PupSpawnSettings! " + min + " > " + max);
            }
            
            return new PupSpawnSettings(spawns, min, max, chance);
        }
        
        private List<string> ParseSymbols(string settings, char openBracket = '{', char closeBracket = '}')
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
        
        private object ParseValue(string setting)
        {
            string[] value = setting.Split(":".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (value.Length == 2)
            {
                float parsedNum;
                if (float.TryParse(value[1], out parsedNum))
                {
                    return parsedNum;
                }

                if (value[1].ToLower() == "false")
                {
                    return false;
                }
                if (value[1].ToLower() == "true")
                {
                    return true;
                }

                return value[1];
            }
            return null;
        }
        
        public void CreateOptionsMenuInstance()
        {
            _options = new DPSOptionsMenu(this);
        }
        
        private void LoadOptionsMenu(On.ModManager.orig_WrapModsInit orig)
        {
            try
            {
                MachineConnector.SetRegisteredOI(_MOD_ID, _options);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Logger.LogError(e.Message);
            }
        }

        private void PrintNullReturnError(string value, string source, string cause)
        {
            Logger.LogError("A " + value + " returned null in " + source + "! Cause: " + cause);
        }

        private void PrintInvalidCastError(string failedObj, string triedType, string message)
        {
            Logger.LogError("An invalid cast was encountered trying to convert " 
                            + failedObj + " from object to " + triedType + "!:\n" + message);
        }
        
        private string[] SettingTestData()
        {
            return new[]
            {
                /*DATA SET 1 [X]
                empty file
                expected behavior: triggers PrintNullReturnError() in ProcessSettings()
                 due to missing campaign or region objects*/
                "",
                
                /*DATA SET 2 [X]
                empty campaign data
                expected behavior: triggers PrintNullReturnError() in ParseSettings()
                 due to missing a campaign ID*/
                "CAMPAIGNS;\n",
                
                /*DATA SET 3 [X]
                campaign data with no pup settings
                expected behavior: results in a CustomSettingsObject of type Campaign
                 with dynamic pup spawning defaulted to false*/
                "CAMPAIGNS;\n" +
                "id: Campaign with no pup settings;\n",
                
                /*DATA SET 4 [X]
                campaign where pups spawn
                expected behavior: results in a CustomSettingsWrapper object
                 with one CustomSettingsObject of type Campaign*/
                "CAMPAIGNS;\n" +
                "id: 1st Campaign with pup settings (correct);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 2;\n" +
                "\tmax: 5;\n" +
                "\tspawnChance: 1.0;\n" +
                "};\n",
                
                /*DATA SET 5 [X]
                campaign where pups don't spawn (explicit)
                expected behavior: results in a CustomSettings object of type Campaign
                 with pup spawns defaulted to false*/
                "CAMPAIGNS;\n" +
                "id: 2nd Campaign with pup settings (explicit);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n",
                
                /*DATA SET 6 [X]
                empty region data
                expected behavior: triggers PrintNullReturnError() in ParseSettings()
                 due to missing id*/
                "REGIONS;\n",

                /*DATA SET 7 [X]
                region with no pup settings
                expected behavior: results in CustomSettingsObject of type Region
                 with pup spawns defaulted to false*/
                "REGIONS;\n" +
                "id: Region with no pup settings;\n",
                
                /*DATA SET 8 [X]
                region with empty pup settings
                expected behavior: triggers PrintNullReturnError() in ParsePupSpawnSettings()
                 due to empty settings list for pup settings*/
                "REGIONS;\n" +
                "id: Region with empty pup settings;\n" +
                "pup_settings: {};\n",
                
                /*DATA SET 9 [X]
                region where pups spawn (correct)
                expected behavior: results in a CustomSettingsWrapper object
                 which contains one CustomSettingsObject of type Region in _regionSettings*/
                "REGIONS;\n" +
                "id: 1st Region with pup settings (correct);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 1.0;\n" +
                "};\n",
                
                /*DATA SET 10 [X]
                region where pups don't spawn (explicit)
                expected behavior: results in CustomSettingsObject of type Region
                 with pup spawns defaulted to false*/
                "REGIONS;\n" +
                "id: 2nd Region with pup settings (explicit);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n",
                
                /*DATA SET 11 [X]
                campaign with empty id value & explicit false pup settings
                expected behavior: triggers PrintNullReturnError() in ParseValue()
                 due to empty id string*/
                "CAMPAIGNS;\n" +
                "id:  ;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n",
                
                /*DATA SET 12 [X]
                region with empty id value & no pup settings
                expected behavior: triggers PrintNullReturnError() in ParseSettings()
                 due to missing ID*/
                "REGIONS;\n" +
                "id:  ;\n",
                
                /*DATA SET 13 [X]
                campaign with custom region overrides
                expected behavior: will result in a CustomSettingsObject of type Campaign
                 which has an _overrides list size of 1 CustomSettingsObject of type Region*/
                "CAMPAIGNS;\n" +
                "id: RegionOverrideWithoutSpawns;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 2;\n" +
                "\tmax: 20;\n" +
                "\tspawnChance: 0.2;\n" +
                "};\n" +
                "overrides: {\n" +
                "\tid: TR;\n" +
                "};\n",
                
                /*DATA SET 14 [X]
                campaign with custom region overrides
                expected behavior: will result in a CustomSettingsObject which
                 has an _overrides list size of 1 CustomSettingsObject of type Region*/
                "CAMPAIGNS;\n" +
                "id: RegionOverrideWithSpawns;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n" +
                "overrides: {\n" +
                "\tid: TR;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 1;\n" +
                "\t\tmax: 3;\n" +
                "\t\tspawnChance: 0.5;\n" +
                "\t];\n" +
                "};\n",
                
                /*DATA SET 15 [X]
                campaign with MULTIPLE custom region overrides
                expected behavior: results in a CustomSettings object with pup spawns set to false by default, 
                and an _overrides list of size 2 CustomSettingsObjects of type Region*/
                "CAMPAIGNS;\n" +
                "id: CampaignWithMultipleRegionOverrides;\n" +
                "overrides: {\n" +
                "\tid: AB;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 10;\n" +
                "\t\tmax: 50;\n" +
                "\t\tspawnChance: 0.01;\n" +
                "\t];\n" +
                "\tREGION;\n" +
                "\tid: BC;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 2;\n" +
                "\t\tmax: 7;\n" +
                "\t\tspawnChance: 0.1;\n" +
                "\t];\n" +
                "};\n",
                
                /*DATA SET 16 [X]
                multiple regions under one mod
                expected behavior: results in a CustomSettingsWrapper object with a _regionSettings
                list size of 3 CustomSettingsObjects of type Region*/
                "REGIONS;\n" +
                "id: SomeSpawnsRegion;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 3;\n" +
                "\tspawnChance: 0.4;\n" +
                "};\n" +
                "REGION;\n" +
                "id: NoSpawnsRegion;\n" +
                "REGION;\n" +
                "id: ManySpawnsRegion;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 100;\n" +
                "\tmax: 200;\n" +
                "\tspawnChance: 1;\n" +
                "};\n",
                
                /*DATA SET 17 [X]
                multiple campaign under one mod
                expected behavior: results in 3 CustomSettingsObjects of type Campaign*/
                "CAMPAIGNS;\n" +
                "id: Campaign1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 10;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 0.1;\n" +
                "};\n" +
                "CAMPAIGN;\n" +
                "id: Campaign2;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 2;\n" +
                "\tspawnChance: 0.75;\n" +
                "};\n" +
                "CAMPAIGN;\n" +
                "id: Campaign3;",
                
                /*DATA SET 18 [X]
                multiple campaigns and regions under one mod
                expected behavior: results in 2 CustomSettingsObjects of type Campaign
                 and 2 CustomSettingsObjects of type Region*/
                "CAMPAIGNS;\n" +
                "id: Campaign1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 10;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 0.1;\n" +
                "};\n" +
                "CAMPAIGN;\n" +
                "id: Campaign2;" +
                "REGIONS;\n" +
                "id: Region1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 3;\n" +
                "\tspawnChance: 0.4;\n" +
                "};\n" +
                "REGION;\n" +
                "id: Region2;\n",
                
                /*DATA SET 19 [X]
                multiple campaigns and regions under one mod (reversed)
                expected behavior: results in 2 CustomSettingsObjects of type Campaign
                 and 2 CustomSettingsObjects of type Region*/
                "REGIONS;\n" +
                "id: Region1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 3;\n" +
                "\tspawnChance: 0.4;\n" +
                "};\n" +
                "REGION;\n" +
                "id: Region2;\n" +
                "CAMPAIGNS;\n" +
                "id: Campaign1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 10;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 0.1;\n" +
                "};\n" +
                "CAMPAIGN;\n" +
                "id: Campaign2;",
                
                /*DATA SET 20 [X]
                multiple campaigns with multiple region overrides and multiple regions under one mod
                expected behavior: results in 2 CustomSettingsObjects of type Campaign, each of which
                 has 2 region overrides, and 2 CustomSettingsObjects of type Region*/
                "CAMPAIGNS;\n" +
                "id: Campaign1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 10;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 0.1;\n" +
                "};\n" +
                "overrides: {\n" +
                "\tid: AB;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: false;\n" +
                "\t];\n" +
                "\tREGION;\n" +
                "\tid: BC;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 2;\n" +
                "\t\tmax: 7;\n" +
                "\t\tspawnChance: 0.1;\n" +
                "\t];\n" +
                "};\n" +
                "CAMPAIGN;\n" +
                "id: Campaign2;" +
                "overrides: {\n" +
                "\tid: AB;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 10;\n" +
                "\t\tmax: 50;\n" +
                "\t\tspawnChance: 0.01;\n" +
                "\t];\n" +
                "\tREGION;\n" +
                "\tid: BC;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 2;\n" +
                "\t\tmax: 7;\n" +
                "\t\tspawnChance: 0.1;\n" +
                "\t];\n" +
                "};\n" +
                "REGIONS;\n" +
                "id: Region1;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 3;\n" +
                "\tspawnChance: 0.4;\n" +
                "};\n" +
                "REGION;\n" +
                "id: Region2;\n"
            };
        }
    }
}
