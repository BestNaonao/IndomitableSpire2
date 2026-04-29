using Godot;
using Godot.Bridge;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Providers;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace IndomitableSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "IndomitableSpire2";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        InitializeBlockRetentionProviders();
        Logger.Info("Block Retention Providers registered successfully");
        
        Harmony harmony = new(ModId);

        harmony.PatchAll();
        
        // 使得场景文件可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(MainFile).Assembly);
        
        Logger.Info("Indomitable mod loaded");
    }
    
    /// <summary>
    /// 在 Mod 加载、应用 Harmony 补丁前调用此方法
    /// </summary>
    private static void InitializeBlockRetentionProviders()
    {
        // 注册官方的格挡保留模型
        BlockRetentionRegistry.Register(typeof(BarricadePower), new BarricadeProvider());
        BlockRetentionRegistry.Register(typeof(BlurPower), new BlurProvider());
        BlockRetentionRegistry.Register(typeof(BurrowedPower), new BurrowedProvider());
        BlockRetentionRegistry.Register(typeof(SturdyClamp), new SturdyClampProvider());
    }
}