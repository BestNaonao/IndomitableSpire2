using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TashkentSpire2.TashkentSpire2Code.Keywords;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class BreakThroughOnAllFronts() : TashkentCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private LocString BreakThroughOnAllFronts_Dialogue => new("cards", $"{Id.Entry}.banter");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BreakThroughOnAllFrontsPower>(2M)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(TashkentKeyword.Barrage)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.PlayerCombatState is null) return;
        
        TalkCmd.Play(BreakThroughOnAllFronts_Dialogue, Owner.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        var ammuCards = Owner.Piles
            .SelectMany(p => p.Cards)
            .Where(c => c is AmmunitionCard)
            .ToList();
        
        foreach (var original in ammuCards)
        {
            original.AddKeyword(TashkentKeyword.Barrage);
        }
        
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BreakThroughOnAllFrontsPower>(base.Owner.Creature, base.DynamicVars["BreakThroughOnAllFrontsPower"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}