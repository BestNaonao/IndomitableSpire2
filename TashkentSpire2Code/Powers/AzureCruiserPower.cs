using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class AzureCruiserPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/AzureCruiserPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/AzureCruiserPower.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TorpedoPower>()];
    
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(0M)
    ];
    
    public AzureCruiserPower SetTorpedoPower(decimal amount)
    {
        AssertMutable();
        base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue = amount;
        InvokeDisplayAmountChanged();
        return this;
    }
    
    public override int DisplayAmount => base.DynamicVars["TashkentSpire2-Torpedo"].IntValue;
    
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (base.Owner.Player != null && card.Owner.Creature == base.Owner)
        {
            Flash();
            for (int i = 0; i < this.Amount; i++)
            {
                await PowerCmd.Apply<TorpedoPower>(choiceContext, base.Owner.Player.Creature, base.DynamicVars["TashkentSpire2-Torpedo"].IntValue, base.Owner.Player.Creature, null);
            }
        }
        var torpedoes = base.Owner.Powers
            .OfType<TorpedoPower>()
            .ToList();

        foreach (var power in torpedoes)
        {
            power.ReduceTurnCount(this.Amount);
        }
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}