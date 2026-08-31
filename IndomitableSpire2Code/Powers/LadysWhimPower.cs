using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class LadysWhimPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<MotivationPower>()
    ];
    
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner.Player) return;
        // After 钩子调用前当前牌已经写入历史；按代码类寻找本回合自己的首次完成记录。
        // 比较 CardPlay 而非卡牌实例，也能区分重放及嵌套打出的同类牌。
        var firstPlay = CombatManager.Instance.History.Entries.OfType<CardPlayFinishedEntry>()
            .FirstOrDefault(entry => entry.HappenedThisTurn(CombatState) && 
                entry.CardPlay.Player == cardPlay.Player && entry.CardPlay.Card.GetType() == cardPlay.Card.GetType());
        if (firstPlay?.CardPlay != cardPlay) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: true);
        await PowerCmd.Apply<MotivationPower>(choiceContext, Owner, Amount, Owner, null);
    }
}