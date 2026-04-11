using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Registries;

public static class BlockRetentionRegistry
{
    private static readonly Dictionary<Type, IBlockRetentionProvider> Providers = new();

    /// <summary>
    /// 向系统中注册一个格挡保留提供者（主要用于原版模型的外部适配）
    /// </summary>
    public static void Register(Type modelType, IBlockRetentionProvider provider)
    {
        Providers[modelType] = provider;
    }

    /// <summary>
    /// 获取保留策略：优先检查模型自身是否实现接口，否则查询外部注册表
    /// </summary>
    public static bool TryGetProvider(AbstractModel model, out IBlockRetentionProvider? provider)
    {
        // 1. 外部适配接入：模型在注册表中绑定了外部 Provider（适用于官方内容）
        if (model is not IBlockRetentionProvider directProvider)
            return Providers.TryGetValue(model.GetType(), out provider);
        // 2. 原生接入：模型直接实现了 IBlockRetentionProvider 接口（适用于自定义 Mod 内容）
        provider = directProvider;
        return true;
    }
        
    // 留给后续补丁使用的配置项
    public const bool UseMaxForNonAggregates = true;
}