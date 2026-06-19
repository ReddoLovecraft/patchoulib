using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using Patchouib.Scrpits.Main;

namespace Patchouib.Scrpits.Patches
{
    [HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
    public static class HealthBarOverlayPowerPatch
    {
        private static MethodInfo? _getFgWidthMethod;
        private static MethodInfo? _getFgWidthMethodWithMaxWidth;
        private static FieldInfo? _creatureField;
        private static FieldInfo? _hpForegroundField;
        private static FieldInfo? _poisonForegroundField;
        private static FieldInfo? _doomForegroundField;
        private static PropertyInfo? _maxFgWidthProp;
        private static MethodInfo? _isPoisonLethalMethod;
        private static MethodInfo? _isDoomLethalMethod;

        [HarmonyPrefix]
        public static bool Prefix(NHealthBar __instance)
        {
            EnsureReflectedMembersCached();
            Creature creature = (Creature)_creatureField.GetValue(__instance)!;
            Control hpForeground = (Control)_hpForegroundField.GetValue(__instance)!;
            Control poisonForeground = (Control)_poisonForegroundField.GetValue(__instance)!;
            Control doomForeground = (Control)_doomForegroundField.GetValue(__instance)!;
            float maxFgWidth = (float)_maxFgWidthProp.GetValue(__instance)!;

            if (creature.CurrentHp <= 0)
            {
                poisonForeground.Visible = false;
                doomForeground.Visible = false;
                hpForeground.Visible = false;
                return false; 
            }

            hpForeground.Visible = true;
            float offsetRight = GetFgWidth(__instance, creature.CurrentHp) - maxFgWidth;
            hpForeground.OffsetRight = offsetRight;

            if (creature.HpDisplay.IsInfinite())
            {
                var invincibleColorField = AccessTools.Field(typeof(NHealthBar), "_invincibleForegroundColor");
                Color invincibleColor = (Color)invincibleColorField.GetValue(__instance)!;
                hpForeground.SelfModulate = invincibleColor;
                return false;
            }
            //原有中毒灾厄逻辑
            int powerAmount = creature.GetPowerAmount<DoomPower>();
            int poisonDamage = creature.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;

            HandlePoison(__instance, creature, poisonForeground, hpForeground, offsetRight, maxFgWidth, poisonDamage);
            HandleDoom(__instance, creature, doomForeground, hpForeground, poisonDamage, powerAmount, maxFgWidth);
            var customPowers = creature.Powers
                .Where(p => p is IHealthBarOverlayPower)
                .Cast<IHealthBarOverlayPower>()
                .ToList();

            if (customPowers.Count > 0)
            {
                var customPower = customPowers.First();
                ApplyCustomPower(__instance, creature, customPower, poisonForeground, hpForeground, offsetRight, maxFgWidth);
            }

            return false; 
        }

        private static void EnsureReflectedMembersCached()
        {
            if (_creatureField == null)
                _creatureField = AccessTools.Field(typeof(NHealthBar), "_creature");
            if (_hpForegroundField == null)
                _hpForegroundField = AccessTools.Field(typeof(NHealthBar), "_hpForeground");
            if (_poisonForegroundField == null)
                _poisonForegroundField = AccessTools.Field(typeof(NHealthBar), "_poisonForeground");
            if (_doomForegroundField == null)
                _doomForegroundField = AccessTools.Field(typeof(NHealthBar), "_doomForeground");
            if (_maxFgWidthProp == null)
                _maxFgWidthProp = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
            if (_getFgWidthMethod == null)
                _getFgWidthMethod = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", new[] { typeof(int) });
            if (_getFgWidthMethodWithMaxWidth == null)
                _getFgWidthMethodWithMaxWidth = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", new[] { typeof(int), typeof(float) });
            if (_isPoisonLethalMethod == null)
                _isPoisonLethalMethod = AccessTools.Method(typeof(NHealthBar), "IsPoisonLethal");
            if (_isDoomLethalMethod == null)
                _isDoomLethalMethod = AccessTools.Method(typeof(NHealthBar), "IsDoomLethal");
        }

        private static float GetFgWidth(NHealthBar instance, int amount)
        {
            return (float)_getFgWidthMethod.Invoke(instance, new object[] { amount })!;
        }

        private static float GetFgWidth(NHealthBar instance, int amount, float maxWidth)
        {
            return (float)_getFgWidthMethodWithMaxWidth.Invoke(instance, new object[] { amount, maxWidth })!;
        }

        private static bool IsPoisonLethal(NHealthBar instance, int damage)
        {
            return (bool)_isPoisonLethalMethod.Invoke(instance, new object[] { damage })!;
        }

        private static bool IsDoomLethal(NHealthBar instance, int doomAmount, int poisonDamage)
        {
            return (bool)_isDoomLethalMethod.Invoke(instance, new object[] { doomAmount, poisonDamage })!;
        }

        private static void HandlePoison(NHealthBar instance, Creature creature, Control poisonForeground, Control hpForeground, float offsetRight, float maxFgWidth, int poisonDamage)
        {
            if (creature.HasPower<PoisonPower>())
            {
                if (poisonDamage > 0)
                {
                    poisonForeground.Visible = true;
                    if (IsPoisonLethal(instance, poisonDamage))
                    {
                        poisonForeground.OffsetLeft = 0f;
                        poisonForeground.OffsetRight = offsetRight;
                        hpForeground.Visible = false;
                    }
                    else
                    {
                        float fgWidth = GetFgWidth(instance, creature.CurrentHp - poisonDamage);
                        hpForeground.OffsetRight = fgWidth - maxFgWidth;
                        hpForeground.Visible = true;
                        int patchMarginLeft = ((NinePatchRect)poisonForeground).PatchMarginLeft;
                        poisonForeground.OffsetLeft = Math.Max(0f, fgWidth - (float)patchMarginLeft);
                        poisonForeground.OffsetRight = offsetRight;
                    }
                }
                else
                {
                    poisonForeground.Visible = false;
                }
            }
            else
            {
                poisonForeground.Visible = false;
                poisonForeground.OffsetLeft = 0f;
            }
        }

        private static void HandleDoom(NHealthBar instance, Creature creature, Control doomForeground, Control hpForeground, int poisonDamage, int powerAmount, float maxFgWidth)
        {
            if (creature.HasPower<DoomPower>())
            {
                if (powerAmount > 0)
                {
                    doomForeground.Visible = true;
                    float num2 = GetFgWidth(instance, powerAmount) - maxFgWidth;
                    if (IsDoomLethal(instance, powerAmount, poisonDamage))
                    {
                        if (!IsPoisonLethal(instance, poisonDamage))
                        {
                            doomForeground.OffsetRight = hpForeground.OffsetRight;
                            hpForeground.Visible = false;
                        }
                        else
                        {
                            hpForeground.Visible = false;
                            doomForeground.Visible = false;
                        }
                    }
                    else
                    {
                        int patchMarginRight = ((NinePatchRect)doomForeground).PatchMarginRight;
                        doomForeground.OffsetRight = Math.Min(0f, num2 + (float)patchMarginRight);
                        hpForeground.Visible = true;
                    }
                }
                else
                {
                    doomForeground.Visible = false;
                }
            }
            else
            {
                doomForeground.Visible = false;
            }
        }

        private static void ApplyCustomPower(NHealthBar instance, Creature creature, IHealthBarOverlayPower customPower, Control foregroundControl, Control hpForeground, float offsetRight, float maxFgWidth)
        {
            int overlayValue = customPower.GetHealthBarOverlayValue(creature);
            Color color = customPower.GetHealthBarOverlayColor();
            bool fromEnd = customPower.IsOverlayFromEnd();
            bool isLethal = customPower.IsOverlayLethal(creature);
            foregroundControl.SelfModulate = color;
            foregroundControl.Visible = true;

            if (isLethal)
            {
                foregroundControl.OffsetLeft = 0f;
                foregroundControl.OffsetRight = offsetRight;
                hpForeground.Visible = false;
            }
            else
            {
                if (fromEnd)
                {
                    float fgWidth = GetFgWidth(instance, creature.CurrentHp - overlayValue);
                    hpForeground.OffsetRight = fgWidth - maxFgWidth;
                    hpForeground.Visible = true;
                    int patchMarginLeft = ((NinePatchRect)foregroundControl).PatchMarginLeft;
                    foregroundControl.OffsetLeft = Math.Max(0f, fgWidth - (float)patchMarginLeft);
                    foregroundControl.OffsetRight = offsetRight;
                }
                else
                {
                    float fgWidth = GetFgWidth(instance, overlayValue);
                    foregroundControl.OffsetLeft = 0f;
                    foregroundControl.OffsetRight = fgWidth - maxFgWidth;
                }
            }
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._EnterTree))]
    public static class VisiblePowerCreatureInitPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NCreature __instance)
        {
            VisiblePowerAnchor anchor = VisiblePowerAnchor.GetOrCreate(__instance);
            anchor.SyncToCurrentPowers();
        }
    }

    [HarmonyPatch(typeof(NCreature), "OnPowerApplied")]
    public static class VisiblePowerAppliedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NCreature __instance, PowerModel power)
        {
            if (power is not IVisiblePower visiblePower)
            {
                return;
            }

            VisiblePowerAnchor.GetOrCreate(__instance).TryAdd(power, visiblePower.TscnPath);
        }
    }

    [HarmonyPatch(typeof(NCreature), "OnPowerRemoved")]
    public static class VisiblePowerRemovedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NCreature __instance, PowerModel power)
        {
            VisiblePowerAnchor anchor = VisiblePowerAnchor.TryGet(__instance);
            anchor?.TryRemove(power);
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._ExitTree))]
    public static class VisiblePowerCreatureCleanupPatch
    {
        [HarmonyPrefix]
        private static void Prefix(NCreature __instance)
        {
            VisiblePowerAnchor anchor = VisiblePowerAnchor.TryGet(__instance);
            anchor?.QueueFreeSafely();
            VisiblePowerAnchor.Remove(__instance);
        }
    }

    public partial class VisiblePowerAnchor : Node2D
    {
        private const string _metaKey = "Patchoulib_VisiblePowerAnchor";
        private const float _headOffsetY = 75f;
        private const float _radius = 160f;
        private const float _maxAngleDegrees = 80f;
        private const float _stepDegrees = 80f;

        private static readonly ConditionalWeakTable<NCreature, VisiblePowerAnchor> _anchors = new ConditionalWeakTable<NCreature, VisiblePowerAnchor>();

        private readonly NCreature _creatureNode;
        private readonly Dictionary<PowerModel, Node2D> _wrapperByPower = new Dictionary<PowerModel, Node2D>();
        private readonly List<PowerModel> _powerOrder = new List<PowerModel>();
        private bool _layoutDirty;

        private VisiblePowerAnchor(NCreature creatureNode)
        {
            _creatureNode = creatureNode;
            Name = "Patchoulib_VisiblePowerAnchor";
            _layoutDirty = true;
        }

        public static VisiblePowerAnchor GetOrCreate(NCreature creatureNode)
        {
            if (_anchors.TryGetValue(creatureNode, out VisiblePowerAnchor? anchor))
            {
                return anchor;
            }

            anchor = new VisiblePowerAnchor(creatureNode);
            creatureNode.SetMeta(_metaKey, anchor);
            creatureNode.AddChildSafely(anchor);
            if (creatureNode.GetChildCount() > 1)
            {
                creatureNode.MoveChild(anchor, 1);
            }
            _anchors.Add(creatureNode, anchor);
            return anchor;
        }

        public static VisiblePowerAnchor? TryGet(NCreature creatureNode)
        {
            if (_anchors.TryGetValue(creatureNode, out VisiblePowerAnchor? anchor))
            {
                return anchor;
            }
            return null;
        }

        public static void Remove(NCreature creatureNode)
        {
            _anchors.Remove(creatureNode);
            if (creatureNode.HasMeta(_metaKey))
            {
                creatureNode.RemoveMeta(_metaKey);
            }
        }

        public override void _Process(double delta)
        {
            if (!GodotObject.IsInstanceValid(_creatureNode))
            {
                this.QueueFreeSafely();
                return;
            }

            UpdateAnchorPosition();

            if (_layoutDirty)
            {
                _layoutDirty = false;
                UpdateLayout();
            }
        }

        public void SyncToCurrentPowers()
        {
            if (!GodotObject.IsInstanceValid(_creatureNode))
            {
                return;
            }

            HashSet<PowerModel> current = _creatureNode.Entity.Powers.ToHashSet();
            foreach (PowerModel existing in _powerOrder.ToList())
            {
                if (!current.Contains(existing))
                {
                    TryRemove(existing);
                }
            }

            foreach (PowerModel power in _creatureNode.Entity.Powers)
            {
                if (power is IVisiblePower visiblePower)
                {
                    TryAdd(power, visiblePower.TscnPath);
                }
            }
        }

        public void TryAdd(PowerModel power, string tscnPath)
        {
            if (_wrapperByPower.ContainsKey(power))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tscnPath))
            {
                return;
            }

            PackedScene? packed = ResourceLoader.Load<PackedScene>(tscnPath);
            if (packed == null)
            {
                return;
            }

            Node instance = packed.Instantiate();
            MakeNonBlocking(instance);

            Node2D wrapper = new Node2D
            {
                Name = $"VisiblePower_{power.GetType().Name}"
            };
            wrapper.AddChildSafely(instance);
            this.AddChildSafely(wrapper);

            _wrapperByPower.Add(power, wrapper);
            _powerOrder.Add(power);
            _layoutDirty = true;
        }

        public void TryRemove(PowerModel power)
        {
            if (!_wrapperByPower.TryGetValue(power, out Node2D? wrapper))
            {
                return;
            }

            _wrapperByPower.Remove(power);
            _powerOrder.Remove(power);
            wrapper.QueueFreeSafely();
            _layoutDirty = true;
        }

        private void UpdateAnchorPosition()
        {
            Control hitbox = _creatureNode.Hitbox;
            Vector2 head = hitbox.Position + new Vector2(hitbox.Size.X / 2f, 0f);
            Position = head + new Vector2(0f, _headOffsetY);
        }

        private void UpdateLayout()
        {
            int count = _powerOrder.Count;
            if (count == 0)
            {
                return;
            }

            int maxSideIndex = Mathf.CeilToInt((float)(count - 1) / 2f);
            float maxAngleUsed = Mathf.Min(_maxAngleDegrees, maxSideIndex * _stepDegrees);
            float stepDegreesUsed = (maxSideIndex <= 0) ? 0f : maxAngleUsed / maxSideIndex;
            float stepRad = Mathf.DegToRad(stepDegreesUsed);
            for (int i = 0; i < count; i++)
            {
                PowerModel power = _powerOrder[i];
                if (!_wrapperByPower.TryGetValue(power, out Node2D? wrapper))
                {
                    continue;
                }

                int sideIndex;
                if (i == 0)
                {
                    sideIndex = 0;
                }
                else if (i % 2 == 1)
                {
                    sideIndex = (i + 1) / 2;
                }
                else
                {
                    sideIndex = -(i / 2);
                }

                float angle = sideIndex * stepRad;
                Vector2 pos = new Vector2(Mathf.Sin(angle) * _radius, -Mathf.Cos(angle) * _radius);
                wrapper.Position = pos;
            }
        }

        private static void MakeNonBlocking(Node node)
        {
            if (node is Control control)
            {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            }

            if (node is CollisionObject2D collision)
            {
                collision.InputPickable = false;
            }

            foreach (Node child in node.GetChildren())
            {
                MakeNonBlocking(child);
            }
        }
    }
}
