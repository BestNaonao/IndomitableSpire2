using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class ClusterBomb() : TashkentCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private decimal _extraDamageFromPlays;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9M, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    private decimal ExtraDamageFromPlays
    {
        get => _extraDamageFromPlays;
        set
        {
            AssertMutable();
            _extraDamageFromPlays = value;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        decimal currentDamage = DynamicVars.Damage.BaseValue;

        await DamageCmd.Attack(currentDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        decimal newDamage = currentDamage * 2;
        ExtraDamageFromPlays += (newDamage - currentDamage);

        DynamicVars.Damage.BaseValue = newDamage;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamageFromPlays;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            CardModel cardclone = CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(cardclone, PileType.Discard, addedByPlayer: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
    }
}