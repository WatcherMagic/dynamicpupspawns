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
                PickRandomRoomByWeight(parallelArrays.ElementAt(0).Key, weightsScale);
                // Logger.LogInfo(spawnRoom.name + " picked for Pup " + (i + 1));
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
    }
}
