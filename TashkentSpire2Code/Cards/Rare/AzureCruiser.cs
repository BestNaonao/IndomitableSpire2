using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class AzureCruiser() : TashkentCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var powers = base.Owner.Creature.Powers
            .Where(p => p.Type is PowerType.Buff)
            .ToList();

        int sum = 0;
        foreach (var power in powers)
        {
            await PowerCmd.Remove(power);
            sum++;
        }
        
        await PlayerCmd.GainEnergy(sum, base.Owner);
        await CardPileCmd.Draw(choiceContext, sum, base.Owner);
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, sum, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade(){
        base.EnergyCost.UpgradeBy(-1);
    }
}