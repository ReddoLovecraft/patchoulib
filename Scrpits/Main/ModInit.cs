using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using System.Runtime.CompilerServices;

namespace Patchouib.Scripts.Main
{
	[ModInitializer("Init")]
	public class ModInit
	{
		
		private static Harmony? _harmony;
		private static bool _initialized;

		[ModuleInitializer]
		public static void ModuleInit()
		{
			Log.Info("[Patchoulib] ModuleInit hit");
			Init();
		}

		public static void Init()
		{
			if (_initialized)
			{
				Log.Info("[Patchoulib] Init skipped (already initialized)");
				return;
			}
			_initialized = true;

			Log.Info("[Patchoulib] Init begin, patching...");
			_harmony = new Harmony("Patchoulib");
			_harmony.PatchAll();
			Log.Info("[Patchoulib] Init done (PatchAll finished)");
		}
	}
}
