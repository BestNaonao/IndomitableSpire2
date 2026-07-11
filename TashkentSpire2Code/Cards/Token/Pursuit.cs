using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Pursuit() : TashkentCard(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6M, ValueProp.Move),
        new MarkDynamicVar(2M),
        new ChargeDynamicVar(1M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, base.DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, this);
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        await PowerCmd.Apply<MarkPower>(choiceContext, cardPlay.Target, base.DynamicVars["TashkentSpire2-Mark"].BaseValue, base.Owner.Creature, this);
    }
    
    public static async Task<IEnumerable<Pursuit>> CreateInHand(Player owner, int amount, ICombatState combatState)
    {
        IEnumerable<Pursuit> Pursuits = Create(owner, amount, combatState);
        await CardPileCmd.AddGeneratedCardsToCombat(Pursuits, PileType.Hand, owner);
        return Pursuits;
    }
    
    public static IEnumerable<Pursuit> Create(Player owner, int amount, ICombatState combatState)
    {
        List<Pursuit> list = new List<Pursuit>();
        for (int i = 0; i < amount; i++)
        {
            list.Add(combatState.CreateCard<Pursuit>(owner));
        }
        return list;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(1M);
    }
}