using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public class FinalAttempt() : TashkentCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12M, ValueProp.Move),
        new CardsVar(3)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        var drawPile = PileType.Draw.GetPile(base.Owner).Cards;

        var candidates = drawPile
            .ToList()
            .StableShuffle(base.Owner.RunState.Rng.Shuffle)
            .Take(base.DynamicVars.Cards.IntValue)
            .ToList();
        
        var selectedList = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1)
        );

        CardModel? cardModel = selectedList.FirstOrDefault();
        
        if (cardModel != null)
        {
            await CardCmd.AutoPlay(choiceContext, cardModel, null);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6M);
    }
}