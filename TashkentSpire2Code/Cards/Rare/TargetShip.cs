using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Monsters;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class TargetShip() : TashkentCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MarkDynamicVar(6M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = base.Owner.Creature.CombatState;
        var encounter = combat.Encounter;
        Log.Info("0");
        if (encounter == null || encounter.Slots == null || encounter.Slots.Count == 0)
            return;
        Log.Info("1");
        string nextSlot = encounter.Slots
            .LastOrDefault(s => combat.Enemies.All(c => c.SlotName != s), string.Empty);
        Log.Info("2");
        if (string.IsNullOrEmpty(nextSlot))
            return;
        Log.Info("3");
        await Cmd.Wait(0.1f);
        Log.Info("4");
        await CreatureCmd.Add<TwoTailedRat>(combat, nextSlot);
        Log.Info("5");
        await Cmd.Wait(0.1f);
        Log.Info("6");
        var summoned = combat.Enemies
            .FirstOrDefault(c => c.SlotName == nextSlot);
        Log.Info("7");
        if (summoned == null)
            return;
        Log.Info("8");
        await PowerCmd.Apply<MarkPower>(
            summoned,
            DynamicVars["TashkentSpire2-Mark"].BaseValue,
            base.Owner.Creature,
            this
        );
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(3M);
    }
}