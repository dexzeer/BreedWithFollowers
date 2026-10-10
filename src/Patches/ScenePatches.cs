using System;
using System.Collections;
using HarmonyLib;

namespace BreedWithFollowers.Patches
{
    /// <summary>Remembers who was picked next to the player when "Mate" is confirmed in the tent menu.</summary>
    [HarmonyPatch(typeof(UIMatingMenuController), nameof(UIMatingMenuController.ConfirmMatingButtonPress), new Type[0])]
    internal static class Patch_ConfirmMating
    {
        public static FollowerInfo PendingPartner;

        private static void Prefix(UIMatingMenuController __instance)
        {
            var t = Traverse.Create(__instance);
            FollowerInfo a = t.Field("follower1").GetValue<FollowerInfo>();
            FollowerInfo b = t.Field("follower2").GetValue<FollowerInfo>();
            if (PlayerStandIn.IsStandIn(a) && b != null && !PlayerStandIn.IsStandIn(b)) PendingPartner = b;
            else if (PlayerStandIn.IsStandIn(b) && a != null && !PlayerStandIn.IsStandIn(a)) PendingPartner = a;
            else PendingPartner = null;
        }
    }

    /// <summary>When the pair includes a player (Lamb or Goat), replace the tent's private SexyTimeIE with our scene.</summary>
    [HarmonyPatch(typeof(Interaction_MatingTent), "SexyTimeIE", new Type[0])]
    internal static class Patch_SexyTimeIE
    {
        private static bool Prefix(Interaction_MatingTent __instance, ref IEnumerator __result)
        {
            FollowerInfo partner = Patch_ConfirmMating.PendingPartner;
            Patch_ConfirmMating.PendingPartner = null;
            if (partner == null) return true; // normal follower + follower mating: vanilla code

            Follower follower = FollowerManager.FindFollowerByID(partner.ID);
            if (follower == null)
            {
                Plugin.Log.LogError($"Player mating: follower {partner.ID} is not in the world; falling back to vanilla");
                return true;
            }
            __result = LambMatingScene.Run(__instance, follower);
            return false;
        }
    }
}
