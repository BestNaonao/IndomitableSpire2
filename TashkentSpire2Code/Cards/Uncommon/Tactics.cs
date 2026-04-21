using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class Tactics() : TashkentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(9M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int turns = TorpedoPower.ComputeTurns(base.Owner.Creature);
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, (decimal)turns, base.Owner.Creature, this))
            ?.SetDamage(DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        
        var powers = base.Owner.Creature.Powers
            .Where(p => p is TorpedoPower || p is TheBombPower)
            .ToList();

        foreach (var power in powers)
        {
            int current = (int)power.Amount;
            int target = Math.Max(1, current - 1);
            int delta = target - current;

            if (delta != 0)
            {
                await PowerCmd.ModifyAmount(power, delta, base.Owner.Creature, this);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(3M);
    }
}