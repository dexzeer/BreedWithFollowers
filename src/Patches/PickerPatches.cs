using System;
using System.Collections.Generic;
using HarmonyLib;
using Lamb.UI.FollowerSelect;

namespace BreedWithFollowers.Patches
{
    /// <summary>
    /// Adds the player who opened the tent (the Lamb, or the Goat in co-op) to the mating tent's follower picker.
    /// The tent already knows who interacted: Interaction.playerFarming.
    /// </summary>
    [HarmonyPatch(typeof(UIMatingMenuController), nameof(UIMatingMenuController.Show),
        new Type[] { typeof(Interaction_MatingTent), typeof(List<FollowerSelectEntry>) })]
    internal static class Patch_MatingMenu_Show
    {
        private static void Prefix(Interaction_MatingTent matingTent, List<FollowerSelectEntry> followerSelectEntries)
        {
            if (followerSelectEntries == null) return;
            PlayerFarming player = matingTent != null ? matingTent.playerFarming : PlayerFarming.Instance;
            followerSelectEntries.Add(new FollowerSelectEntry(PlayerStandIn.For(player), FollowerSelectEntry.Status.Available));
        }
    }

    /// <summary>The info card asks for a brain for each pick; give a player stand-in the inert one, never a real one.</summary>
    [HarmonyPatch(typeof(FollowerBrain), nameof(FollowerBrain.GetOrCreateBrain), new Type[] { typeof(FollowerInfo) })]
    internal static class Patch_GetOrCreateBrain
    {
        private static bool Prefix(FollowerInfo info, ref FollowerBrain __result)
        {
            if (!PlayerStandIn.IsStandIn(info)) return true;
            __result = PlayerStandIn.InertBrainFor(info);
            return false;
        }
    }

    /// <summary>Relationships are stored in save data; never create one involving a player stand-in.</summary>
    [HarmonyPatch(typeof(FollowerBrainInfo), nameof(FollowerBrainInfo.GetOrCreateRelationship), new Type[] { typeof(int) })]
    internal static class Patch_GetOrCreateRelationship
    {
        private static bool Prefix(FollowerBrainInfo __instance, int ID, ref IDAndRelationship __result)
        {
            if (!PlayerStandIn.IsStandIn(ID) && !PlayerStandIn.IsStandIn(__instance.ID)) return true;
            __result = new IDAndRelationship { ID = ID, CurrentRelationshipState = IDAndRelationship.RelationshipState.Strangers };
            return false;
        }
    }

    /// <summary>Your followers love you: breeding with a player always shows (and rolls) 100%.</summary>
    [HarmonyPatch(typeof(Interaction_MatingTent), nameof(Interaction_MatingTent.GetChanceToMate),
        new Type[] { typeof(FollowerBrain), typeof(FollowerBrain) })]
    internal static class Patch_GetChanceToMate
    {
        private static bool Prefix(FollowerBrain f1, FollowerBrain f2, ref float __result)
        {
            if (!PlayerStandIn.IsStandInBrain(f1) && !PlayerStandIn.IsStandInBrain(f2)) return true;
            __result = 1f;
            return false;
        }
    }
}
