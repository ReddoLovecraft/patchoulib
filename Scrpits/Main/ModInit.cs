using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Patchouib.Scripts.Main
{
	[ModInitializer("Init")]
	public class ModInit
	{
		
		private static Harmony? _harmony;
		public static void Init()
		{
			_harmony = new Harmony("Patchoulib");
			_harmony.PatchAll();
			Log.Debug("Lib has been loaded successfully");
		}
	}
}
