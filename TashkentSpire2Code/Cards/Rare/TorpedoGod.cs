using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class TorpedoGod() : TashkentCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M),
        new PowerVar<TorpedoGodPower>(1M),
        new RepeatVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<TorpedoGodPower>(base.Owner.Creature, DynamicVars["TorpedoGodPower"].BaseValue, base.Owner.Creature, this);
        
        foreach (var p in base.Owner.Creature.Powers.OfType<TorpedoPower>())
        {
            p.SyncAOEFlag();
        }
        
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1M);
}