using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class WavePiercingDaggerPower : TashkentPower
{
    private sealed class Data
    {
        public readonly Dictionary<CardPlay, bool> CardPlays = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/WavePiercingDaggerPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/WavePiercingDaggerPower.png";
    
    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }

        var data = GetInternalData<Data>();
        if (data.CardPlays.Count < base.Amount)
        {
            data.CardPlays.TryAdd(cardPlay, false);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != base.Owner || command.TargetSide == base.Owner.Side || !command.DamageProps.IsPoweredAttack()
            || command.CardPlay is not { } cardPlay || command.ModelSource != cardPlay.Card)
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (!data.CardPlays.ContainsKey(cardPlay))
        {
            return;
        }

        data.CardPlays[cardPlay] = true;
        decimal totalDamage = command.Results.SelectMany((List<DamageResult> r) => r)
            .Sum((DamageResult r) => (decimal)r.TotalDamage + r.OverkillDamage);

        if (totalDamage > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, totalDamage, ValueProp.Move, null);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (GetInternalData<Data>().CardPlays.Remove(cardPlay, out bool attacked) && attacked && base.Amount > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -1M, null, null);
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        GetInternalData<Data>().CardPlays.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        GetInternalData<Data>().CardPlays.Clear();
        return Task.CompletedTask;
    }
}
