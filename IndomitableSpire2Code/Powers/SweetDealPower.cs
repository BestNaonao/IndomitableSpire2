using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class SweetDealPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced; // 允许存在多份独立契约
    public override PowerStackType StackType => PowerStackType.Single;
    
    protected override object InitInternalData() => new Data();
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new StringVar("CardName"), new StringVar("TargetName")];
    
    // 智能切分文本：借出阶段和归还阶段显示不同文本
    protected override string SmartDescriptionLocKey => 
        GetInternalData<Data>().IsLending ? $"{Id.Entry}.smartDescriptionLend" : $"{Id.Entry}.smartDescriptionReturn";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        GetInternalData<Data>().LeasedCard is { } c ? [HoverTipFactory.FromCard(c)] : [];
    
    public void SetContract(CardModel card, Player targetPlayer, bool isLending)
    {
        var data = GetInternalData<Data>();
        data.LeasedCard = card;
        data.TargetPlayer = targetPlayer;
        data.IsLending = isLending;
        ((StringVar)DynamicVars["CardName"]).StringValue = card.Title;
        ((StringVar)DynamicVars["TargetName"]).StringValue = PlatformUtil.GetPlayerName(
            RunManager.Instance.NetService.Platform, targetPlayer.NetId);
    }
    
    // 核心机制：修改打出后的去向。引擎随后会自动触发完美的动画与跨玩家移交。
    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
    {
        var data = GetInternalData<Data>();
        if (card != data.LeasedCard || data.TargetPlayer is not { Creature.IsAlive: true } || Owner.Player != card.Owner) 
            return cardLocation;
        Flash();
        data.IsReturned = true;
        return data.IsLending
            // 借出阶段：无视消耗、能力牌的原始落点，强制丢入对方手牌
            // TODO: 等官方修复BUG后把抽牌堆顶改为手牌底
            ? new CardLocation(data.TargetPlayer, PileType.Draw, CardPilePosition.Top)
            // 归还阶段：保留卡牌原本该去的 Pile（比如消耗的去消耗堆），但将所属人改为原主人
            : new CardLocation(data.TargetPlayer, cardLocation.pileType, cardLocation.position);
    }
    
    // 任务完成后，移除能力本身
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var data = GetInternalData<Data>();
        if (data.IsReturned) await PowerCmd.Remove(this);
    }
    
    private class Data
    {
        public CardModel? LeasedCard;
        public Player? TargetPlayer;
        public bool IsLending;
        public bool IsReturned;
    }
}