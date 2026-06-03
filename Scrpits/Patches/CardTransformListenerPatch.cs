using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using Patchouib.Scrpits.Main;

namespace Patchouib.Scrpits.Patches
{
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Transform), new Type[] { typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle) })]
    public static class CardTransformListenerPatch
    {
        private const string _logPrefix = "[Patchoulib][ITransformListener]";
        private static int _debugBudget = 200;

        private static void Debug(string message)
        {
            if (_debugBudget <= 0)
            {
                return;
            }
            _debugBudget--;
            Log.Info($"{_logPrefix} {message}");
        }

        [HarmonyPostfix]
        public static void Postfix(IEnumerable<CardTransformation> transformations, Rng? rng, CardPreviewStyle style, ref Task<IEnumerable<CardPileAddResult>> __result)
        {
            CardTransformation[] transformationsArr = transformations as CardTransformation[] ?? transformations.ToArray();
            Task<IEnumerable<CardPileAddResult>> originalTask = __result;
            Debug($"Postfix hit. transformations={transformationsArr.Length} rngNull={rng == null} style={style} netId={LocalContext.NetId?.ToString() ?? "null"}");
            __result = Wrap(originalTask, transformationsArr);
        }

        private static async Task<IEnumerable<CardPileAddResult>> Wrap(Task<IEnumerable<CardPileAddResult>> originalTask, CardTransformation[] transformationsArr)
        {
            Debug($"Wrap begin. transformations={transformationsArr.Length}");
            IEnumerable<CardPileAddResult> enumerable = await originalTask;
            List<CardPileAddResult> results = enumerable as List<CardPileAddResult> ?? enumerable.ToList();
            Debug($"Wrap after await. results={results.Count}");

            try
            {
                await NotifyListeners(transformationsArr, results);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            return results;
        }

        private static async Task NotifyListeners(CardTransformation[] transformationsArr, List<CardPileAddResult> results)
        {
            int count = Math.Min(transformationsArr.Length, results.Count);
            Debug($"Notify begin. pairCount={count} transformations={transformationsArr.Length} results={results.Count}");
            for (int i = 0; i < count; i++)
            {
                CardPileAddResult result = results[i];
                if (!result.success)
                {
                    Debug($"Skip[{i}] result.success=false");
                    continue;
                }

                CardModel transformedCard = transformationsArr[i].Original;
                CardModel resultCard = result.cardAdded;
                if (resultCard?.Pile == null)
                {
                    Debug($"Skip[{i}] resultCard or resultCard.Pile is null. transformed={transformedCard?.Id.Entry ?? "null"}");
                    continue;
                }

                PileType pileType = resultCard.Pile.Type;
                if (pileType == PileType.Deck || !pileType.IsCombatPile())
                {
                    Debug($"Skip[{i}] pileType not combat. pileType={pileType}");
                    continue;
                }

                if (pileType != PileType.Draw &&
                    pileType != PileType.Hand &&
                    pileType != PileType.Play &&
                    pileType != PileType.Discard &&
                    pileType != PileType.Exhaust)
                {
                    Debug($"Skip[{i}] pileType filtered. pileType={pileType}");
                    continue;
                }

                if (LocalContext.NetId.HasValue && !LocalContext.IsMine(resultCard))
                {
                    Debug($"Skip[{i}] not mine. pileType={pileType} ownerNetId={resultCard.Owner?.NetId.ToString() ?? "null"} localNetId={LocalContext.NetId?.ToString() ?? "null"}");
                    continue;
                }

                CombatState? combatState = resultCard.CombatState ?? transformedCard.CombatState ?? resultCard.Owner.Creature.CombatState;
                if (combatState == null)
                {
                    Debug($"Skip[{i}] combatState null. pileType={pileType}");
                    continue;
                }

                BlockingPlayerChoiceContext choiceContext = new BlockingPlayerChoiceContext();
                Player owner = resultCard.Owner;
                int listenerCount = 0;
                int invokeCount = 0;
                IEnumerable<AbstractModel> models = IteratePotentialListeners(owner);
                foreach (AbstractModel model in models)
                {
                    if (model is not ITransformListener listener)
                    {
                        continue;
                    }
                    listenerCount++;

                    choiceContext.PushModel(model);
                    try
                    {
                        int costBefore = resultCard.EnergyCost.GetWithModifiers(CostModifiers.All);
                        int baseBefore = resultCard.EnergyCost.GetWithModifiers(CostModifiers.None);
                        bool hasLocalBefore = resultCard.EnergyCost.HasLocalModifiers;
                        Debug($"Invoke[{i}] listener={model.GetType().FullName} transformed={transformedCard.Id.Entry} result={resultCard.Id.Entry} pile={pileType} activeHooks={owner.IsActiveForHooks} cost={costBefore} base={baseBefore} localMods={hasLocalBefore}");
                        await listener.AfterCardTransformed(choiceContext, transformedCard, resultCard, owner.Creature);
                        int costAfter = resultCard.EnergyCost.GetWithModifiers(CostModifiers.All);
                        int baseAfter = resultCard.EnergyCost.GetWithModifiers(CostModifiers.None);
                        bool hasLocalAfter = resultCard.EnergyCost.HasLocalModifiers;
                        Debug($"Invoke[{i}] done listener={model.GetType().FullName} cost={costAfter} base={baseAfter} localMods={hasLocalAfter}");
                        if (costAfter != costBefore || baseAfter != baseBefore || hasLocalAfter != hasLocalBefore)
                        {
                            resultCard.InvokeEnergyCostChanged();
                            Debug($"Invoke[{i}] fired EnergyCostChanged");
                        }
                        model.InvokeExecutionFinished();
                        invokeCount++;
                    }
                    catch (Exception e)
                    {
                        Debug($"Invoke[{i}] exception in listener={model.GetType().FullName}: {e.GetType().Name}");
                        Log.Error(e.ToString());
                    }
                    finally
                    {
                        choiceContext.PopModel(model);
                    }
                }
                Debug($"Done[{i}] listeners={listenerCount} invoked={invokeCount} transformed={transformedCard.Id.Entry} result={resultCard.Id.Entry} pile={pileType}");
            }
        }

        private static IEnumerable<AbstractModel> IteratePotentialListeners(Player player)
        {
            List<AbstractModel> list = new List<AbstractModel>(128);
            HashSet<AbstractModel> seen = new HashSet<AbstractModel>();

            void Add(AbstractModel? model)
            {
                if (model == null)
                {
                    return;
                }
                if (seen.Add(model))
                {
                    list.Add(model);
                }
            }

            foreach (PowerModel power in player.Creature.Powers)
            {
                Add(power);
            }

            foreach (RelicModel relic in player.Relics)
            {
                if (!relic.IsMelted)
                {
                    Add(relic);
                }
            }

            foreach (PotionModel potion in player.Potions)
            {
                Add(potion);
            }

            PlayerCombatState? pcs = player.PlayerCombatState;
            if (pcs != null)
            {
                foreach (OrbModel orb in pcs.OrbQueue.Orbs)
                {
                    Add(orb);
                }

                foreach (CardPile pile in pcs.AllPiles)
                {
                    foreach (CardModel card in pile.Cards)
                    {
                        Add(card);
                        Add(card.Affliction);
                        Add(card.Enchantment);
                    }
                }
            }

            Debug($"IteratePotentialListeners. playerNetId={player.NetId} activeHooks={player.IsActiveForHooks} models={list.Count} powers={player.Creature.Powers.Count} relics={player.Relics.Count} potions={player.Potions.Count()} piles={pcs?.AllPiles.Count ?? 0}");
            return list;
        }
    }
}
