using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Providers.DotProviders;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Modding;

namespace IndomitableSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string
        ModId = "IndomitableSpire2"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // ========== 注册持续伤害提供者 ==========
        var registry = DamageOverTimeRegistry.Instance;
        
        // 注册中毒、起火，未来可以轻松添加更多
        registry.Register(new PoisonDotProvider());
        registry.Register(new OnFireDotProvider());
        
        Logger.Info("Dot providers registered successfully");
        
        Harmony harmony = new(ModId);

        harmony.PatchAll();
        
        Logger.Info("Indomitable mod loaded");
    }
}