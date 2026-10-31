using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Rewards;

public static class AllyRewardPreviewRules
{
    /// <summary>校验接收者是否合法</summary>
    public static bool IsEligibleRecipient(CardModel source, Player recipient) =>
        source is { IsMutable: true, CombatState: not null } && recipient != source.Owner &&
        recipient.Creature.IsAlive && recipient.RunState == source.Owner.RunState &&
        recipient.Creature.CombatState == source.CombatState && recipient.Creature.Side == source.Owner.Creature.Side;
    
    /// <summary>UI 的通用过滤；卡牌自己的完成进度、首次下单等条件由接口实现负责。</summary>
    public static AllyRewardPreview[] Collect(CardModel source, AllyRewardPreviewContext context)
    {
        if (source is not IAllyRewardPreview provider || !source.IsMutable || source.CombatState == null) return [];
        return provider.GetAllyRewardPreviews(context)
            .Where(reward => reward.Amount > 0 && !string.IsNullOrWhiteSpace(reward.IconPath) &&
                             IsEligibleRecipient(source, reward.Recipient))
            .GroupBy(reward => (reward.Recipient, reward.IconPath)) // 相同玩家和相同 Icon 会被归为一组
            .Select(group =>
                new AllyRewardPreview(group.Key.Recipient, group.Key.IconPath, group.Sum(reward => reward.Amount)))
            .ToArray();
    }
}