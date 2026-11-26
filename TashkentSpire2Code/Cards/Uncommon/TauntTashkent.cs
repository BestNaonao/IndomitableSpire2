using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using BaseLib.Utils.Attributes;
using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

[CustomID("TASHKENTSPIRE2-TAUNT")]
[SuppressMessage("Localization", "STS001", Justification = "CustomID preserves the existing localization keys.")]
public sealed class TauntTashkent() : TashkentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("TauntPower", 1m),
        new ChargeDynamicVar(0M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<TauntPower>(choiceContext, base.Owner.Creature, base.DynamicVars["TauntPower"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<TauntEndTurnPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
