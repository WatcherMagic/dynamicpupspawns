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
        private const string _PUP_DATA_DIVIDER = ":";

        private List<CustomSettingsWrapper> _settings;
        private CustomCampaignSettings _survivorSettings;

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

        public int RandomPupGaussian(float min, float max)
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
                                //Logger.LogInfo("Found saved room! " + room.name);

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
                        StaticWorld.GetCreatureTemplate(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC),
                        null,
                        new WorldCoordinate(room.index, -1, -1, 0),
                        id);

                    try
                    {
                        room.AddEntity(abstractPup);
                        try
                        {
                            (abstractPup.state as MoreSlugcats.PlayerNPCState).foodInStomach = 1;
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

            string[] recognizedPupTypes = { "SlugNPC" /*, "Bup"*/ };

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
                                data += abstractCreature.ID + _PUP_DATA_DIVIDER + _world.abstractRooms[i].name +
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
                Logger.LogInfo("Creating new global settings list\n");
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
                        ProcessSettings(filePath, mod.id);
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
                            ProcessSettings(filePath, mod.id);
                            break;
                        }
                    }
                }
            }

            string message = "Finished processing custom settings for dependents!:\n";
            foreach (CustomSettingsWrapper wrapper in _settings)
            {
                message += wrapper.ToString();
            }
            Logger.LogInfo(message);
        }

        private void ProcessSettings(string filePath, string modID)
        {
            Logger.LogInfo("Parsing settings for " + modID + ":");

            CustomSettingsWrapper modSettings = new CustomSettingsWrapper(modID);

            try
            {   
                string settings = File.ReadAllText("DummySettings.txt");
                settings = Regex.Replace(settings, @"\s+", "");
                Logger.LogInfo("Settings: " + settings);
                StringReader reader = new StringReader(settings);
                
                List<string> symbols = new List<string>();

                string symbol = "";
                while (reader.Peek() >= 0)
                {
                    char c = (char)reader.Peek();
                    if (c != ';')
                    {
                        if (c == '{')
                        {
                            while (reader.Peek() >= 0)
                            {
                                c = (char)reader.Peek();
                                if (c != '}')
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

                string message = "Extracted symbols:\n";
                foreach (string s in symbols)
                {
                    message += s + "\n";
                }
                Logger.LogInfo(message);
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

            Logger.LogInfo("Finished parsing for " + modID + "!");
            _settings.Add(modSettings);
        }
        
        private Tuple<CustomCampaignSettings, StreamReader> ParseCampaignSettings(StreamReader reader)
        {
            string message = "Parsing Campaign Settings... ";
            
            // bool parsingRegions = false;
            // List<string> regionSettingsContainer = new List<string>();

            CustomCampaignSettings campaign = null;
            
            string line;
            while (reader.Peek() >= 0)
            {
                line = reader.ReadLine();
                // if (line == "END_CAMPAIGN")
                // {
                //     break;
                // }
                //
                // if (campaign == null && line.StartsWith("ID"))
                // {
                //     campaign = new CustomCampaignSettings(ParseString(line));
                // }
                // else if (campaign == null)
                // {
                //     message += "Did not find Campaign ID after declaration, aborting!";
                //     Logger.LogInfo(message);
                //     return new Tuple<CustomCampaignSettings, StreamReader>(null, reader);
                // }
                //
                // if (line.StartsWith("PUPS_SPAWN"))
                // {
                //     campaign.SpawnsDynamicPups = ParseBool(line);
                //     continue;
                // }
                // if (line.StartsWith("MIN"))
                // {
                //     //campaign.MinPups = ParseInt(line);
                //     continue;
                // }
                // if (line.StartsWith("MAX"))
                // {
                //     //campaign.MaxPups = ParseInt(line);
                //     continue;
                // }
                //
                // if (line == "END_REGIONS")
                // {
                //     parsingRegions = false;
                // }
                // if (line == "REGIONS" || parsingRegions)
                // {
                //     parsingRegions = true;
                //     Tuple<CustomRegionSettings, StreamReader> result = ParseRegionSettings(reader);
                //     campaign.AddCampaignRegionSettings(result.Item1);
                //     reader = result.Item2;
                // }
            }

            //message += "WARNING: Reached end of settings file!";
            //Logger.LogInfo(message);
            return new Tuple<CustomCampaignSettings, StreamReader>(campaign, reader);
        }

        private Tuple<CustomRegionSettings, StreamReader> ParseRegionSettings(StreamReader reader)
        {
            string message = "Parsing region settings... ";
            
            CustomRegionSettings region = null;
            //string acronym = null;
            
            string line;
            while (reader.Peek() >= 0)
            {
                line = reader.ReadLine();
                // if (line == "END_REGION")
                // {
                //     message += "created CustomRegionSettings object for " + region.RegionAcronym + "!";
                //     Logger.LogInfo(message);
                //     return new Tuple<CustomRegionSettings, StreamReader>(region, reader);
                // }
                //
                // if (acronym == null && line.StartsWith("AC"))
                // {
                //     acronym = ParseString(line);
                // }
                // else if (region == null && acronym != null && line.StartsWith("PUPS_SPAWN"))
                // {
                //     region = new CustomRegionSettings(acronym, ParseBool(line));
                // }
                // else if (region == null)
                // {
                //     message += "Did not find either Region acronym, spawn bool or both after declaration, aborting!";
                //     Logger.LogInfo(message);
                //     return new Tuple<CustomRegionSettings, StreamReader>(null, reader);
                // }
                //
                // if (line.StartsWith("MIN"))
                // {
                //     //region.MinPups = ParseInt(line);
                // }
                // if (line.StartsWith("MAX"))
                // {
                //     //region.MaxPups = ParseInt(line);
                // }
                //
                // if (line.StartsWith("ROOM_OVERRIDES_FORBIDDEN"))
                // {
                //     string forbiddenRooms = ParseString(line);
                //     string[] forbidden = Regex.Split(forbiddenRooms, ",");
                //     foreach (string s in forbidden)
                //     {
                //         region.AddOverriddenRoom(s, false);
                //     }
                // }
                // if (line.StartsWith("ROOM_OVERRIDES_ALLOWED"))
                // {
                //     string allowedRooms = ParseString(line);
                //     string[] allowed = Regex.Split(allowedRooms, ",");
                //     foreach (string s in allowed)
                //     {
                //         region.AddOverriddenRoom(s, false);
                //     }
                // }
                // if (line == "END_REGION")
                // {
                //     return new Tuple<CustomRegionSettings, StreamReader>(region, reader);
                // }
            }

            //message += "WARNING: Reached end of settings file!";
            //Logger.LogInfo(message);
            return new Tuple<CustomRegionSettings, StreamReader>(region, reader);
        }

        private int ParseInt(string setting)
        {
            string message = "Parsing int... ";
            
            int n = -1;
            string[] num = Regex.Split(setting, ":");
            if (num.Length == 2)
            {
                try
                {
                    n = Int32.Parse(num[1]);
                    message += "Succeeded!";
                }
                catch (Exception e)
                {
                    message += "Int32.Parse Error! See exceptions log.";
                    Debug.LogError(e.Message);
                }
            }
            else
            {
                message += "Failed to parse number value!\n";
            }
            Logger.LogInfo(message);

            return n;
        }

        private bool ParseBool(string setting)
        {
            string message = "Parsing bool... ";
            
            bool val = false;
            string[] boo = Regex.Split(setting, ":");
            if (boo.Length == 2)
            {
                if (boo[1] == "TRUE")
                {
                    val = true;
                }
                else if (boo[1] != "FALSE")
                {
                    message += "Failed to parse value for bool, will return False!\n";
                }
            }
            Logger.LogInfo(message);

            return val;
        }

        private string ParseString(string setting)
        {
            string message = "Parsing string... ";
            
            string val = null;
            string[] str = Regex.Split(setting, ":");
            if (str.Length == 2)
            {
                val = str[1];
            }
            else
            {
                message += "Failed to read string or no value is present!";
            }
            Logger.LogInfo(message);
            
            return val;
        }
    }
}
