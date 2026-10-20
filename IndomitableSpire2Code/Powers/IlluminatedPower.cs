using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class IlluminatedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override object InitInternalData() => new Data();
    
    private sealed class Data
    {
        // 按卡牌实例隔离不同玩家；栈区分火冠等同一个实例在结算期间的嵌套打出。
        public readonly Dictionary<CardModel, Stack<PlayData>> ActivePlays = [];
        public int BenefitedPlayCount;
    }
    
    private sealed class PlayData(CardPlay? play)
    {
        public CardPlay? Play { get; } = play;
        public bool Benefited;
    }
    
    private Stack<PlayData> GetActivePlays(CardModel card)
    {
        var plays = GetInternalData<Data>().ActivePlays;
        if (!plays.TryGetValue(card, out var stack)) plays[card] = stack = new Stack<PlayData>();
        return stack;
    }
    
    // 在舰载机牌打出之前，先将潜在的受益对象计入字典
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.IsCarrierAircraft()) GetActivePlays(cardPlay.Card).Push(new PlayData(cardPlay));
        return Task.CompletedTask;
    }
    
    // 攻击拥有者的舰载机牌的攻击伤害获得加成，原版逻辑会根据返回值通知自己执行 AfterModifyingDamageAmount 方法。
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        Amount > 0 && target == Owner && props.IsPoweredAttack() && cardSource?.IsCarrierAircraft() == true
            ? Amount : 0M;
    
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        // 引擎仅在真实结算、且本能力确实提供了正数加成时通知。预览调用上面的纯计算不记数。
        if (cardSource == null) return Task.CompletedTask;
        var data = GetInternalData<Data>();
        if (!data.ActivePlays.TryGetValue(cardSource, out var plays))
        {
            // 照明可能在这次打出开始后才被施加：只记录后续真正获得加成的攻击，不追溯先前攻击。
            if (cardSource.Pile?.Type != PileType.Play) return Task.CompletedTask;
            plays = GetActivePlays(cardSource);
            plays.Push(new PlayData(null));
        }
        plays.Peek().Benefited = true;
        return Task.CompletedTask;
    }
    
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var data = GetInternalData<Data>();
        // 没有该卡牌的记录，或者有记录但是栈顶的这次打出不是触发钩子的这次打出。
        if (!data.ActivePlays.TryGetValue(cardPlay.Card, out var plays) ||
            plays.Peek().Play is { } startedPlay && startedPlay != cardPlay)
            return Task.CompletedTask;
        // 每个 CardPlay 单独结算：多段只计一次，重放、临终追加和再次打出分别计数。
        if (plays.Pop().Benefited) data.BenefitedPlayCount++;
        if (plays.Count == 0) data.ActivePlays.Remove(cardPlay.Card);
        return Task.CompletedTask;
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return;
        var data = GetInternalData<Data>();
        var count = data.BenefitedPlayCount;
        data.BenefitedPlayCount = 0;
        if (count > 0) await PowerCmd.ModifyAmount(choiceContext, this, -count, null, null);
    }
}