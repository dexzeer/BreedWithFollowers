using System;
using System.Collections.Generic;
using HarmonyLib;
using Lamb.UI.FollowerSelect;

namespace BreedWithFollowers.Patches
{
    /// <summary>Adds the Lamb to the mating tent's follower picker.</summary>
    [HarmonyPatch(typeof(UIMatingMenuController), nameof(UIMatingMenuController.Show),
        new Type[] { typeof(Interaction_MatingTent), typeof(List<FollowerSelectEntry>) })]
    internal static class Patch_MatingMenu_Show
    {
        private static void Prefix(List<FollowerSelectEntry> followerSelectEntries)
        {
            if (followerSelectEntries == null) return;
            followerSelectEntries.Add(new FollowerSelectEntry(LambStandIn.Info, FollowerSelectEntry.Status.Available));
        }
    }

    /// <summary>The info card asks for a brain for each pick; give the Lamb the inert one, never a real one.</summary>
    [HarmonyPatch(typeof(FollowerBrain), nameof(FollowerBrain.GetOrCreateBrain), new Type[] { typeof(FollowerInfo) })]
    internal static class Patch_GetOrCreateBrain
    {
        private static bool Prefix(FollowerInfo info, ref FollowerBrain __result)
        {
            if (!LambStandIn.IsLamb(info)) return true;
            __result = LambStandIn.InertBrain;
            return false;
        }
    }

    /// <summary>Relationships are stored in save data; never create one involving the Lamb.</summary>
    [HarmonyPatch(typeof(FollowerBrainInfo), nameof(FollowerBrainInfo.GetOrCreateRelationship), new Type[] { typeof(int) })]
    internal static class Patch_GetOrCreateRelationship
    {
        private static bool Prefix(FollowerBrainInfo __instance, int ID, ref IDAndRelationship __result)
        {
            if (!LambStandIn.IsLamb(ID) && __instance.ID != LambStandIn.LambId) return true;
            __result = new IDAndRelationship { ID = ID, CurrentRelationshipState = IDAndRelationship.RelationshipState.Strangers };
            return false;
        }
    }

    /// <summary>The Lamb's followers love them: breeding with the Lamb always shows (and rolls) 100%.</summary>
    [HarmonyPatch(typeof(Interaction_MatingTent), nameof(Interaction_MatingTent.GetChanceToMate),
        new Type[] { typeof(FollowerBrain), typeof(FollowerBrain) })]
    internal static class Patch_GetChanceToMate
    {
        private static bool Prefix(FollowerBrain f1, FollowerBrain f2, ref float __result)
        {
            if (!LambStandIn.IsLambBrain(f1) && !LambStandIn.IsLambBrain(f2)) return true;
            __result = 1f;
            return false;
        }
    }
}
