using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public class Emergency() : TashkentCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MarkDynamicVar(2M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!base.Keywords.Contains(CardKeyword.Exhaust) && !base.ExhaustOnNextPlay)
        {
            await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Top);
        }
    }
    
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);
        
        var enemies = base.Owner.Creature.CombatState?.HittableEnemies;
        if (enemies != null && enemies.Any())
        {
            Creature target = base.Owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
            await PowerCmd.Apply<MarkPower>(target, DynamicVars["TashkentSpire2-Mark"].BaseValue,base.Owner.Creature,this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(1M);
    }
}