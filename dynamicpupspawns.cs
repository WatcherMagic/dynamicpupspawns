using BepInEx;
using UnityEngine;

namespace RainWorld_Mod_Template
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
            //
        }

    }
}
