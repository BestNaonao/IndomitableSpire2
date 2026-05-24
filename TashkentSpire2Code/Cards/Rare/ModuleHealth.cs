using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Orb;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class ModuleHealth() : TashkentCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Orbs", 1m)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<ModuleOrb>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await OrbCmd.Channel<ModuleOrb>(choiceContext, cardPlay.Card.Owner);
        if (base.IsUpgraded)
        {
            await OrbCmd.Channel<ModuleOrb>(choiceContext, cardPlay.Card.Owner);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.DynamicVars["Orbs"].UpgradeValueBy(1M);
    }
}