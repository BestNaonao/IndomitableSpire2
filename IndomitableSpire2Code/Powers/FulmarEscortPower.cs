using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class FulmarEscortPower : IndomitablePower, IDurabilityLossModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    private sealed class Data
    {
        public readonly List<CardPlay> ActivePlays = [];    // 打出卡牌的递归栈
        public CardPlay? SelectedPlay;
        public int AmountAtStart;
    }
    
    private bool Eligible(CardModel card) => card.Owner.Creature == Owner &&
        card.IsAircraftType(IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber);
    
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        // 在这里只检验拥有者是否是能力持有者，而不检验卡牌是否是鱼雷轰炸机牌，因为可能在内层嵌套中包含新打出的鱼雷轰炸机牌，也可能改变舰载机类型。
        if (cardPlay.Card.Owner.Creature == Owner) GetInternalData<Data>().ActivePlays.Add(cardPlay);
        return Task.CompletedTask;
    }
    
    public override Task BeforeAttack(AttackCommand command)
    {
        var data = GetInternalData<Data>();
        if (data.SelectedPlay == null && command.Attacker == Owner && command.DamageProps.IsPoweredAttack() &&
            command.ModelSource is CardModel card && Eligible(card) && command.CardPlay is { } play)
        {
            data.SelectedPlay = play;
            data.AmountAtStart = Amount;
        }
        return Task.CompletedTask;
    }
    
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || !props.IsPoweredAttack() || cardSource == null || !Eligible(cardSource)) return 0M;
        var selected = GetInternalData<Data>().SelectedPlay;
        return selected == null || cardPlay == selected || cardPlay == null && selected.Card == cardSource ? Amount : 0M;
    }
    
    public bool TryModifyDurabilityLoss(CardModel card, int originalLoss, out int modifiedLoss)
    {
        modifiedLoss = originalLoss;
        if (!Eligible(card)) return false;
        var data = GetInternalData<Data>();
        if (data.SelectedPlay != null && (data.SelectedPlay.Card != card || data.ActivePlays.LastOrDefault() != data.SelectedPlay))
            return false;
        // 仅减半一次，层数只影响伤害；奇数损失减半后向上取整。此查询也用于预览，不改状态。
        modifiedLoss = (int)Math.Ceiling(originalLoss / 2M);
        return modifiedLoss != originalLoss;
    }
    
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var data = GetInternalData<Data>();
        data.ActivePlays.Remove(cardPlay);
        if (data.SelectedPlay != cardPlay) return;
        var consumed = data.AmountAtStart;
        data.SelectedPlay = null;
        // 参考活力、空袭引导等能力的处理。等耐久牌在 AfterCardPlayed 完成损耗后才消耗；无防空损耗也只加成这一张。
        await PowerCmd.ModifyAmount(choiceContext, this, -consumed, null, null);
    }
}