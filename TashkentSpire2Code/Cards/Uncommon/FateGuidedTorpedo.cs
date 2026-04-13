using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public class FateGuidedTorpedo() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7M, ValueProp.Move),
        new TorpedoDynamicVar(12M),
        new EnergyVar(0)
    ];
    
    protected override bool IsPlayable => (Owner?.Creature?.GetPowerAmount<DistancePower>() ?? 0) <= -2;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var temp = new TorpedoPower();
        int value = temp.ComputeTurns();
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, value, base.Owner.Creature, this))?.SetDamage(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, value, base.Owner.Creature, this))?.SetDamage(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        
        var num = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (num < DynamicVars.Block.BaseValue)
        {
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(1M, ValueProp.Unpowered), cardPlay);
        }
        
        if (base.IsUpgraded)
        {
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1M);
    }
}