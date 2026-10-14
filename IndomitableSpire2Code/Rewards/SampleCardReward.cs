using IndomitableSpire2.IndomitableSpire2Code.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Rewards;

/// <summary>
/// 指定类型的一张试吃卡牌，仍然使用 CardReward 的选牌、跳过、历史记录和联机选择同步。读档时随普通奖励重新生成。
/// 原版指定卡牌构造器不支持直接序列化，不能把本类当成可保存的战斗中途额外奖励使用。
/// </summary>
public sealed class SampleCardReward<TCard>(Player player) : CardReward([CreateCard(player)], CardCreationSource.Other,
    player, new CardCreationOptions([], CardCreationSource.Other, CardRarityOddsType.Uniform))
    where TCard : CardModel
{
    private static CardModel CreateCard(Player player)
    {
        var card = player.RunState.CreateCard<TCard>(player);
        CardCmd.Enchant<SampleEnchantment>(card, 1);
        return card;
    }
    
    public override LocString Description
    {
        get
        {
            var description = new LocString("gameplay_ui", "INDOMITABLESPIRE2-SAMPLE_CARD_REWARD");
            description.Add("Card", ModelDb.Card<TCard>().Title);
            return description;
        }
    }
    
    // Reward.HoverTips 是所有奖励共用的扩展点；普通 CardReward 也能显示卡牌与附魔提示。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        Cards.SelectMany(card => new[] { HoverTipFactory.FromCard(card) }.Concat(card.HoverTips));
    
    // 构造器已指定唯一的附魔卡牌；禁止普通奖励数量/卡池钩子再把它变成随机多选一。
    public override void Populate() { }
    
    protected override Task<bool> OnSelect()
    {
        // 浮木的 late 钩子会为所有 CardReward 开启重抽。本奖励是确定的赠品，在打开界面前关闭重抽，保留原版跳过和其他替代奖励选项。
        CanReroll = false;
        return base.OnSelect();
    }
}