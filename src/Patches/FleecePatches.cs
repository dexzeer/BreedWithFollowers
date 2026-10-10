using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Spine;
using Spine.Unity;

namespace BreedWithFollowers.Patches
{
    /// <summary>
    /// The picker, the info card and the trait screen all draw a follower with
    /// SpineExtensions.ConfigureFollower(SkeletonGraphic, FollowerInfo). For the Lamb we swap that picture onto the
    /// player's own skeleton and apply "Lamb_&lt;fleece&gt;" (what the game's CharacterMenu does), so the Lamb wears their
    /// fleece instead of a follower shirt. Everyone else gets their original follower skeleton back, because the
    /// game reuses these pictures.
    /// </summary>
    [HarmonyPatch(typeof(SpineExtensions), nameof(SpineExtensions.ConfigureFollower), new Type[] { typeof(SkeletonGraphic), typeof(FollowerInfo) })]
    internal static class Patch_ConfigureFollower_Fleece
    {
        private sealed class Original { public SkeletonDataAsset Asset; }
        private static readonly ConditionalWeakTable<SkeletonGraphic, Original> OriginalAssets = new ConditionalWeakTable<SkeletonGraphic, Original>();

        private static void Prefix(SkeletonGraphic skeletonGraphic)
        {
            try
            {
                if (skeletonGraphic == null) return;
                Original original = OriginalAssets.GetOrCreateValue(skeletonGraphic);
                if (original.Asset == null)
                {
                    original.Asset = skeletonGraphic.skeletonDataAsset;
                }
                else if (skeletonGraphic.skeletonDataAsset != original.Asset)
                {
                    // A previous Lamb left this picture on the player's skeleton: put the follower one back.
                    skeletonGraphic.skeletonDataAsset = original.Asset;
                    skeletonGraphic.Initialize(true);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Fleece restore failed: {e.Message}");
            }
        }

        private static void Postfix(SkeletonGraphic skeletonGraphic, FollowerInfo followerInfo)
        {
            if (skeletonGraphic == null || !PlayerStandIn.IsStandIn(followerInfo)) return;
            try
            {
                PlayerFarming player = PlayerFarming.Instance;
                if (player == null || player.Spine == null || player.Spine.skeletonDataAsset == null) return;

                skeletonGraphic.skeletonDataAsset = player.Spine.skeletonDataAsset;
                skeletonGraphic.Initialize(true);

                int fleece = DataManager.Instance.PlayerVisualFleece;
                string skinName = (PlayerStandIn.IsGoat(followerInfo) || fleece == 1003) ? "Goat" : "Lamb_" + fleece;
                Skin fleeceSkin = skeletonGraphic.Skeleton.Data.FindSkin(skinName);
                if (fleeceSkin == null)
                {
                    Plugin.Log.LogWarning($"Skin '{skinName}' not found on the player skeleton; leaving the follower look");
                    return;
                }
                Skin skin = new Skin("Player Skin");
                skin.AddSkin(fleeceSkin);
                skeletonGraphic.Skeleton.SetSkin(skin);
                skeletonGraphic.Skeleton.SetSlotsToSetupPose();
                skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Fleece look failed, keeping the follower look: {e.Message}");
            }
        }
    }
}
