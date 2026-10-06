using System;
using HarmonyLib;
using I2.Loc;

namespace BreedWithFollowers.Patches
{
    /// <summary>
    /// Vanilla only lets you open the tent with 2+ available followers. With the Lamb as a partner, one is enough.
    /// Everything else about the tent's label (mating in progress, co-op, no followers at all) is left alone.
    /// </summary>
    [HarmonyPatch(typeof(Interaction_MatingTent), nameof(Interaction_MatingTent.GetLabel), new Type[0])]
    internal static class Patch_TentLabel_OneFollower
    {
        private static void Postfix(Interaction_MatingTent __instance)
        {
            if (__instance.Interactable) return;
            if (DataManager.Instance.MatingCompletedTimestamp != -1f) return; // someone is mating / waiting to come out

            var brain = Traverse.Create(__instance).Field("structureBrain").GetValue<Structures_MatingTent>();
            if (brain != null && brain.Data != null && brain.Data.MultipleFollowerIDs.Count > 0) return;

            int available = 0;
            foreach (Follower follower in Follower.Followers)
            {
                if (!FollowerManager.FollowerLocked(follower.Brain.Info.ID, false, false, false, true, false))
                {
                    available++;
                }
            }
            if (available != 1) return;

            __instance.Label = LocalizationManager.GetTranslation("Interactions/InviteFollowers") + " " + InventoryItem.CapacityString(InventoryItem.ITEM_TYPE.PLEASURE_POINT, 1);
            __instance.Interactable = true;
        }
    }
}
