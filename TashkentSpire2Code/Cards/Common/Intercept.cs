using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Basics;

public class Intercept() : TashkentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8M, ValueProp.Move),
        new TorpedoDynamicVar(18M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var num = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (num < DynamicVars.Block.BaseValue)
        {
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(1M, ValueProp.Unpowered), cardPlay);
        }
        var temp = new TorpedoPower();
        int value = temp.ComputeTurns();
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, value, base.Owner.Creature, this))?.SetDamage(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2M);
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(6M);
    }
}