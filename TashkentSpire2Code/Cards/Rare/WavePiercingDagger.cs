using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class WavePiercingDagger() : TashkentCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VigorPower>(50M)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<VigorPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal de = (decimal)this.Owner.Creature.Block * DynamicVars["VigorPower"].BaseValue / 100M;
        await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, de, base.Owner.Creature, this);
        await PowerCmd.Apply<WavePiercingDaggerPower>(choiceContext, base.Owner.Creature, 1M, base.Owner.Creature, this);
        this.Owner.Creature.LoseBlockInternal(this.Owner.Creature.Block);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(25M);
    }
}