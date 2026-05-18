using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    
    public override bool IsInstanced => true;
    
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
                await PowerCmd.Apply<TorpedoPower>(base.Owner.Player.Creature, base.DynamicVars["TashkentSpire2-Torpedo"].IntValue, base.Owner.Player.Creature, null);
            }
        }
    }
    
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}