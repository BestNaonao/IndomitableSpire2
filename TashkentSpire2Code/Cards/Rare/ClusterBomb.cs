using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class ClusterBomb() : TashkentCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private decimal _extraDamageFromExhaust;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9M, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    private decimal ExtraDamageFromExhaust
    {
        get => _extraDamageFromExhaust;
        set
        {
            AssertMutable();
            _extraDamageFromExhaust = value;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            decimal currentBase = DynamicVars.Damage.BaseValue;
            decimal doubleBonus = currentBase;

            DynamicVars.Damage.BaseValue += doubleBonus;
            ExtraDamageFromExhaust += doubleBonus;

            CardModel cardClone = CreateClone();
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cardClone, PileType.Discard, base.Owner), 0.2f);
        }
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue -= ExtraDamageFromExhaust;
        ExtraDamageFromExhaust = 0;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        AddKeyword(CardKeyword.Ethereal);
    }
}