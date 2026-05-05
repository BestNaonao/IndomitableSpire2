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
        public int triggerCount;
    }

    private const int DistanceThreshold = 3;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<DistancePower>()];

    public override int DisplayAmount
    {
        get
        {
            Data data = GetInternalData<Data>();
            return DistanceThreshold - (data.totalDistanceGained % DistanceThreshold);
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
        if (delta <= 0) return;

        Data data = GetInternalData<Data>();
        data.totalDistanceGained += delta;

        int triggers = data.totalDistanceGained / DistanceThreshold - data.triggerCount;

        if (triggers > 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(Amount * triggers, Owner.Player!);
            data.triggerCount += triggers;
        }

        int remainder = data.totalDistanceGained % DistanceThreshold;
        DynamicVars["Progress"].BaseValue = remainder;

        InvokeDisplayAmountChanged();
    }
}