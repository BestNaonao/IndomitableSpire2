using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Status;

[Pool(typeof(StatusCardPool))]
public sealed class Shell6() : TashkentCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    public override bool CanBeGeneratedInCombat => false;
}