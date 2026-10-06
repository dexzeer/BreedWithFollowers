using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace BreedWithFollowers
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dexzeer.cotl.breedwithfollowers";
        public const string PluginName = "Breed With Followers";
        public const string PluginVersion = "2.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} {PluginVersion} loading");

            var harmony = new Harmony(PluginGuid);
            // One patch class at a time, so a single bad patch cannot take the plugin down.
            foreach (var type in new Type[]
            {
                typeof(Patches.Patch_MatingMenu_Show),
                typeof(Patches.Patch_GetOrCreateBrain),
                typeof(Patches.Patch_GetOrCreateRelationship),
                typeof(Patches.Patch_GetChanceToMate),
                typeof(Patches.Patch_ConfirmMating),
                typeof(Patches.Patch_SexyTimeIE),
                typeof(Patches.Patch_ConfigureFollower_Fleece),
                typeof(Patches.Patch_TentLabel_OneFollower),
            })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogError($"Patch class {type.Name} failed: {e}"); }
            }

            Log.LogInfo($"{PluginName} loaded");
        }
    }
}
