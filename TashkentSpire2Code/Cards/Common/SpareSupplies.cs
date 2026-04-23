using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class SpareSupplies() : AmmunitionCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AmmunitionDynamicVar(1M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(3M),
        new EnergyVar(1),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Vodka>()];
    
    protected override async Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu)
    {
        if (this.CanonicalKeywords.Contains(TashkentKeyword.Barrage))
        {
            await PlayerCmd.GainEnergy(ammu, base.Owner);
            UpdateAmmuGlobal(0);
        }
        else
        {
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
            UpdateAmmuGlobal(ammu - 1);
        }
    }
    
    protected override async Task OnPlayWithoutAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        List<Vodka> list = Vodka.Create(base.Owner, base.DynamicVars.Cards.IntValue, base.CombatState).ToList();
        foreach (Vodka item in list)
        {
            await CardPileCmd.AddGeneratedCardToCombat(item, PileType.Hand, addedByPlayer: true);
        }
        
        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);
    }
    
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}