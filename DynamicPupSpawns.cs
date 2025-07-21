using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly int _minPupsInRegion = 1;
        private readonly int _maxPupsInRegion = 10;

        private string testSaveString = "Saved Data";
        private int testSaveInt = 89001;
        
        private string modSymbolStart = "<DynamicPups.WatcherMagicB>";
        private string splitSymbolStart = "<DPS.WMB>";
        private string splitSymbolEnd = "<DPS.WME";
        private string modSymbolEnd = "<DynamicPups.WatcherMagicE>";
        
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;

            On.MiscWorldSaveData.ToString += SaveToString;
            On.MiscWorldSaveData.FromString += SaveDataFromString;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            //get rooms with unsubmerged den nodes
            Dictionary <AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);
            
            //determine room spawn weight based on number of dens in room
            Dictionary<AbstractRoom, float> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);
            
            //generate number of pups for this cycle
            int pupNum = Random.Range(_minPupsInRegion, _maxPupsInRegion + 1);
            Logger.LogInfo(pupNum + " pups this cycle");
            
            //get dict of rooms and weights in parallel arrays
            Dictionary<AbstractRoom[], float[]> parallelArrays = CreateParallelRoomWeightArrays(roomWeights);
            float[] weightsScale = AssignSortedRoomScaleValues(parallelArrays.ElementAt(0).Value);
            
            //get random room for each pup
            for (int i = 0; i < pupNum; i++)
            {
                AbstractRoom spawnRoom = PickRandomRoomByWeight(parallelArrays.ElementAt(0).Key, weightsScale);
                if (self.game.IsStorySession) //temp
                {
                    PutPupInRoom(self.game, self, spawnRoom, null, self.game.GetStorySession.characterStats.name);
                }
            }

            return orig(self);
        }

        private Dictionary<AbstractRoom, int> GetRoomsWithViableDens(World world)
        {
            //get all rooms in region with den nodes that are not submerged
            Dictionary<AbstractRoom, int> roomsWithDens = new Dictionary<AbstractRoom, int>();
            
            int densInRoom = 0;
            foreach (AbstractRoom room in world.abstractRooms)
            {
                if (!room.offScreenDen)
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
            
            //Debug parallel arrays
            string message = "parallelArrays dict to be returned:\n";
            foreach (KeyValuePair<AbstractRoom[], float[]> pair in parallelArrays)
            {
                message += string.Format("{0, -9}|{1, 7}\n------------------\n", "ROOM", "WEIGHT");
                for (int i = 0; i < pair.Key.Length; i++)
                {
                    if (i < pair.Value.Length)
                    {
                        message += string.Format("{0, -9}|{1, 7:0.##%}\n", pair.Key[i].name, pair.Value[i]);
                    }
                    else
                    {
                        message += "The arrays are not the same length! Aborting Debug Statement";
                        break;
                    }
                }
            }
            Logger.LogInfo(message);
            
            return parallelArrays;
        }
        
        private float[] AssignSortedRoomScaleValues(float[] weightsArray)
        {
            //change indexes of weightsArray to increment to total weight in ascending order
            string message = string.Format("Modifying weightsArray to reflect scale of total weight:\n{0, -7}|{1, 7}\n---------------\n", "OLD", "NEW");
            float scaleValue = 0f;
            for (int i = 0; i < weightsArray.Length; i++)
            {
                message += string.Format("{0, -7:0.##%}|", weightsArray[i]);
                scaleValue += weightsArray[i];
                weightsArray[i] = scaleValue;
                message += string.Format("{0, 7:0.##%}\n", weightsArray[i]);
            }
            message += "Total scale weight: " + scaleValue.ToString("0.##%");
            Logger.LogInfo(message);
            
            return weightsArray;
        }
        
        private AbstractRoom PickRandomRoomByWeight(AbstractRoom[] roomsArray, float[] weightsArray)
        {
            //pick room that corresponds to the randomly selected number on the weight scale
            float totalWeight = weightsArray[weightsArray.Length - 1];
            
            int roomIndex;
            float randNum = Random.Range(0f, totalWeight);
            Logger.LogInfo("Room selection: random number is " + randNum.ToString("0.##%"));
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
            Logger.LogInfo("Index selected is " + roomIndex + " (" + weightsArray[roomIndex].ToString("0.##%") + " <= " + randNum.ToString("0.##%") + " <= " + weightsArray[roomIndex + 1].ToString("0.##%") + ")");
            
            return roomsArray[roomIndex];
        }
        
        private void PutPupInRoom(RainWorldGame game, World world, AbstractRoom room, Player pup, SlugcatStats.Name curSlug)
        {
            bool temp = false;
            if (ModManager.MSC && game.IsStorySession)
            {
                if (ModManager.Watcher && curSlug == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                {
                    temp = true;
                }
                //spawn new pup with random ID
                if (pup == null && !temp)
                {
                    //copied from AbstractRoom.RealizeRoom()
                    AbstractCreature abstractPup = new AbstractCreature(world,
                        StaticWorld.GetCreatureTemplate(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC),
                        null,
                        new WorldCoordinate(room.index, -1, -1, 0),
                        game.GetNewID());

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
                        
                        Logger.LogInfo(abstractPup.GetType() + " " + abstractPup.ID + " spawned in " + abstractPup.Room.name);
                        Debug.Log("DynamicPupSpawns: " + abstractPup.GetType() + " " + abstractPup.ID + " spawned in " + abstractPup.Room.name);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.Message);
                    }
                }
            }
        }
        
        private string SaveToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
        {   
            string s = orig(self);
            
            Logger.LogInfo("Beginning saving string for MiscWorldData");

            string save = modSymbolStart + splitSymbolStart + "testSaveString:" + testSaveString + splitSymbolEnd
                          + splitSymbolStart + "testSaveInt:" + testSaveInt + splitSymbolEnd + modSymbolEnd;
            Logger.LogInfo("String to be saved:\n" + save);
            
            s.Concat(save);
            
            return s;
        }
        
        private void SaveDataFromString(On.MiscWorldSaveData.orig_FromString orig, MiscWorldSaveData self, string s)
        {
            orig(self, s);
            
            Logger.LogInfo("Looking for mod save string for MiscWorldData");
            
            foreach (String u in self.unrecognizedSaveStrings)
            {
                if (u.StartsWith(modSymbolStart))
                {
                    Logger.LogInfo("Found the save string!");
                }
            }
        }
    }
}
