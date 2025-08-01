using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace dynamicpupspawns
{
    //BEFORE LOADING MOD, ADD TO modinfo.json FOR EACH DEPENDENCY:

    //hard dependencies:
    //"requirements": ["mod_id_1", "mod_id_2", "etc"],

    //soft dependencies:
    //"priorities": ["mod_id_1", "mod_id_2", "etc"]

    [BepInPlugin("dynamicpupspawns", "Dynamic Pup Spawns", "0.1")]
    public class DynamicPupSpawns : BaseUnityPlugin
    {
        private World _world = null;
        private Dictionary<string, string> _persistentPups = null;
        
        private const string _SAVE_DATA_DELIMITER = "DynamicPupSpawnsData";
        private const string _REGX_STR_SPLIT = "<WM,DPS>";
        private const string _PUP_DATA_DIVIDER = ":";
        
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;

            On.SaveState.SaveToString += SaveDataToString;
            On.SaveState.LoadGame += GetSaveDataFromString;
        }
        
        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            _world = self;
            
            int minPupsInRegion = 1;
            int maxPupsInRegion = 10;
            
            //get rooms with unsubmerged den nodes
            Dictionary <AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);
            
            //determine room spawn weight based on number of dens in room
            Dictionary<AbstractRoom, float> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);
            
            //get dict of rooms and weights in parallel arrays
            Dictionary<AbstractRoom[], float[]> parallelArrays = CreateParallelRoomWeightArrays(roomWeights);
            float[] weightsScale = AssignSortedRoomScaleValues(parallelArrays.ElementAt(0).Value);
            
            //generate number of pups for this cycle
            // + 1 to max to account for rounding down w/ cast to int
            int pupNum = RandomPupGaussian(minPupsInRegion, maxPupsInRegion + 1);
            
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

            float u, v, S;

            do
            {
                u = 2.0f * Random.value - 1.0f;
                v = 2.0f * Random.value - 1.0f;
                S = u * u + v * v;
            }
            while (S >= 1.0f);

            // Standard Normal Distribution
            float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

            // clamped following the "three-sigma rule"
            float mean = min + (max - min) * 0.3f;
            Logger.LogInfo("Gausian mean: " + mean.ToString("00.##"));
            Debug.Log("DynamicPupSpawns: Gaussian mean: " + mean);
            float sigma = (max - mean) / 3.0f;
            float result = Mathf.Clamp(std * sigma + mean, min, max);
            Logger.LogInfo("Gausian random: " + result.ToString("00.##"));
            Debug.Log("DynamicPupSpawns: Gausian random: " + result.ToString("00.##"));
            Logger.LogInfo("Gausian random int: " + (int)result);
            Debug.Log("DynamicPupSpawns: Gausian random int: " + (int)result);
            
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

        private Dictionary<AbstractRoom[], float[]> CreateParallelRoomWeightArrays(Dictionary<AbstractRoom, float> roomWeights)
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
            Dictionary<AbstractRoom[], float[]> parallelArrays = new Dictionary<AbstractRoom[], float[]> { { rooms, weights } };
            
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
                                
                                PutPupInRoom(world.game, world, room, pup.Key, world.game.GetStorySession.characterStats.name);
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

        private void PutPupInRoom(RainWorldGame game, World world, AbstractRoom room, string pupID,
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
                        
                        Logger.LogInfo(abstractPup.creatureTemplate.type + " " + abstractPup.ID + " spawned in " + abstractPup.Room.name + (persistent ? " PERSISTENT" : ""));
                        Debug.Log("DynamicPupSpawns: " + abstractPup.creatureTemplate.type + " " + abstractPup.ID + " spawned in " + abstractPup.Room.name + (persistent ? " PERSISTENT" : ""));
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
        
            string[] recognizedPupTypes = { "SlugNPC"/*, "Bup"*/ };

            string data = _SAVE_DATA_DELIMITER;
            
            string message = "Adding pups to save data...\n";
            if (_world != null)
            {
                //make sure not to save pups in shelter player is ins
                
                for (int i = 0; i < _world.abstractRooms.Length; i++)
                {
                    //message += "Iterating over " + _world.abstractRooms[i].name + ":\n";
                    if (!_world.abstractRooms[i].shelter)
                    {
                        foreach (AbstractCreature abstractCreature in _world.abstractRooms[i].creatures)
                        {
                            //List<string> roomExceptions = new List<string>();
                            //message += "Found creature! " + abstractCreature.creatureTemplate.type + "\n";
                        
                            /*ISSUE: abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat
                             only detects players, not SlugNPCs. Additionally, Bups and likely others
                             are apparently different templates from SlugNPC. Hardcoded workaround for now.*/
                            foreach (string pupType in recognizedPupTypes)
                            {
                                if (abstractCreature.creatureTemplate.type.ToString() == pupType)
                                {
                                    data += abstractCreature.ID + _PUP_DATA_DIVIDER + _world.abstractRooms[i].name + _REGX_STR_SPLIT;
                                }
                            }
                        }
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
        
        //currently holdover
        private void SubstringEntityID(AbstractCreature abstractCreature)
        {
            string id = abstractCreature.ID.ToString();
            if (!id.EndsWith("."))
            {
                int substrIndex = id.LastIndexOf('.') + 1;
                id = id.Substring(substrIndex, id.Length - substrIndex);
                bool isDigits = true;
                foreach (char c in id)
                {
                    if (!char.IsDigit(c))
                    {
                        isDigits = false;
                        break;
                    }
                }

                if (isDigits)
                {
                    int idNum = int.Parse(id);
                    //message += idNum + "\n";
                }
                else
                {
                    //message += "Failed to retrieve int from ID substring!\n";
                }   
            }
            else
            {
                //message += "ID substring ended in '.'!\n";
            }
        }
        
        private void GetSaveDataFromString(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
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
                    message +=  pair.Key + " : " + pair.Value + "\n";
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
    }
}
