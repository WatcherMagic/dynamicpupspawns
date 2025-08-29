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

        private World _world;
        private Dictionary<string, string> _persistentPups;

        private const string _SAVE_DATA_DELIMITER = "DynamicPupSpawnsData";
        private const string _REGX_STR_SPLIT = "<WM,DPS>";
        private const string DATA_DIVIDER = ":";
        private const string CAMPAIGN_SETTINGS_DELIM = "campaigns";
        private const string CAMPAIGN_SETTINGS_STOP = "end_campaigns";
        private const string REGION_SETTINGS_DELIM = "regions";
        private const string REGION_SETTINGS_STOP = "end_regions";
        private const string REGION_SETTINGS_DIVIDE = "region";

        private List<CustomSettingsWrapper> _settings;
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;

            On.SaveState.SaveToString += SaveDataToString;
            On.SaveState.LoadGame += GetSaveDataFromString;

            On.Creature.Die += LogPupDeath;

            On.ModManager.WrapPostModsInit += ProcessCustomData;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            _world = self;

            int minPupsInRegion = 1;
            int maxPupsInRegion = 5;
            float spawnChance = 0.3f;

            //get rooms with unsubmerged den nodes
            Dictionary<AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);

            //determine room spawn weight based on number of dens in room
            Dictionary<AbstractRoom, float> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);

            //get dict of rooms and weights in parallel arrays
            Dictionary<AbstractRoom[], float[]> parallelArrays = CreateParallelRoomWeightArrays(roomWeights);
            float[] weightsScale = AssignSortedRoomScaleValues(parallelArrays.ElementAt(0).Value);

            
            //generate number of pups for this cycle
            // + 1 to max to account for rounding down w/ cast to int
            int pupNum = RandomPupGaussian(minPupsInRegion, maxPupsInRegion + 1);
            Debug.Log("DynamicPupSpawns: " + pupNum + " pups this cycle");

            //respawn pups from save data
            pupNum = SpawnPersistentPups(self, pupNum);

            if (pupNum > 0)
            {
                //get random room for each pup
                for (int i = 0; i < pupNum; i++)
                {
                    AbstractRoom spawnRoom = PickRandomRoomByWeight(parallelArrays.ElementAt(0).Key, weightsScale);
                    if (self.game.IsStorySession)
                    {
                        PutPupInRoom(self.game, self, spawnRoom, null, self.game.GetStorySession.characterStats.name);
                    }
                }
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
                foreach (KeyValuePair<string, string> pup in _persistentPups)
                {
                    string[] region = pup.Value.Split('_');
                    if (region.Length >= 1)
                    {
                        if (world.region.name.ToLower() == region[0].ToLower())
                        {
                            AbstractRoom room = world.GetAbstractRoom(pup.Value);
                            if (room != null)
                            {
                                PutPupInRoom(world.game, world, room, pup.Key,
                                    world.game.GetStorySession.characterStats.name);
                                pupNum--; //persistent pups count towards the total pups spawned per cycle
                            }
                            else
                            {
                                Logger.LogInfo("Room " + room.name + " pulled from save data not recognized!");
                            }
                        }
                    }
                    else
                    {
                        Logger.LogError("Region acronym could not be pulled from room name " + pup.Value + "!");
                    }
                }
            }
            else
            {
                Logger.LogInfo("Saved pup data not found in Dictionary at time of pup placement!");
            }

            return pupNum;
        }

        public void PutPupInRoom(RainWorldGame game, World world, AbstractRoom room, string pupID,
            SlugcatStats.Name curSlug)
        {
            bool temp = false;
            if (ModManager.MSC && game.IsStorySession)
            {
                if (ModManager.Watcher && curSlug == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                {
                    temp = true;
                }

                if (!temp)
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

                    try
                    {
                        room.AddEntity(abstractPup);
                        if (room.realizedRoom != null)
                        {
                            abstractPup.RealizeInRoom();
                        }
                        try
                        {
                            (abstractPup.state as PlayerNPCState).foodInStomach = 1;
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e.Message);
                        }

                        Logger.LogInfo(abstractPup.creatureTemplate.type + " " + abstractPup.ID + " spawned in " +
                                       abstractPup.Room.name + (persistent ? " PERSISTENT" : ""));
                        Debug.Log("DynamicPupSpawns: " + abstractPup.creatureTemplate.type + " " + abstractPup.ID +
                                  " spawned in " + abstractPup.Room.name + (persistent ? " PERSISTENT" : ""));
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.Message);
                    }
                }
            }
        }

        private string SaveDataToString(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            string s = orig(self);

            string data = _SAVE_DATA_DELIMITER;

            string message = "Adding pups to save data...\n";
            if (_world != null)
            {
                for (int i = 0; i < _world.abstractRooms.Length; i++)
                {
                    foreach (AbstractCreature abstractCreature in _world.abstractRooms[i].creatures)
                    {
                        //check for player's shelter
                        if (_world.abstractRooms[i].shelter
                            && abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat
                            && abstractCreature.ID.number < 1000)
                        {
                            message += "Found shelter " + _world.abstractRooms[i].name + " for Slugcat " + abstractCreature.ID.number + "; skipping\n";
                            continue;
                        }
                        
                        //this autofilled: investigate usefulness
                        //if (abstractCreature.state is MoreSlugcats.PlayerNPCState)
                        
                        /*ISSUE: abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat
                         only detects players, not SlugNPCs. Additionally, Bups and likely others
                         are apparently different templates from SlugNPC. Hardcoded workaround for now.*/
                        //foreach (string pupType in recognizedPupTypes)
                        //{
                            if (abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
                                && !abstractCreature.state.dead)
                            {
                                data += abstractCreature.ID + DATA_DIVIDER + _world.abstractRooms[i].name +
                                        _REGX_STR_SPLIT;
                                //if tamed, save tamed status here
                            }
                        //}
                    }
                }

                //remove trailing split sequence to prevent unrecognized data pair error at end in ExtractSaveValues()
                data = data.Remove(data.Length - _REGX_STR_SPLIT.Length, _REGX_STR_SPLIT.Length);
                message += "Final save string: " + data;
                Logger.LogInfo(message);
            }
            else
            {
                Logger.LogError("_world was null, cannot save abstract pups!");
            }

            s = String.Concat(s, data, "<svA>");

            return s;
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
            string[] dataValues = Regex.Split(modString, _REGX_STR_SPLIT);
            string message = "";

            // foreach (string s in dataValues)
            // {
            //     Logger.LogInfo("Data values: " + s);
            // }

            if (_persistentPups == null)
            {
                _persistentPups = new Dictionary<string, string>();
            }
            else
            {
                message += "_persistentPups was not null on value extraction from save string.\n";
                foreach (KeyValuePair<string, string> pair in _persistentPups)
                {
                    message += pair.Key + " : " + pair.Value + "\n";
                }

                message += "Clearing _persistentPups!";
                Logger.LogInfo(message);

                _persistentPups.Clear();
            }

            string[] pairContainer;
            message = "Adding pups from save data to _persistentPups...\n";
            for (int i = 0; i < dataValues.Length; i++)
            {
                pairContainer = Regex.Split(dataValues[i], ":");
                if (pairContainer.Length >= 2)
                {
                    try
                    {
                        _persistentPups.Add(pairContainer[0], pairContainer[1]);
                        message += "Added " + pairContainer[0] + " : " + pairContainer[1] + "\n";
                    }
                    catch (Exception e)
                    {
                        message += "ERROR: " + e.Message + "\n";
                        Debug.LogError(e.Message);
                    }
                }
                else
                {
                    message += "Returned invalid data pair while extracting from save string!\n";
                }
            }

            Logger.LogInfo(message);
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

        private void ProcessCustomData(On.ModManager.orig_WrapPostModsInit orig)
        {
            orig();
            
            if (_settings == null)
            {
                Logger.LogInfo("Creating new global settings list");
                _settings = new List<CustomSettingsWrapper>();
            }

            //temporarily removed while settings functionality is tested
            // foreach (ModManager.Mod mod in ModManager.ActiveMods)
            // {
            //     bool depends = false;
            //     string filePath = mod.path + "\\dynamicpups\\settings.txt";
            //     for (int i = 0; i < mod.requirements.Length; i++)
            //     {
            //         if (mod.requirements[i] == _MOD_ID)
            //         {
            //             depends = true;
            //             Logger.LogInfo("Found dependent!: " + mod.name);
            //             ProcessSettings(filePath, mod.id);
            //             break;
            //         }
            //     }
            //
            //     if (!depends)
            //     {
            //         for (int i = 0; i < mod.priorities.Length; i++)
            //         {
            //             if (mod.priorities[i] == _MOD_ID)
            //             {
            //                 Logger.LogInfo("Found priority!: " + mod.name);
            //                 ProcessSettings(filePath, mod.id);
            //                 break;
            //             }
            //         }
            //     }
            // }

            string[] testSettingsArray = SettingTestData();
            for (int i = 0; i < testSettingsArray.Length; i++)
            {
                ProcessSettings(testSettingsArray[i], "Test Data " + (i + 1), true);
            }

            string message = "Finished processing custom settings for dependents!:";
            foreach (CustomSettingsWrapper wrapper in _settings)
            {
                message += "\n" + wrapper.ToString();
            }
            Logger.LogInfo(message);
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
                expected behavior: triggers PrintNullReturnError() in ParseGeneralSettings()
                 due to missing a campaign ID*/
                "CAMPAIGNS;\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 3 [X]
                campaign data with no pup settings
                expected behavior: results in a CustomCampaignSettings object with
                 dynamic pup spawning defaulted to false*/
                "CAMPAIGNS;\n" +
                "id: Campaign with no pup settings;\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 4 [X]
                campaign data with empty pup settings
                expected behavior: triggers PrintNullReturnError() in ParseCampaignSettings()
                 due to ParsePupSpawnSettings() returning null because the values list is empty*/
                "CAMPAIGNS;\n" +
                "id: Campaign with empty pup settings;\n" +
                "pup_settings: {};\n" +
                "END_CAMPAIGNS;",
                
                //succeeds creating pup settings but data string returns null in PupSpawnSettings()
                /*DATA SET 5 [X]
                campaign where pups spawn
                expected behavior: results in a CustomSettingsWrapper object
                 with one CustomCampaignSettings object inside, which holds one
                 PupSpawnSettings object that allows pups to spawn with a 100% spawn rate of 2-5*/
                "CAMPAIGNS;\n" +
                "id: 1st Campaign with pup settings (correct);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 2;\n" +
                "\tmax: 5;\n" +
                "\tspawnChance: 1.0;\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                //succeeds creating pup settings but data string returns null in PupSpawnSettings()
                /*DATA SET 6 [X]
                campaign where pups don't spawn (explicit)
                expected behavior: results in a CustomCampaignSettings object with pup spawns defaulted to false*/
                "CAMPAIGNS;\n" +
                "id: 2nd Campaign with pup settings (explicit);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 7 [X]
                empty region data
                expected behavior: triggers PrintNullReturnError() in ParseGeneralSettings()
                 due to missing a region acronym and pup settings object*/
                "REGIONS;" +
                "END_REGIONS;",

                /*DATA SET 8 [X]
                //region with no pup settings
                expected behavior: results in CustomRegionSettings object with pup spawns defaulted to false*/
                "REGIONS;\n" +
                "name: Region with no pup settings;\n" +
                "END_REGIONS;",
                
                /*DATA SET 9 [X]
                region with empty pup settings
                expected behavior: triggers PrintNullReturnError() in ParseRegionSettings()
                 due to ParsePupSpawnSettings() returning null because the values list is empty*/
                "REGIONS;\n" +
                "name: Region with empty pup settings;\n" +
                "pup_settings: {};\n" +
                "END_REGIONS;",
                
                /*DATA SET 10 [X]
                region where pups spawn (correct)
                expected behavior: results in a CustomSettingsWrapper object
                 which contains one CustomRegionSettings object that allows 1-10 pups
                 to spawn at a 100% rate*/
                "REGIONS;\n" +
                "name: 1st Region with pup settings (correct);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 10;\n" +
                "\tspawnChance: 1.0;\n" +
                "};\n" +
                "END_REGIONS;",
                
                /*DATA SET 11 [X]
                region where pups don't spawn (explicit)
                expected behavior: results in CustomRegionSettings object with pup spawns defaulted to false*/
                "REGIONS;\n" +
                "name: 2nd Region with pup settings (explicit);\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n" +
                "END_REGIONS;",
                
                /*DATA SET 12 [X]
                campaign with empty id value & explicit false pup settings
                expected behavior: triggers PrintNullReturnError() in ParseCampaignSettings()
                 due to the id field being empty*/
                "CAMPAIGNS;\n" +
                "id:  ;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 13 [X]
                region with empty name value & no pup settings
                expected behavior: triggers PrintNullReturnError() in ParseRegionSettings()
                 due to the name field being empty*/
                "REGIONS;\n" +
                "name:  ;\n" +
                "END_REGIONS;",
                
                /*DATA SET 14 [X]
                campaign with empty id value and empty pup settings
                expected behavior: triggers PrintNullReturnError() in ParseCampaignSettings()
                 due to missing ID*/
                "CAMPAIGNS;\n" +
                "id:;\n" +
                "pup_settings: {};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 15 [X]
                campaign with custom region overrides
                expected behavior: will result in a CustomCampaignSettings object which
                 has a _campaignRegionSettings list size of 1 CustomRegionSettings object*/
                "CAMPAIGNS;\n" +
                "id: RegionOverrideWithoutSpawns;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 2;\n" +
                "\tmax: 20;\n" +
                "\tspawnChance: 0.2;\n" +
                "};\n" +
                "region_overrides: {\n" +
                "\tname: TR;\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 16 [X]
                campaign with custom region overrides
                expected behavior: will result in a CustomCampaignSettings object which
                 has a _campaignRegionSettings list size of 1 CustomRegionSettings object*/
                "CAMPAIGNS;\n" +
                "id: RegionOverrideWithSpawns;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: false;\n" +
                "};\n" +
                "region_overrides: {\n" +
                "\tname: TR;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 1;\n" +
                "\t\tmax: 3;\n" +
                "\t\tspawnChance: 0.5;\n" +
                "\t];\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 17 [X]
                campaign with MULTIPLE custom region overrides
                expected behavior: results in a CustomCampaignSettings object with pup spawns set to false by default, 
                and a _campaignRegionSettings list of size 2 CustomRegionSettings objects*/
                "CAMPAIGNS;\n" +
                "id: CampaignWithMultipleRegionOverrides;\n" +
                "region_overrides: {\n" +
                "\tname: AB;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 10;\n" +
                "\t\tmax: 50;\n" +
                "\t\tspawnChance: 0.01;\n" +
                "\t];\n" +
                "\tREGION;\n" +
                "\tname: BC;\n" +
                "\tpup_settings: [\n" +
                "\t\tpupsDynamicSpawn: true;\n" +
                "\t\tmin: 2;\n" +
                "\t\tmax: 7;\n" +
                "\t\tspawnChance: 0.1;\n" +
                "\t];\n" +
                "};\n" +
                "END_CAMPAIGNS;",
                
                /*DATA SET 18 [X]
                multiple regions under one mod
                expected behavior: results in a CustomSettingsWrapper object with a _regionSettings
                list size of 3 CustomRegionSettings objects*/
                "REGIONS;\n" +
                "name: SomeSpawnsRegion;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 1;\n" +
                "\tmax: 3;\n" +
                "\tspawnChance: 0.4;\n" +
                "};\n" +
                "REGION;\n" +
                "name: NoSpawnsRegion;\n" +
                "REGION;\n" +
                "name: ManySpawnsRegion;\n" +
                "pup_settings: {\n" +
                "\tpupsDynamicSpawn: true;\n" +
                "\tmin: 100;\n" +
                "\tmax: 200;\n" +
                "\tspawnChance: 1;\n" +
                "};\n" +
                "END_REGIONS;"
            };
        }

        private void ProcessSettings(string filePath, string modID, bool testing)
        {
            Logger.LogInfo("Parsing settings for " + modID + ":");

            CustomSettingsWrapper modSettings = new CustomSettingsWrapper(modID);
            LinkedList<string> symbols = new LinkedList<string>();
            
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
            if (modSettings == null)
            {
                PrintNullReturnError("Custom Settings Wrapper", "ProcessSettings()");
            }
            else
            {
                _settings.Add(modSettings);
            }
            
            Logger.LogInfo("Finished parsing for " + modID + "!");
        }

        private LinkedList<string> ParseSymbols(string settings, char openBracket = '{', char closeBracket = '}')
        {
            StringReader reader = new StringReader(settings);
            LinkedList<string> symbols = new LinkedList<string>();
            
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
                    symbols.AddLast(symbol);
                    symbol = "";
                }
            }

            return symbols;
        }
        
        private CustomSettingsWrapper ParseGeneralSettings(LinkedList<string> symbols, CustomSettingsWrapper settings)
        {
            LinkedListNode<string> node = symbols.First;

            int infiniteCatch = 0;
            while (node != null)
            {
                infiniteCatch++;
                if (infiniteCatch > 200)
                {
                    Logger.LogWarning("ParseGeneralSettings() entered an infinite loop, " +
                                      "check that your settings.txt is free of typos " +
                                      "and that there's a semicolon (;) at the end of every line!");
                }
                
                if (node.Value.ToLower() == CAMPAIGN_SETTINGS_DELIM)
                {
                    node = node.Next;
                    LinkedList<string> cSettings = new LinkedList<string>();
                    while (node != null && node.Value.ToLower() != CAMPAIGN_SETTINGS_STOP)
                    {
                        cSettings.AddLast(node.Value);
                        node = node.Next;
                    }

                    CustomCampaignSettings set = ParseCampaignSettings(cSettings);
                    if (set != null)
                    {
                        Logger.LogInfo("Succeeded for campaign " + set.CampaignID + "!");
                        settings.AddCampaignSettings(set);
                    }
                    else
                    {
                        PrintNullReturnError("Campaign Settings Object", "ParseGeneralSettings()");
                    }
                }
                else if (node.Value.ToLower() == REGION_SETTINGS_DELIM)
                {
                    node = node.Next;
                    LinkedList<string> rSettings = new LinkedList<string>();
                    CustomRegionSettings set;
                    while (node != null && node.Value.ToLower() != REGION_SETTINGS_STOP)
                    {
                        if (node.Value.ToLower() == REGION_SETTINGS_DIVIDE
                            || node.Next != null && node.Next.Value.ToLower() == REGION_SETTINGS_STOP)
                        {
                            if (node.Next.Value.ToLower() == REGION_SETTINGS_STOP)
                            {
                                rSettings.AddLast(node.Value);
                            }
                            
                            set = ParseRegionSettings(rSettings);
                            if (set != null)
                            {
                                Logger.LogInfo("Succeeded for region " + set.RegionAcronym + "!");
                                settings.AddRegionSettings(set);
                            }
                            else
                            {
                                PrintNullReturnError("Region Settings Object", "ParseGeneralSettings()");
                            }
                            rSettings.Clear();
                            node = node.Next;
                            continue;
                        }
                        
                        rSettings.AddLast(node.Value);
                        node = node.Next;
                    }
                }
                
                if (node == null)
                {
                    Logger.LogWarning("ParseGeneralSettings() reached unexpected end of settings.txts!");
                    break;
                }
                node = node.Next;
            }

            if (!settings.HasCampaignSettings() && !settings.HasRegionSettings())
            {
                return null;
            }
            return settings;
        }
        
        private CustomCampaignSettings ParseCampaignSettings(LinkedList<string> symbols)
        {
            Logger.LogInfo("Parsing campaign settings");
            LinkedListNode<string> node = symbols.First;

            string id = null;
            PupSpawnSettings pupSettings = new PupSpawnSettings();
            List<CustomRegionSettings> rOverrides = new List<CustomRegionSettings>();

            while (node != null)
            {   
                if (node.Value.ToLower().StartsWith("id"))
                {
                    object o = ParseValue(node.Value);
                    if (o != null)
                    {
                        try
                        {
                            id = (string)o;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            id = "Something went wrong casting from object to string!";

                            if (e.GetType() == typeof(InvalidCastException))
                            {
                                PrintInvalidCastError("Campaign ID", "string", e.Message);
                            }
                        }
                    }
                    else
                    {
                        PrintNullReturnError("Campaign ID", "ParseCampaignSettings()");
                    }
                }
                else if (node.Value.ToLower().StartsWith("region_overrides"))
                {
                    object o = ParseValue(node.Value);
                    if (o != null)
                    {
                        string rSettings = "";
                        try
                        {
                            rSettings = Convert.ToString(o);
                            rSettings = rSettings.Substring(1, rSettings.Length - 2);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            if (e.GetType() == typeof(InvalidCastException))
                            {
                                PrintInvalidCastError("Campaign Region Overrides", "string", e.Message);
                            }
                        }
                        
                        LinkedList<string> rSymbols = ParseSymbols(rSettings, '[', ']');
                        LinkedList<string> singleRegion = new LinkedList<string>();

                        LinkedListNode<string> rNode = rSymbols.First;
                        CustomRegionSettings rSet;
                        while (rNode != null)
                        {
                            if (rNode.Value.ToLower() == REGION_SETTINGS_DIVIDE
                                || rNode.Next == null)
                            {
                                if (rNode.Next == null)
                                {
                                    Logger.LogInfo("Node: " + rNode.Value);
                                    singleRegion.AddLast(rNode.Value);
                                }
                                
                                rSet = ParseRegionSettings(singleRegion);
                                if (rSet != null)
                                {
                                    rOverrides.Add(rSet);
                                }
                                else
                                {
                                    PrintNullReturnError("Region Settings Object", "ParseCampaignSettings()");
                                }
                                singleRegion.Clear();
                                rNode = rNode.Next;
                                continue;
                            }
                            
                            Logger.LogInfo("Node: " + rNode.Value);
                            singleRegion.AddLast(rNode.Value);
                            
                            rNode = rNode.Next;
                        }
                    }
                    else
                    {
                        PrintNullReturnError("Region Overrides String", "ParseCampaignSettings()");
                    }
                }
                else if (node.Value.ToLower().StartsWith("pup_settings"))
                {
                    object o = ParseValue(node.Value);
                    if (o != null)
                    {
                        string pSettings = "";
                        try
                        {
                            pSettings = Convert.ToString(o);
                            pSettings = pSettings.Substring(1, pSettings.Length - 2);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            if (e.GetType() == typeof(InvalidCastException))
                            {
                                PrintInvalidCastError("Campaign Pup Settings", "string", e.Message);
                            }
                        }
                        
                        LinkedList<string> pSymbols = ParseSymbols(pSettings);
                        
                        //eventually all linked lists will be replaced with Lists, temporary measure in meantime
                        LinkedListNode<string> pNode = pSymbols.First;
                        List<string> pSym = new List<string>();
                        while (pNode != null)
                        {
                            pSym.Add(pNode.Value);
                            pNode = pNode.Next;
                        }
                        
                        pupSettings = ParsePupSpawnSettings(pSym);
                        if (pupSettings == null)
                        {
                            PrintNullReturnError("Pup Settings Object", "ParseCampaignSettings()");
                        }
                    }
                    else
                    {
                        PrintNullReturnError("Pup Settings String", "ParseCampaignSettings()");
                    }
                }
                else
                {
                    Logger.LogWarning("Unrecognized value in ParseCampaignSettings()!");
                }
                node = node.Next;
            }
            
            if (id == null || pupSettings == null)
            {
                return null;
            }

            CustomCampaignSettings result = new CustomCampaignSettings(id, pupSettings);
            if (rOverrides.Count > 0)
            {
                foreach (CustomRegionSettings r in rOverrides)
                {
                    result.AddCampaignRegionSettings(r);
                }
            }
            
            return result;
        }

        private CustomRegionSettings ParseRegionSettings(LinkedList<string> symbols)
        {
            Logger.LogInfo("Parsing Region settings");
            LinkedListNode<string> node = symbols.First;

            string name = null;
            PupSpawnSettings pupSettings = new PupSpawnSettings();

            while (node != null)
            {
                Logger.LogInfo("Region settings symbol: " + node.Value);
                
                if (node.Value.ToLower().StartsWith("name"))
                {
                    object o = ParseValue(node.Value);
                    if (o != null)
                    {
                        try
                        {
                            name = Convert.ToString(o);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            name = "Something went wrong casting from object to string!";

                            if (e.GetType() == typeof(InvalidCastException))
                            {
                                PrintInvalidCastError("Region Acronym", "string", e.Message);
                            }
                        }
                    }
                    else
                    {
                        PrintNullReturnError("Region Name", "ParseRegionSettings()");
                    }
                }
                else if (node.Value.ToLower().StartsWith("pup_settings"))
                {
                    object o = ParseValue(node.Value);
                    if (o != null)
                    {
                        string pSettings = "";
                        try
                        {
                            pSettings = Convert.ToString(o);
                            pSettings = pSettings.Substring(1, pSettings.Length - 2);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            if (e.GetType() == typeof(InvalidCastException))
                            {
                                PrintInvalidCastError("Campaign Pup Settings", "string", e.Message);
                            }
                        }

                        LinkedList<string> pSymbols = ParseSymbols(pSettings);

                        //eventually all linked lists will be replaced with Lists, temporary measure in meantime
                        LinkedListNode<string> pNode = pSymbols.First;
                        List<string> pSym = new List<string>();
                        while (pNode != null)
                        {
                            pSym.Add(pNode.Value);
                            pNode = pNode.Next;
                        }
                        
                        pupSettings = ParsePupSpawnSettings(pSym);
                        if (pupSettings == null)
                        {
                            PrintNullReturnError("Pup Settings Object", "ParseRegionSettings()");
                        }
                    }
                    else
                    {
                        PrintNullReturnError("Pup Settings String", "ParseCampaignSettings()");
                    }
                }
                else
                {
                    Logger.LogWarning("Unrecognized value in ParseRegionSettings()!");
                }
                node = node.Next;
            }
            
            if (name == null || pupSettings == null)
            {
                return null;
            }
            return new CustomRegionSettings(name, pupSettings);
        }

        private PupSpawnSettings ParsePupSpawnSettings(List<string> pSettings)
        {
            if (pSettings.Count == 0)
            {
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
                    PrintNullReturnError("Setting String", "PupSpawnSettings()");
                }
            }

            PupSpawnSettings result = new PupSpawnSettings(spawns, min, max, chance);
            if (!result.SetMinMaxSucceeded)
            {
                Logger.LogWarning("Failed to set min and max property in PupSpawnSettings! " + min + " > " + max);
            }
            
            return new PupSpawnSettings(spawns, min, max, chance);
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

        private void PrintNullReturnError(string value, string source)
        {
            Logger.LogError("A " + value + " returned null in " + source + "!");
        }

        private void PrintInvalidCastError(string failedObj, string triedType, string message)
        {
            Logger.LogError("An invalid cast was encountered trying to convert " 
                            + failedObj + " from object to " + triedType + "!:\n" + message);
        }
    }
}
