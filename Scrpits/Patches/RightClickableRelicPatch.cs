using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using Patchouib.Scrpits.Main;

namespace SilkSong.Scrpits.Patches
{
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
    public static class RightClickableRelicPatch
    {
        private const string _connectedMetaKey = "Patchoulib_RightClickConnected";

        private static readonly object _invokerLock = new object();
        private static readonly Dictionary<Type, Func<object, PlayerChoiceContext, Task>?> _invokersByType = new Dictionary<Type, Func<object, PlayerChoiceContext, Task>?>();

        private static void Postfix(NRelicInventoryHolder __instance)
        {
            TryConnect(__instance);
        }

        [HarmonyPatch(typeof(NRelicBasicHolder), nameof(NRelicBasicHolder._Ready))]
        [HarmonyPostfix]
        private static void NRelicBasicHolderReadyPostfix(NRelicBasicHolder __instance)
        {
            TryConnect(__instance);
        }

        private static void TryConnect(NClickableControl clickable)
        {
            if (clickable == null || clickable.HasMeta(_connectedMetaKey))
            {
                return;
            }

            clickable.SetMeta(_connectedMetaKey, true);
            clickable.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(e => HandleMouseReleased(clickable, e)));
        }

        private static void HandleMouseReleased(NClickableControl clickable, InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton mouseButton)
            {
                return;
            }

            if (mouseButton.ButtonIndex != MouseButton.Right || mouseButton.Pressed)
            {
                return;
            }

            RelicModel? model = clickable switch
            {
                NRelicInventoryHolder inv => inv.Relic?.Model,
                NRelicBasicHolder basic => basic.Relic?.Model,
                _ => null
            };

            if (model == null)
            {
                return;
            }

            InvokeOnRightClick(model);
        }

        private static void InvokeOnRightClick(RelicModel model)
        {
            PlayerChoiceContext context = new BlockingPlayerChoiceContext();
            context.PushModel(model);

            Task? task = null;
            try
            {
                if (TryInvokeViaReflection(model, context, out Task reflectedTask))
                {
                    task = reflectedTask;
                }
                else if (model is IRightCilckable cilckableRelic)
                {
                    task = cilckableRelic.OnRightClick(context);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            if (task == null)
            {
                context.PopModel(model);
                return;
            }

            _ = task.ContinueWith(t =>
            {
                try
                {
                    context.PopModel(model);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }

                if (t.IsFaulted && t.Exception != null)
                {
                    Log.Error(t.Exception.ToString());
                }
            }, TaskScheduler.Default);
        }

        private static bool TryInvokeViaReflection(RelicModel model, PlayerChoiceContext context, out Task task)
        {
            task = Task.CompletedTask;
            Func<object, PlayerChoiceContext, Task>? invoker = GetOrCreateInvoker(model.GetType());
            if (invoker == null)
            {
                return false;
            }
            task = invoker(model, context);
            return true;
        }

        private static Func<object, PlayerChoiceContext, Task>? GetOrCreateInvoker(Type type)
        {
            lock (_invokerLock)
            {
                if (_invokersByType.TryGetValue(type, out Func<object, PlayerChoiceContext, Task>? cached))
                {
                    return cached;
                }

                MethodInfo? method = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                        m.Name == "OnRightClick" &&
                        m.ReturnType == typeof(Task) &&
                        m.GetParameters().Length == 1 &&
                        typeof(PlayerChoiceContext).IsAssignableFrom(m.GetParameters()[0].ParameterType));

                if (method == null)
                {
                    _invokersByType[type] = null;
                    return null;
                }

                Func<object, PlayerChoiceContext, Task> invoker = (obj, ctx) => (Task)method.Invoke(obj, new object[] { ctx })!;
                _invokersByType[type] = invoker;
                return invoker;
            }
        }
    }

    [HarmonyPatch(typeof(NCardHolder), "ConnectSignals")]
    public static class RightClickableCardPatch
    {
        private const string _connectedMetaKey = "Patchoulib_CardRightClickConnected";

        private static void Postfix(NCardHolder __instance)
        {
            TryConnect(__instance);
        }

        private static void TryConnect(NCardHolder holder)
        {
            NClickableControl hitbox = holder.Hitbox;
            if (hitbox == null || hitbox.HasMeta(_connectedMetaKey))
            {
                return;
            }

            hitbox.SetMeta(_connectedMetaKey, true);
            hitbox.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(e => HandleMouseReleased(holder, e)));
        }

        private static void HandleMouseReleased(NCardHolder holder, InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton mouseButton)
            {
                return;
            }

            if (mouseButton.ButtonIndex != MouseButton.Right || mouseButton.Pressed)
            {
                return;
            }

            CardModel? model = holder.CardModel;
            if (model is not IRightClickableCardModel clickableModel)
            {
                return;
            }

            if (!IsRightClickAllowed(model, clickableModel))
            {
                return;
            }

            InvokeOnRightClick(model, clickableModel);
        }

        private static bool IsRightClickAllowed(CardModel model, IRightClickableCardModel clickableModel)
        {
            if (clickableModel.IsCombat && !CombatManager.Instance.IsInProgress)
            {
                return false;
            }

            List<PileType> piles = clickableModel.Pile;
            if (piles == null || piles.Count == 0)
            {
                return true;
            }

            PileType currentPile = model.Pile?.Type ?? PileType.None;
            return piles.Contains(currentPile);
        }

        private static void InvokeOnRightClick(CardModel model, IRightClickableCardModel clickableModel)
        {
            PlayerChoiceContext context = new BlockingPlayerChoiceContext();
            context.PushModel(model);

            Task? task = null;
            try
            {
                task = clickableModel.OnRightClick(context);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            if (task == null)
            {
                context.PopModel(model);
                return;
            }

            _ = task.ContinueWith(t =>
            {
                try
                {
                    context.PopModel(model);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }

                if (t.IsFaulted && t.Exception != null)
                {
                    Log.Error(t.Exception.ToString());
                }
            }, TaskScheduler.Default);
        }
    }

}

