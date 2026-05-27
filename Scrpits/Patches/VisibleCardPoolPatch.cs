using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using Patchouib.Scrpits.Main;

namespace Patchouib.Scrpits.Patches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCardPools), MethodType.Getter)]
    public static class VisibleCardPoolModelDbPatch
    {
        private static readonly Type _cardPoolBaseType = typeof(CardPoolModel);
        private static readonly Type _visibleCardPoolInterfaceType = typeof(IVisibleCardPool);

        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            try
            {
                HashSet<ModelId> existing = __result.Select(p => p.Id).ToHashSet();
                List<CardPoolModel> extraPools = new List<CardPoolModel>();

                foreach (Type type in ModelDb.AllAbstractModelSubtypes)
                {
                    if (type == null || !type.IsSubclassOf(_cardPoolBaseType))
                    {
                        continue;
                    }
                    if (!_visibleCardPoolInterfaceType.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    try
                    {
                        ModelId id = ModelDb.GetId(type);
                        CardPoolModel pool = ModelDb.GetById<CardPoolModel>(id);
                        if (existing.Add(pool.Id))
                        {
                            extraPools.Add(pool);
                        }
                    }
                    catch
                    {
                    }
                }

                if (extraPools.Count > 0)
                {
                    __result = __result.Concat(extraPools);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Patchoulib][IVisibleCardPool] Failed to extend ModelDb.AllCardPools: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
    public static class VisibleCardPoolCardLibraryPatch
    {
        private const string _toggleScenePath = "res://scenes/screens/card_library/library_pool_toggle.tscn";
        private const string _injectedMetaKey = "Patchoulib_VisibleCardPools_Injected";
        private const string _poolIdMetaKey = "Patchoulib_VisibleCardPool_Id";

        private static readonly Type _visibleCardPoolInterfaceType = typeof(IVisibleCardPool);
        private static readonly HashSet<Type> _builtInPoolTypes = new HashSet<Type>
        {
            typeof(IroncladCardPool),
            typeof(SilentCardPool),
            typeof(DefectCardPool),
            typeof(RegentCardPool),
            typeof(NecrobinderCardPool),
            typeof(ColorlessCardPool)
        };

        private static readonly Lazy<PackedScene?> _toggleScene = new Lazy<PackedScene?>(() =>
        {
            try
            {
                return GD.Load<PackedScene>(_toggleScenePath);
            }
            catch
            {
                return null;
            }
        });

        private static readonly Lazy<AccessTools.FieldRef<NCardLibrary, Dictionary<NCardPoolFilter, Func<CardModel, bool>>>> _poolFiltersField =
            new Lazy<AccessTools.FieldRef<NCardLibrary, Dictionary<NCardPoolFilter, Func<CardModel, bool>>>>(() =>
                AccessTools.FieldRefAccess<NCardLibrary, Dictionary<NCardPoolFilter, Func<CardModel, bool>>>("_poolFilters"));

        private static readonly Lazy<Action<NCardLibrary, NCardPoolFilter>> _updateCardPoolFilterInvoker =
            new Lazy<Action<NCardLibrary, NCardPoolFilter>>(() =>
            {
                var method = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter", new[] { typeof(NCardPoolFilter) });
                return (library, filter) =>
                {
                    try
                    {
                        method?.Invoke(library, new object[] { filter });
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Patchoulib][IVisibleCardPool] Failed to invoke UpdateCardPoolFilter: {e}");
                    }
                };
            });

        public static void Postfix(NCardLibrary __instance)
        {
            try
            {
                if (__instance.HasMeta(_injectedMetaKey))
                {
                    return;
                }
                __instance.SetMeta(_injectedMetaKey, true);

                NCardPoolFilter? anchor = __instance.GetNodeOrNull<NCardPoolFilter>("%IroncladPool")
                    ?? __instance.GetNodeOrNull<NCardPoolFilter>("%ColorlessPool")
                    ?? __instance.GetNodeOrNull<NCardPoolFilter>("%MiscPool");
                if (anchor == null)
                {
                    return;
                }

                Node container = anchor.GetParent();
                if (container == null || !GodotObject.IsInstanceValid(container))
                {
                    return;
                }

                PackedScene? toggleScene = _toggleScene.Value;
                if (toggleScene == null)
                {
                    return;
                }

                Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters = _poolFiltersField.Value(__instance);
                HashSet<string> alreadyAddedPoolIds = container.GetChildren()
                    .OfType<Node>()
                    .Where(n => n.HasMeta(_poolIdMetaKey))
                    .Select(n =>
                    {
                        try
                        {
                            Variant v = n.GetMeta(_poolIdMetaKey);
                            if (v.VariantType == Variant.Type.Nil)
                            {
                                return null;
                            }
                            return v.AsString();
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!)
                    .ToHashSet();

                foreach (CardPoolModel pool in EnumerateVisibleCardPools())
                {
                    if (pool == null)
                    {
                        continue;
                    }
                    Type poolType = pool.GetType();
                    if (_builtInPoolTypes.Contains(poolType))
                    {
                        continue;
                    }
                    if (alreadyAddedPoolIds.Contains(pool.Id.ToString()))
                    {
                        continue;
                    }

                    string iconPath;
                    try
                    {
                        iconPath = ((IVisibleCardPool)pool).GetCardLibraryIconPath();
                    }
                    catch
                    {
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(iconPath))
                    {
                        continue;
                    }

                    Texture2D? iconTexture = null;
                    try
                    {
                        iconTexture = GD.Load<Texture2D>(iconPath);
                    }
                    catch
                    {
                    }
                    if (iconTexture == null)
                    {
                        continue;
                    }

                    Node toggleNode = toggleScene.Instantiate();
                    if (toggleNode is not NCardPoolFilter filterButton)
                    {
                        toggleNode.QueueFree();
                        continue;
                    }

                    filterButton.Name = $"VisiblePool_{pool.Id.Entry}";
                    filterButton.SetMeta(_poolIdMetaKey, pool.Id.ToString());
                    container.AddChild(filterButton);

                    TextureRect? image = filterButton.GetNodeOrNull<TextureRect>("Image");
                    TextureRect? shadow = filterButton.GetNodeOrNull<TextureRect>("Image/Shadow");
                    if (image != null)
                    {
                        image.Texture = iconTexture;
                    }
                    if (shadow != null)
                    {
                        shadow.Texture = iconTexture;
                    }

                    filterButton.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(f => _updateCardPoolFilterInvoker.Value(__instance, f)));
                    poolFilters[filterButton] = (CardModel c) => c.Pool != null && c.Pool.Id == pool.Id;

                    alreadyAddedPoolIds.Add(pool.Id.ToString());
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Patchoulib][IVisibleCardPool] Failed to inject pool filters into card library: {e}");
            }
        }

        private static IEnumerable<CardPoolModel> EnumerateVisibleCardPools()
        {
            foreach (Type type in ModelDb.AllAbstractModelSubtypes)
            {
                if (type == null || !type.IsSubclassOf(typeof(CardPoolModel)))
                {
                    continue;
                }
                if (!_visibleCardPoolInterfaceType.IsAssignableFrom(type))
                {
                    continue;
                }

                CardPoolModel? pool = null;
                try
                {
                    ModelId id = ModelDb.GetId(type);
                    pool = ModelDb.GetById<CardPoolModel>(id);
                }
                catch
                {
                }

                if (pool != null)
                {
                    yield return pool;
                }
            }
        }
    }
}
