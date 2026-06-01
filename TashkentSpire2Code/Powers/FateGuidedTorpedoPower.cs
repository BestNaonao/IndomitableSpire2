using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class FateGuidedTorpedoPower : TashkentPower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/FateGuidedTorpedoPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/FateGuidedTorpedoPower.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TorpedoPower>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(0M)
    ];
    
    public FateGuidedTorpedoPower SetTorpedoPower(decimal amount)
    {
        AssertMutable();
        base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue = amount;
        InvokeDisplayAmountChanged();
        return this;
    }
    
    public string GetSecondAmount()
    {
        return DynamicVars["TashkentSpire2-Torpedo"].BaseValue.ToString();
    }
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Side)
        {
            for (int i = 0; i < base.Amount; i++)
            {
                await PowerCmd.Apply<TorpedoPower>(base.Owner, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner, null);
            }
        }
    }
}