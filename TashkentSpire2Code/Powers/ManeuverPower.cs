using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class ManeuverPower : TashkentTemporaryPower<StrengthPower>
{
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/ManeuverPower.png";
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/ManeuverPower.png";

    public override AbstractModel OriginModel => ModelDb.Card<Maneuver>();
}