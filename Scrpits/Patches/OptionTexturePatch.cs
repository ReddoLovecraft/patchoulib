using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Patchoulib.Scrpits.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patchoulib.Scrpits.Patches
{
    [HarmonyPatch(typeof(RestSiteOption))]
    [HarmonyPatch(nameof(RestSiteOption.Icon), MethodType.Getter)]
    public static class OptionTexturePatch
    {

        [HarmonyPrefix]
        static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
        {
            if (__instance is CustomOption)
            {
                CustomOption __custom = __instance as CustomOption;
                if(__custom.CustomTexture!=null)
                __result = __custom.CustomTexture;
                return false;
            }
            return true;
        }
    }
}
