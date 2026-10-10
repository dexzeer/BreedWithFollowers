using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HarmonyLib;
using Lamb.UI;
using Spine;
using src.Extensions;
using UnityEngine;

namespace BreedWithFollowers
{
    /// <summary>
    /// The Lamb + follower version of Interaction_MatingTent.SexyTimeIE / SuccessfulMatingIE.
    /// Same beats as vanilla: consider, kiss, walk into the tent, pick traits, walk out with the egg.
    /// Differences (by design): always succeeds, only the follower is undressed / carries the egg / is exhausted,
    /// the Lamb is hidden inside the tent and comes out as they went in.
    /// </summary>
    internal static class LambMatingScene
    {
        // Vanilla's Kiss command uses 1, but the Lamb's kiss animation overlaps the follower at that spacing here.
        private const float KissDistance = 1.2f;

        public static IEnumerator Run(Interaction_MatingTent tent, Follower follower)
        {
            var t = Traverse.Create(tent);
            PlayerFarming player = tent.playerFarming;
            Structures_MatingTent structureBrain = t.Field("structureBrain").GetValue<Structures_MatingTent>();
            FollowerInfo info = follower.Brain._directInfoAccess;

            // The tent's OnHidden handler ends the conversation if either of its follower fields is null.
            // The Lamb has no Follower, so point both at the real one for the duration of the scene.
            t.Field("follower1").SetValue(follower);
            t.Field("follower2").SetValue(follower);

            player.GoToAndStop(tent.pos2.transform.position, follower.gameObject);
            Inventory.ChangeItemQuantity(InventoryItem.ITEM_TYPE.PLEASURE_POINT, -1);
            Time.timeScale = 1f;
            GameManager.GetInstance().OnConversationNew();
            GameManager.GetInstance().OnConversationNext(tent.gameObject, 6f);
            SimulationManager.Pause();

            bool zombie = follower.Brain.HasTrait(FollowerTrait.TraitType.Zombie);
            follower.Brain.HardSwapToTask(new FollowerTask_ManualControl());
            follower.transform.position = tent.pos1.transform.position;
            follower.FacePosition(tent.sexyPos.transform.position);
            yield return new WaitForEndOfFrame();

            // The follower considers the Lamb's offer...
            follower.SetBodyAnimation(zombie ? "Zombie/zombie-idle" : RandomConsiderAnim(), loop: true);
            yield return new WaitForSeconds(zombie ? 0.5f : 3f);

            // ...and, because they love the Lamb, accepts.
            follower.SetBodyAnimation(zombie ? "Zombie/zombie-idle" : (UnityEngine.Random.value < 0.5f ? "Reactions/react-loved2" : "Reactions/react-loved"), loop: false);
            follower.AddBodyAnimation("idle", loop: true, 0f);
            yield return new WaitForSeconds(zombie ? 0.5f : 1.5f);

            DataManager.Instance.HadInitialMatingTentInteraction = true;

            yield return Smooch(follower, player, zombie);

            // Both walk into the tent.
            follower.GoTo(tent.sexyPos.transform.position, () => follower.FacePosition(player.transform.position));
            bool lambInside = false;
            player.GoToAndStop(tent.sexyPos.transform.position, null, IdleOnEnd: false, DisableCollider: false, () =>
            {
                player.gameObject.SetActive(value: false);
                lambInside = true;
            });
            yield return new WaitForSeconds(0.5f);
            float timeout = 5f;
            while (!lambInside && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!lambInside) player.gameObject.SetActive(value: false);

            follower.HideAllFollowerIcons();
            follower.Spine.gameObject.SetActive(value: false);

            follower.Brain.HardSwapToTask(new FollowerTask_Mating());
            structureBrain.Data.MultipleFollowerIDs.Add(info.ID);
            tent.normal.gameObject.SetActive(value: false);
            tent.inprogress.gameObject.SetActive(value: true);
            tent.particles.gameObject.SetActive(value: true);
            ObjectiveManager.CompleteCustomObjective(Objectives.CustomQuestTypes.SendFollowerToMatingTent, info.ID);

            // Egg: vanilla builds it from two brains. The Lamb's is the inert stand-in.
            FollowerInfo playerInfo = PlayerStandIn.For(player);
            structureBrain.SetEggInfo(follower.Brain, PlayerStandIn.InertBrainFor(playerInfo), 1f);
            structureBrain.Data.MatingFailed = false;
            StructuresData.EggData egg = structureBrain.Data.EggInfo;
            // Child takes after the follower: give "parent 2" the follower's look as well.
            egg.Parent_2_SkinName = info.SkinName;
            egg.Parent_2_SkinColor = info.SkinColour;
            egg.Parent_2_SkinVariant = info.SkinVariation;

            yield return RunTraitSelector(info, playerInfo, egg);

            // Mating is over: the egg is ready, the tent is free again.
            structureBrain.SetEggReady();
            structureBrain.Data.MultipleFollowerIDs.Clear();
            DataManager.Instance.MatingCompletedTimestamp = -1f;

            // Only the follower is undressed (the Lamb has no such animation and stays as they were).
            FollowerBrain.SetFollowerCostume(follower.Spine.Skeleton, info.XPLevel, info.SkinName, info.SkinColour, FollowerOutfitType.Naked, info.Hat, info.Clothing, info.Customisation, FollowerSpecialType.None, info.Necklace, info.ClothingVariant, info);

            yield return ComeOut(tent, follower, player, egg);
        }

        /// <summary>
        /// Vanilla's Kiss command (interaction_FollowerInteraction.RomanceRoutine): the follower stands exactly
        /// 1 unit to the side of the Lamb at the same height, the Lamb faces them, then "kiss-follower" (Lamb)
        /// and "kiss" (follower) play together. The Lamb's animation is drawn for that exact spacing.
        /// </summary>
        private static IEnumerator Smooch(Follower follower, PlayerFarming player, bool zombie)
        {
            Vector3 target = player.transform.position + Vector3.left * ((follower.transform.position.x < player.transform.position.x) ? KissDistance : -KissDistance);
            bool arrived = false;
            follower.GoTo(target, () => arrived = true);
            float timeout = 6f;
            while (!arrived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            follower.transform.position = target; // GoTo stops "near"; the kiss needs the exact spot

            follower.FacePosition(player.transform.position);
            player.state.facingAngle = Utils.GetAngle(player.transform.position, follower.transform.position);
            player.state.CURRENT_STATE = StateMachine.State.CustomAnimation;
            follower.State.CURRENT_STATE = StateMachine.State.Dancing;
            yield return null;

            player.Spine.AnimationState.SetAnimation(0, "kiss-follower", loop: false);
            player.Spine.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
            follower.SetBodyAnimation("kiss", loop: true);
            follower.AddBodyAnimation("idle", loop: true, 0f);
            yield return new WaitForSeconds(5f / 6f);
            AudioManager.Instance.PlayOneShot("event:/followers/love_hearts", follower.transform.position);
            BiomeConstants.Instance.EmitHeartPickUpVFX(follower.transform.position, 0f, "red", "burst_big", useDeltaTime: false);
            yield return new WaitForSeconds(3f);

            if (zombie)
            {
                // Same as vanilla: kissing a zombie makes the Lamb gag.
                player.Spine.AnimationState.SetAnimation(0, "eat-react-bad", loop: false);
                player.Spine.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
                yield return new WaitForSeconds(7f / 30f);
                AudioManager.Instance.PlayOneShot("event:/dialogue/followers/hold_back_vom", follower.gameObject);
                yield return new WaitForSeconds(11f / 15f);
                AudioManager.Instance.PlayOneShot("event:/dialogue/followers/vom", follower.gameObject);
                yield return new WaitForSeconds(2f);
            }

            player.Spine.CustomMaterialOverride.Clear();
            follower.Spine.CustomMaterialOverride.Clear();
            follower.State.CURRENT_STATE = StateMachine.State.Idle;
        }

        private static IEnumerator RunTraitSelector(FollowerInfo info, FollowerInfo playerInfo, StructuresData.EggData egg)
        {
            // Same lists vanilla builds: the follower's heritable traits, nothing from the player, two random ones.
            var followerTraits = new List<FollowerTrait.TraitType>(info.Traits);
            for (int i = followerTraits.Count - 1; i >= 0; i--)
            {
                if (FollowerTrait.ExcludedFromMating.Contains(followerTraits[i])) followerTraits.RemoveAt(i);
            }
            var lambTraits = new List<FollowerTrait.TraitType>();

            var randomTraits = new List<FollowerTrait.TraitType>();
            for (int i = 0; i < 2; i++)
            {
                FollowerTrait.TraitType trait;
                int guard = 0;
                do
                {
                    trait = FollowerTrait.GetStartingTrait();
                    guard++;
                }
                while (guard < 100 && (randomTraits.Contains(trait)
                    || (FollowerTrait.ExclusiveTraits.ContainsKey(trait) && followerTraits.Contains(FollowerTrait.ExclusiveTraits[trait]))));
                randomTraits.Add(trait);
            }

            UITraitSelector traitSelector = MonoSingleton<UIManager>.Instance.TraitSelectorTemplate.Instantiate();
            traitSelector.Show();
            traitSelector.Configure(info, playerInfo, followerTraits, lambTraits, randomTraits, egg);
            traitSelector.TraitsChosen += traits => egg.Traits = traits;
            while (traitSelector != null)
            {
                yield return null;
            }
        }

        private static IEnumerator ComeOut(Interaction_MatingTent tent, Follower follower, PlayerFarming player, StructuresData.EggData egg)
        {
            bool zombie = follower.Brain.HasTrait(FollowerTrait.TraitType.Zombie);
            bool golden = egg.Golden;

            GameManager.GetInstance().OnConversationNew();
            GameManager.GetInstance().OnConversationNext(tent.gameObject, 6f);
            yield return new WaitForEndOfFrame();

            follower.Interaction_FollowerInteraction.enabled = false;
            follower.Brain.HardSwapToTask(new FollowerTask_ManualControl());
            follower.Spine.gameObject.SetActive(value: true);
            follower.transform.position = tent.sexyPos.transform.position;
            follower.HideAllFollowerIcons();
            DataManager.Instance.EggsProduced++;

            tent.normal.gameObject.SetActive(value: true);
            tent.inprogress.gameObject.SetActive(value: false);
            tent.transform.localScale = new Vector3(1.05f, 0.95f);
            tent.transform.DOScale(Vector3.one, 0.8f).SetEase(Ease.OutElastic, 0.35f, 0.3f);

            // The Lamb steps out first and waits where the second partner would stand.
            player.transform.position = tent.sexyPos.transform.position;
            player.gameObject.SetActive(value: true);
            player.GoToAndStop(tent.pos2.transform.position, follower.gameObject);

            follower.AddThought((Thought)UnityEngine.Random.Range(316, 320));

            if (zombie)
            {
                follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Moving, golden ? "Egg/zombie-walk-limp-egg-gold" : "Egg/zombie-walk-limp-egg");
                follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Idle, golden ? "Egg/zombie-egg-placedown-gold" : "Egg/zombie-egg-placedown");
            }
            else
            {
                follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Moving, golden ? "Egg/run-egg-gold" : "Egg/run-egg");
                follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Idle, golden ? "Egg/egg-placedown-gold" : "Egg/egg-placedown");
            }
            string placeDown = zombie
                ? (golden ? "Egg/zombie-egg-placedown-gold" : "Egg/zombie-egg-placedown")
                : (golden ? "Egg/egg-placedown-gold" : "Egg/egg-placedown");
            follower.GoTo(tent.pos1.transform.position, () =>
            {
                follower.FacePosition(player.transform.position);
                follower.SetBodyAnimation(placeDown, loop: false);
            });

            Skin skin = follower.Spine.Skeleton.Skin;
            string eggSkin = egg.Rotting ? "Eggs/Mutated" : (golden ? "Eggs/Gold" : "Eggs/Normal");
            skin.AddSkin(follower.Spine.Skeleton.Data.FindSkin(eggSkin));
            follower.Spine.Skeleton.SetSkin(skin);

            yield return new WaitForSeconds(0.75f);
            AudioManager.Instance.PlayOneShot("event:/Stings/mating_success");

            bool waiting = true;
            Spine.AnimationState.TrackEntryEventDelegate onPlant = null;
            onPlant = (TrackEntry trackEntry, Spine.Event e) =>
            {
                if (e.Data.Name == "plant")
                {
                    follower.Spine.AnimationState.Event -= onPlant;
                    Vector3 dir = (player.transform.position - follower.transform.position).normalized;
                    AccessTools.Method(typeof(Interaction_MatingTent), "SpawnEgg").Invoke(tent, new object[] { follower.transform.position + dir * 0.8f });
                    waiting = false;
                }
            };
            follower.Spine.AnimationState.Event += onPlant;
            float plantTimeout = 15f;
            while (waiting && plantTimeout > 0f)
            {
                plantTimeout -= Time.deltaTime;
                yield return null;
            }
            if (waiting)
            {
                // The plant event never fired: don't lose the egg.
                follower.Spine.AnimationState.Event -= onPlant;
                AccessTools.Method(typeof(Interaction_MatingTent), "SpawnEgg").Invoke(tent, new object[] { follower.transform.position + Vector3.right * 0.8f });
            }

            if (zombie)
            {
                follower.Brain.CurrentState.SetStateAnimations(follower, forced: true);
            }
            else
            {
                follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Moving, "run");
                follower.AddBodyAnimation("idle", loop: true, 0f);
            }
            yield return new WaitForSeconds(2.5f);
            follower.SimpleAnimator.ChangeStateAnimation(StateMachine.State.Idle, "idle");

            // Wrap up: follower gets dressed again and is worn out; the Lamb is untouched.
            follower.Brain.CompleteCurrentTask();
            follower.Interaction_FollowerInteraction.enabled = true;
            follower.OverridingEmotions = false;
            follower.Brain.MakeExhausted();
            ApplyAfterEffects(follower);
            FollowerBrain.SetFollowerCostume(follower.Spine.Skeleton, follower.Brain._directInfoAccess, hooded: false, forceUpdate: true);
            follower.ShowAllFollowerIcons();
            SimulationManager.UnPause();
            NotificationCentre.Instance.PlayGenericNotificationLocalizedParams("Notifications/MatingSuccessful", follower.Brain.Info.Name, PlayerStandIn.For(player).Name);
            GameManager.GetInstance().OnConversationEnd();
        }

        // Mirrors the per-follower consequences vanilla applies after mating (old age, illness).
        private static void ApplyAfterEffects(Follower follower)
        {
            Thought state = follower.Brain.Info.CursedState;
            if (state == Thought.OldAge)
            {
                if (UnityEngine.Random.value < 0.1f)
                {
                    follower.Brain.DiedOfOldAge = true;
                    follower.DieWithAnimation("die", "dead", playAnimation: true, 1, NotificationCentre.NotificationType.DiedFromOldAge);
                }
                else
                {
                    follower.Brain.MakeInjured();
                }
            }
            else if (state == Thought.Ill)
            {
                follower.Brain.HardSwapToTask(new FollowerTask_Vomit());
            }
        }

        private static string RandomConsiderAnim()
        {
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: return "Egg/mating-consider";
                case 1: return "Egg/mating-consider2";
                case 2: return "Egg/mating-consider3";
                default: return "Egg/mating-consider4";
            }
        }
    }
}
