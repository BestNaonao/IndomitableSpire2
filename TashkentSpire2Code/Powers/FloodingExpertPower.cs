using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Extensions;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class FloodingExpertPower : TashkentPower, IAfterTorpedoDamage
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TorpedoPower>()];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/flooding_expert_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/flooding_expert_power.png";
    
    public async Task AfterTorpedoDamage(TorpedoDamageContext context)
    {
        int floodingAmount = (int)Amount;

        if (floodingAmount <= 0)
            return;

        foreach (var target in context.Targets)
        {
            await PowerCmd.Apply<WeakPower>(target, floodingAmount, context.Source, null);
            await PowerCmd.Apply<StrengthPower>(target, -floodingAmount, context.Source, null);
        }
    }
}