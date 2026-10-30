using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 卡牌声明本次打出会给予队友的奖励。仅用于本地拖拽/手柄预览，会重复查询；
/// 实现必须只读，不施加奖励、不消耗一次性标记、不调用随机数或触发战斗钩子。
/// 自定义目标条件可读取 context.Target；尚未指向角色时 Target 为 null。
/// </summary>
public interface IAllyRewardPreview
{
    IEnumerable<AllyRewardPreview> GetAllyRewardPreviews(AllyRewardPreviewContext context);
}

/// <summary>
/// 承载由 Controller 提取的“当前鼠标悬停目标”信息。Target 指明了这张卡正指着谁。
/// </summary>
public readonly record struct AllyRewardPreviewContext(Creature? Target);

/// <summary>
/// 一项奖励。IconPath 可指定原版或模组的高分辨率 Texture2D（png/tres），
/// 同一收件人的多个条目排在同一行，共用一个加号；Amount &gt; 1 时显示右下角数字。
/// </summary>
public readonly record struct AllyRewardPreview(Player Recipient, string IconPath, decimal Amount = 1M);