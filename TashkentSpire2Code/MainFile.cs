using BaseLib.Config;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TashkentSpire2.TashkentSpire2Code.Config;

namespace TashkentSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	public const string
		ModId = "TashkentSpire2"; //At the moment, this is used only for the Logger and harmony names.

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
		new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
		
	public static void Initialize()
	{
		Harmony harmony = new(ModId);

		ScriptManagerBridge.LookupScriptsInAssembly(typeof(MainFile).Assembly);
		
		ModConfigRegistry.Register(ModId, new TashkentConfig());
		
		harmony.PatchAll();
		
		Logger.Info("Tashkent mod loaded");
	}
}
