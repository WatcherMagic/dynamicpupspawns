using System.Collections.Generic;
using System.Linq;
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
        private readonly int _minPupsInRegion = 0;
        private readonly int _maxPupsInRegion = 10;
        
        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            //get rooms with unsubmerged den nodes
            Dictionary <AbstractRoom, int> validSpawnRooms = GetRoomsWithViableDens(self);
            string logMessage = "Rooms with Den Nodes:\n";
            foreach (KeyValuePair<AbstractRoom, int> pair in validSpawnRooms)
            {
                logMessage += pair.Key.name + " : " + pair.Value + "\n";
            }
            Logger.LogInfo(logMessage);
            Debug.Log(logMessage);
            
            //determine room spawn weight based on number of dens in room
            Dictionary<AbstractRoom, int> roomWeights = CalculateRoomSpawnWeight(validSpawnRooms);
            int totalSpawnWeight = 0;
            logMessage = "Approximate chance for pup spawn per room:\n";
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
            AbstractRoom spawnRoom;
            Dictionary<int[], AbstractRoom[]> sortedArrays = SortRooms(roomWeights);
            Logger.LogInfo("Received sorted weights and rooms: " + sortedArrays);
            
            //get random room for each pup
            for (int i = 0; i < pupNum; i++)
            {
                spawnRoom = RandomPickRoomByWeight(sortedArrays.ElementAt(0).Key, sortedArrays.ElementAt(0).Value, totalSpawnWeight);
                Logger.LogInfo(spawnRoom.name + " picked for Pup " + (i + 1));
            }

            return orig(self);
        }

        private Dictionary<AbstractRoom, int> GetRoomsWithViableDens(World world)
        {
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

        private Dictionary<int[], AbstractRoom[]> SortRooms(Dictionary<AbstractRoom, int> roomWeights)
        {
            //move weights and rooms into parallel arrays
            int[] weights = new int[roomWeights.Count];
            AbstractRoom[] rooms = new AbstractRoom[roomWeights.Count];
            int index = 0;
            foreach (KeyValuePair<AbstractRoom, int> pair in roomWeights)
            {
                weights[index] = pair.Value;
                rooms[index] = pair.Key;
                index++;
            }
            
            //sort parallel arrays weights[] and rooms[] by ascending weight (bubble sort)
            bool swapped;
            int tempWeight;
            AbstractRoom tempRoom;
            for (int i = 0; i < roomWeights.Count; i++)
            {
                swapped = false;
                for (int x = 0; x < roomWeights.Count; x++)
                {
                    if (weights[x] > weights[x + 1])
                    {
                        tempWeight = weights[x];
                        tempRoom = rooms[x];
                        
                        weights[x] = weights[x + 1];
                        rooms[x] = rooms[x + 1];
                        
                        weights[x + 1] = tempWeight;
                        rooms[x + 1] = tempRoom;
                        
                        swapped = true;
                    }
                }

                if (!swapped)
                {
                    break;
                }
            }
            Logger.LogInfo("Sorted arrays of weights and rooms:");
            Logger.LogInfo(weights);
            Logger.LogInfo(rooms);

            return new Dictionary<int[], AbstractRoom[]> {{weights, rooms}};
        }
        
        private AbstractRoom RandomPickRoomByWeight(int[] weightsArray, AbstractRoom[] roomsArray, int totalWeight)
        {
            Logger.LogInfo("Picking room...");

            int roomIndex = 0;
            int randNum = Random.Range(0, totalWeight + 1);
            for (int i = 0; i < weightsArray.Length; i++)
            {
                if (i == weightsArray.Length || weightsArray[i] <= randNum && randNum <= weightsArray[i + 1])
                {
                    roomIndex = i;
                    break;
                }
            }
            
            Logger.LogInfo("Picked " + roomsArray[roomIndex].name);
            return roomsArray[roomIndex];
        }
    }
}
