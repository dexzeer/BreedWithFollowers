using System.Collections.Generic;
using System.Runtime.Serialization;
using I2.Loc;

namespace BreedWithFollowers
{
    /// <summary>
    /// Stand-ins for the player characters (Lamb and, in co-op, the Goat) so the mating tent's own picker can list them.
    /// They are never added to DataManager, never get a real FollowerBrain (that ctor hooks dozens of global events)
    /// and so never reach the save file.
    /// </summary>
    internal static class PlayerStandIn
    {
        // Far above any real follower id (counter) and the special ids the game uses.
        public const int LambId = 1337001;
        public const int GoatId = 1337002;

        private static FollowerInfo _lamb;
        private static FollowerInfo _goat;
        private static FollowerBrain _lambBrain;
        private static FollowerBrain _goatBrain;

        public static FollowerInfo Lamb => _lamb ?? (_lamb = Create(LambId, "Lamb"));

        public static FollowerInfo Goat => _goat ?? (_goat = Create(GoatId, LocalizationManager.GetTranslation("NAMES/Knucklebones/Goat")));

        /// <summary>The game's own test for "this player is drawn as the Goat" (see PlayerFarming skin selection).</summary>
        public static bool IsGoatPlayer(PlayerFarming player) => player != null && (!player.isLamb || player.IsGoat);

        public static FollowerInfo For(PlayerFarming player) => IsGoatPlayer(player) ? Goat : Lamb;

        public static bool IsStandIn(FollowerInfo info) => info != null && IsStandIn(info.ID);

        public static bool IsStandIn(int id) => id == LambId || id == GoatId;

        public static bool IsGoat(FollowerInfo info) => info != null && info.ID == GoatId;

        public static bool IsStandInBrain(FollowerBrain brain) => brain != null && brain.Info != null && IsStandIn(brain.Info.ID);

        /// <summary>
        /// A FollowerBrain built WITHOUT running its constructor, so it is not registered anywhere and subscribes
        /// to nothing. Only .Info and ._directInfoAccess are filled.
        /// </summary>
        public static FollowerBrain InertBrainFor(FollowerInfo info)
        {
            if (IsGoat(info))
            {
                return _goatBrain ?? (_goatBrain = MakeInert(Goat));
            }
            return _lambBrain ?? (_lambBrain = MakeInert(Lamb));
        }

        private static FollowerBrain MakeInert(FollowerInfo info)
        {
            var brain = (FollowerBrain)FormatterServices.GetUninitializedObject(typeof(FollowerBrain));
            brain.Info = new FollowerBrainInfo(info, brain);
            brain._directInfoAccess = info;
            return brain;
        }

        private static FollowerInfo Create(int id, string name)
        {
            // FollowerInfo's ctor is private; NewCharacter is the only factory but it
            // consumes a real follower id, so put the counter back afterwards.
            int savedFollowerId = DataManager.Instance.FollowerID;
            FollowerInfo info = FollowerInfo.NewCharacter(FollowerLocation.Base, "Lamb");
            DataManager.Instance.FollowerID = savedFollowerId;

            info.ID = id;
            info.Name = name;
            // The follower skeleton has no Goat skin; the fleece patch redraws both players from the player skeleton.
            info.SkinName = "Lamb";
            info.SkinCharacter = WorshipperData.Instance.GetSkinIndexFromName("Lamb");
            info.SkinVariation = 0;
            info.SkinColour = 0;
            info.Age = 25;
            info.LifeExpectancy = 100; // never reads as elderly
            info.XPLevel = 10;         // max level, so they sort to the top of the picker
            info.Traits = new List<FollowerTrait.TraitType>();
            info.TraitsSet = true;
            return info;
        }
    }
}
