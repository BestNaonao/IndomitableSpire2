using MegaCrit.Sts2.Core.Entities.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Status;

public sealed class Shell() : TashkentCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    public override bool CanBeGeneratedInCombat => false;
}