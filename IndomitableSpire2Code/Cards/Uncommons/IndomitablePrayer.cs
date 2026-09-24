using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class IndomitablePrayer() : IndomitableCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // ShieldVar 自带护盾提示；不添加本卡同名能力的提示。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ShieldVar(8M, ValueProp.Move)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CustomCreatureCmd.GainShield(choiceContext, Owner.Creature, DynamicVars.Shield(), cardPlay);
        await PowerCmd.Apply<IndomitablePrayerPower>(choiceContext, Owner.Creature, 1M, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Shield().UpgradeValueBy(4M);
    }
}