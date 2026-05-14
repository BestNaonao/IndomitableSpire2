using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Cards.Rare;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class CoreBreakdownPower : TashkentTemporaryPower<StrengthPower>
{
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/core_breakdown_power.png";
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/core_breakdown_power.png";

    public override AbstractModel OriginModel => ModelDb.Card<CoreBreakdown>();
}