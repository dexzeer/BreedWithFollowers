using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BreedWithFollowers
{
    /// <summary>
    /// A stand-in "follower" for the Lamb so the mating tent's own picker can list them.
    /// It is never added to DataManager, never gets a real FollowerBrain (that ctor hooks
    /// dozens of global events) and so never reaches the save file.
    /// </summary>
    internal static class LambStandIn
    {
        // Far above any real follower id (counter) and the special ids the game uses.
        public const int LambId = 1337001;

        private static FollowerInfo _info;
        private static FollowerBrain _inertBrain;

        public static FollowerInfo Info
        {
            get
            {
                if (_info == null)
                {
                    // FollowerInfo's ctor is private; NewCharacter is the only factory but it
                    // consumes a real follower id, so put the counter back afterwards.
                    int savedFollowerId = DataManager.Instance.FollowerID;
                    _info = FollowerInfo.NewCharacter(FollowerLocation.Base, "Lamb");
                    DataManager.Instance.FollowerID = savedFollowerId;

                    _info.ID = LambId;
                    _info.Name = "Lamb";
                    _info.SkinName = "Lamb";
                    _info.SkinCharacter = WorshipperData.Instance.GetSkinIndexFromName("Lamb");
                    _info.SkinVariation = 0;
                    _info.SkinColour = 0;
                    _info.Age = 25;
                    _info.LifeExpectancy = 100; // never reads as elderly
                    _info.XPLevel = 10;         // max level, so the Lamb sorts to the top of the picker
                    _info.Traits = new List<FollowerTrait.TraitType>();
                    _info.TraitsSet = true;
                }
                return _info;
            }
        }

        public static bool IsLamb(FollowerInfo info) => info != null && info.ID == LambId;

        public static bool IsLamb(int id) => id == LambId;

        public static bool IsLambBrain(FollowerBrain brain) => brain != null && brain.Info != null && brain.Info.ID == LambId;

        /// <summary>
        /// A FollowerBrain built WITHOUT running its constructor, so it is not registered
        /// anywhere and subscribes to nothing. Only .Info and ._directInfoAccess are filled.
        /// </summary>
        public static FollowerBrain InertBrain
        {
            get
            {
                if (_inertBrain == null)
                {
                    _inertBrain = (FollowerBrain)FormatterServices.GetUninitializedObject(typeof(FollowerBrain));
                    _inertBrain.Info = new FollowerBrainInfo(Info, _inertBrain);
                    _inertBrain._directInfoAccess = Info;
                }
                return _inertBrain;
            }
        }
    }
}
