using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DynamicPupSpawns
{
    //BEFORE LOADING MOD, ADD TO modinfo.json FOR EACH DEPENDENCY:

    //hard dependencies:
    //"requirements": ["mod_id_1", "mod_id_2", "etc"],

    //soft dependencies:
    //"priorities": ["mod_id_1", "mod_id_2", "etc"]

    [BepInPlugin("dynamicpupspawns", "Dynamic Pup Spawns", "0.1")]
    public class DynamicPupSpawns : BaseUnityPlugin
    {
        private readonly int _minPupsInRegion = 5;
        private readonly int _maxPupsInRegion = 10;
        
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            //get rooms with unsubmerged den nodes
            Dictionary <AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);

            // string logMessage = "Rooms with Den Nodes:\n";
            // foreach (KeyValuePair<AbstractRoom, int> pair in validSpawnRooms)
            // {
            //     logMessage += pair.Key.name + " : " + pair.Value + "\n";
            // }
            // Logger.LogInfo(logMessage);
            // Debug.Log(logMessage);
            
            //determine room spawn weight based on number of dens in room
            Dictionary<AbstractRoom, int> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);
            int totalSpawnWeight = 0;
            string logMessage = "Approximate chance for pup spawn per room:\n";
            foreach (KeyValuePair<AbstractRoom, int> pair in roomWeights)
            {
                logMessage += pair.Key.name + " : " + pair.Value.ToString("00") + "%\n";
                totalSpawnWeight += pair.Value;
            }
            Logger.LogInfo(logMessage);
            Logger.LogInfo("Total weight of spawn rooms = " + totalSpawnWeight);
            
            //generate number of pups for this cycle
            int pupNum = Random.Range(_minPupsInRegion, _maxPupsInRegion + 1);
            Logger.LogInfo(pupNum + " pups this cycle");
            
            //sort dict of rooms and wights into parallel arrays in ascending order
            //Dictionary<int[], AbstractRoom[]> sortedArrays = CreateParallelRoomWeightArrays(roomWeights);
            CreateParallelRoomWeightArrays();
            //Logger.LogInfo("Received sorted weights and rooms.");
            //int[] weightsScale = AssignSortedRoomScaleValues(sortedArrays.ElementAt(0).Key);
            AssignSortedRoomScaleValues();
            //Logger.LogInfo("Received reassigned weights array.");
            
            //get random room for each pup
            for (int i = 0; i < pupNum; i++)
            {
                RandomPickRoomByWeight();
                // AbstractRoom spawnRoom = RandomPickRoomByWeight(weightsScale, sortedArrays.ElementAt(0).Value);
                // Logger.LogInfo("Received randomly selected room.");
                // Logger.LogInfo(spawnRoom.name + " picked for Pup " + (i + 1));
            }

            return orig(self);
        }

        private Dictionary<AbstractRoom, int> GetRoomsWithViableDens(World world)
        {
            Logger.LogInfo("Entered GetRoomsWithViableDens()");
            
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
            Logger.LogInfo("Exited GetRoomWithViableDens()");
            
            return roomsWithDens;
        }

        private Dictionary<AbstractRoom, int> CalculateRoomSpawnWeight(Dictionary<AbstractRoom, int> roomsAndDens)
        {
             Dictionary<AbstractRoom, int> spawnWeights = new Dictionary<AbstractRoom, int>();
            
             int totalDens = 0;
             foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
             {
                 totalDens += pair.Value;
             }
            
             int weight;
             foreach (KeyValuePair<AbstractRoom, int> pair in roomsAndDens)
             {
                 weight = Mathf.RoundToInt(pair.Value / (float)totalDens * 100);
                 spawnWeights.Add(pair.Key, weight);                    
            
             }
            return spawnWeights;
        }

        private /*Dictionary<int[], AbstractRoom[]>*/ void CreateParallelRoomWeightArrays(/*Dictionary<AbstractRoom, int> roomWeights*/)
        {
            Logger.LogInfo("Entered CreateParallelRoomWeightArrays()");
            
            //move weights and rooms into parallel arrays
            // int[] weights = new int[roomWeights.Count];
            // AbstractRoom[] rooms = new AbstractRoom[roomWeights.Count];
            // int index = 0;
            // foreach (KeyValuePair<AbstractRoom, int> pair in roomWeights)
            // {
            //     weights[index] = pair.Value;
            //     rooms[index] = pair.Key;
            //     index++;
            // }
            
            //sort parallel arrays weights[] and rooms[] by ascending weight (bubble sort)
            // bool swapped;
            // int tempWeight;
            // AbstractRoom tempRoom;
            // for (int i = 0; i < roomWeights.Count; i++)
            // {
            //     swapped = false;
            //     for (int x = 0; x < roomWeights.Count; x++)
            //     {
            //         if (weights[x] > weights[x + 1])
            //         {
            //             tempWeight = weights[x];
            //             tempRoom = rooms[x];
            //             
            //             weights[x] = weights[x + 1];
            //             rooms[x] = rooms[x + 1];
            //             
            //             weights[x + 1] = tempWeight;
            //             rooms[x + 1] = tempRoom;
            //             
            //             swapped = true;
            //         }
            //     }
            //
            //     if (!swapped)
            //     {
            //         break;
            //     }
            // }
            // Logger.LogInfo("Sorted arrays of weights and rooms:");
            // Logger.LogInfo("Assigned weights and rooms to parallel arrays.");
            // Logger.LogInfo(weights);
            // Logger.LogInfo(rooms);
        
            //return new Dictionary<int[], AbstractRoom[]> {{weights, rooms}};
        }
        
        private /*int[]*/ void AssignSortedRoomScaleValues(/*int[] weightsArray*/)
        {
            Logger.LogInfo("Entered AssignSortedRoomScaleValues()");
            
            // int scaleValue = 0;
            // for (int i = 0; i < weightsArray.Length; i++)
            // {
            //     scaleValue += weightsArray[i];
            //     weightsArray[i] = scaleValue;
            // }
            // Logger.LogInfo("Weights array was modified to correspond to total weight scale and will be returned!");
            // Logger.LogInfo(weightsArray.ToString());
            //
            // return weightsArray;
        }
        
        private /*AbstractRoom*/ void RandomPickRoomByWeight(/*int[] weightsArray, AbstractRoom[] roomsArray*/)
        {
            Logger.LogInfo("Entered RandomPickRoomByWeight()");
            
            // Logger.LogInfo("Picking room...");
            // int totalWeight = weightsArray[weightsArray.Length - 1];
            //
            // int roomIndex = 0;
            // int randNum = Random.Range(0, totalWeight + 1);
            // Logger.LogInfo("Random number is " + randNum);
            // for (int i = 0; i < weightsArray.Length; i++)
            // {
            //     if (i == weightsArray.Length)
            //     {
            //         Logger.LogInfo("Selected final index");
            //         roomIndex = i;
            //     }
            //     else if (weightsArray[i] <= randNum && randNum <= weightsArray[i + 1])
            //     {
            //         Logger.LogInfo("Index selected is " + i + " (" + weightsArray[i] + " <= " + randNum + " <= " + weightsArray[i + 1] + ")");
            //         roomIndex = i;
            //         break;
            //     }
            // }
            //
            // return roomsArray[roomIndex];
        }
    }
}
