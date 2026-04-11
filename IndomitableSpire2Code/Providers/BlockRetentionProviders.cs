using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Providers;

/// <summary>
/// 壁垒 (Barricade) 保留策略：全量保留，无额外特效
/// </summary>
public class BarricadeProvider : IBlockRetentionProvider
{
    public bool ShouldAggregate => false;

    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature)
    {
        return creature.Block; // 保留当前所有的格挡
    }

    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        // 原版中没有闪烁特效
    }
}

/// <summary>
/// 残影 (Blur) 保留策略：全量保留，触发时闪烁
/// </summary>
public class BlurProvider : IBlockRetentionProvider
{
    public bool ShouldAggregate => false;

    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature)
    {
        return creature.Block; 
    }

    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        if (sourceModel is BlurPower)
            sourceModel.AfterPreventingBlockClear(sourceModel, creature);
    }
}

/// <summary>
/// 遁地 (Burrowed) 保留策略：全量保留，无额外特效
/// </summary>
public class BurrowedProvider : IBlockRetentionProvider
{
    public bool ShouldAggregate => false;

    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature)
    {
        return creature.Block;
    }

    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        // 破甲和移除的逻辑在它原本的重写方法里，这里仅处理保留表现
    }
}

/// <summary>
/// 坚固夹钳 (Sturdy Clamp) 保留策略：定额保留10点，触发时闪烁
/// </summary>
public class SturdyClampProvider : IBlockRetentionProvider
{
    public bool ShouldAggregate => false;

    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature)
    {
        MainFile.Logger.Info("Sturdy Clamp Block Retention Calculated");
        return 10; // 核心逻辑：提供 10 点的基础保留值
    }

    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        if (sourceModel is SturdyClamp sturdyClamp)
            sturdyClamp.Flash();
    }
}