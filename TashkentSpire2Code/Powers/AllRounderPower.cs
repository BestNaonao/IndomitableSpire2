using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Patches;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class AllRounderPower : TashkentClickPower, IHasSecondAmount
{
    private const string ChargeKey = "Charges";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomBigIconPath =>
        "res://TashkentSpire2/images/powers/big/AllRounderPower.png";

    public override string CustomPackedIconPath =>
        "res://TashkentSpire2/images/powers/packed/AllRounderPower.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ChargeKey, 0m)
    ];

    public string GetSecondAmount()
    {
        return DynamicVars[ChargeKey].IntValue.ToString();
    }

    protected override async Task OnLeftClickSynchronized(PlayerChoiceContext choiceContext, ClickContext clickContext)
    {
        int charges = (int)DynamicVars[ChargeKey].BaseValue;

        if (charges <= 0)
            return;

        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);

        charges--;
        DynamicVars[ChargeKey].BaseValue = charges;

        InvokeDisplayAmountChanged();
    }

    public void AddCharge(int value)
    {
        DynamicVars[ChargeKey].BaseValue += value;
        InvokeDisplayAmountChanged();
    }
}