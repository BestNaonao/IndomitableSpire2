using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 格挡保留策略提供者接口
/// </summary>
public interface IBlockRetentionProvider
{
    /// <summary>
    /// 是否与其他提供者聚合（即进行算术相加）
    /// 例如：如果未来有遗物提供“额外保留5点”，这个值应为 true。
    /// 如果是类似“残影”或“坚固夹钳”这种设定一个基准线的，通常为 false。
    /// </summary>
    bool ShouldAggregate { get; }

    /// <summary>
    /// 计算该提供者试图保留的格挡数值
    /// </summary>
    /// <param name="sourceModel">提供保留效果的对象（如 BlurPower, SturdyClamp 的实例）</param>
    /// <param name="creature">要保留格挡的生物</param>
    /// <returns>保留的具体数值（例如残影返回 creature.Block，夹钳返回 10）</returns>
    int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature);

    /// <summary>
    /// 当保留行为最终生效时触发的视觉/额外效果（替代原先硬编码的 Flash）
    /// </summary>
    Task OnRetentionTriggered(AbstractModel sourceModel, Creature creature);
}