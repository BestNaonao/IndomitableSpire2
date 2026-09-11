using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class RetreatPreparation() : TashkentCard(-1, CardType.Status, CardRarity.Status, TargetType.None), KnowledgeDemon.IChoosable
{
    public override bool CanBeGeneratedInCombat => false;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new RetreatDynamicVar(2M)
    ];

    public async Task OnChosen()
    {
        await PowerCmd.Apply<DistancePower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, -base.DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Retreat"].UpgradeValueBy(1M);
    }
}
