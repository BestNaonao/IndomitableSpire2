using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class DefendTerritorialWatersPower : TashkentPower
{
    private class Data
    {
        public int totalDistanceGained;
        public int totalTriggers;
    }
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DistancePower>()];
    
    public override int DisplayAmount
    {
        get
        {
            if (Amount <= 0) return 0;
            Data data = GetInternalData<Data>();
            int remainder = data.totalDistanceGained % Amount;
            return remainder == 0 ? Amount : Amount - remainder;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Progress", 0m),
        new EnergyVar(1)
    ];
    
    protected override object InitInternalData() => new Data();
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public async Task OnDistanceChanged(int delta)
    {
        if (delta <= 0 || Amount <= 0) return;

        Data data = GetInternalData<Data>();
        data.totalDistanceGained += delta;

        int newTriggers = data.totalDistanceGained / Amount;

        if (newTriggers > data.totalTriggers)
        {
            int diff = newTriggers - data.totalTriggers;
            Flash();
            await PlayerCmd.GainEnergy(diff, Owner.Player!);
            data.totalTriggers = newTriggers;
        }

        int remainder = data.totalDistanceGained % Amount;
        DynamicVars["Progress"].BaseValue = remainder;
        InvokeDisplayAmountChanged();
    }
}