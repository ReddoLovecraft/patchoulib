using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
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

            if (creature.ShowsInfiniteHp)
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
}
