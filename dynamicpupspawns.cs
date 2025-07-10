using System.Collections.Generic;
using BepInEx;
using UnityEngine;

namespace DynamicPupSpawns
{
    //BEFORE LOADING MOD, ADD TO modinfo.json FOR EACH DEPENDENCY:

    //hard dependencies:
    //"requirements": ["mod_id_1", "mod_id_2", "etc"],

    //soft dependencies:
    //"priorities": ["mod_id_1", "mod_id_2", "etc"]

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class DynamicPupSpawns : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dynamicpupspawns";
        public const string PLUGIN_NAME = "Dynamic Pup Spawns";
        public const string PLUGIN_VERSION = "0.1";

        private void OnEnable()
        {
            On.World.SpawnPupNPCs += SpawnPups;
        }

        private int SpawnPups(On.World.orig_SpawnPupNPCs orig, World self)
        {
            Logger.LogInfo("SpawnPupOnWorldLoad() initiated");
            Debug.Log("SpawnPupOnWorldLoad() initiated");

            Logger.LogInfo("StorySession status: " + self.game.IsStorySession);
            Debug.Log("StorySession status: " + self.game.IsStorySession);

            //int pupsThisCycle = UnityEngine.Random.Range(0, 11);
            //Debug.Log("Spawning " + pupsThisCycle + " pups");

            Dictionary <AbstractRoom, int> validSpawnRooms = GetRoomsWithDens(self);
            string logMessege = "Rooms with Den Nodes:\n";
            foreach (KeyValuePair<AbstractRoom, int> pair in validSpawnRooms)
            {
                logMessege += pair.Key.name + " : " + pair.Value + "\n";
            }
            Logger.LogInfo(logMessege);
            Debug.Log(logMessege);

            return orig(self);
        }

        private Dictionary<AbstractRoom, int> GetRoomsWithDens(World world)
        {
            Dictionary<AbstractRoom, int> roomsWithDens = new Dictionary<AbstractRoom, int>();
            int densInRoom = 0;

            foreach (AbstractRoom room in world.abstractRooms)
            {
                foreach (AbstractRoomNode node in room.nodes)
                {
                    if (node.type == AbstractRoomNode.Type.Den)
                    {
                        densInRoom++;
                    }
                }
                if (densInRoom > 0)
                {
                    roomsWithDens.Add(room, densInRoom);
                }
                densInRoom = 0;
            }

            return roomsWithDens;
        }
    }
}
