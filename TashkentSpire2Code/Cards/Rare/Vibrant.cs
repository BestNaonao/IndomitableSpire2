using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public class Vibrant() : TashkentCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        decimal damage = default(decimal);
        
        CardSelectorPrefs cardSelectorPrefs = new CardSelectorPrefs(base.SelectionScreenPrompt, DynamicVars.Cards.IntValue);
        CardModel? card = (await CardSelectCmd.FromHand(choiceContext, base.Owner, cardSelectorPrefs, (CardModel c) => c.Type == CardType.Attack, this)).FirstOrDefault();
        if (card != null)
        {
            if (card.DynamicVars.ContainsKey("CalculatedDamage"))
            {
                damage = card.DynamicVars.CalculatedDamage.Calculate(null);
            }
            else if (card.DynamicVars.ContainsKey("Damage"))
            {
                damage = card.DynamicVars.Damage.BaseValue;
            }
            else if (card.DynamicVars.ContainsKey("OstyDamage"))
            {
                damage = card.DynamicVars.OstyDamage.BaseValue;
            }
            else
            {
                Log.Warn(base.Id.Entry + " exhausted attack card " + card.Id.Entry + " that did not have an appropriate damage var!");
            }
            damage = Hook.ModifyDamage(base.Owner.RunState, base.Owner.Creature.CombatState, null, base.Owner.Creature, damage, ValueProp.Move, card, null, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> _);
            await CardCmd.Exhaust(choiceContext, card);
        }
        
        await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, damage, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}