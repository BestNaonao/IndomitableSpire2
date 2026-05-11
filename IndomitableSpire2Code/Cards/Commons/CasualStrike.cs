using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class CasualStrike() : IndomitableCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 赋予打击标签
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    
    // 注册变量：8点伤害，10点干劲获取 (使用你写好的专属 Var)
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DamageVar(8M, ValueProp.Move),
        new MotivationGainVar(10M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        // 2. 获得干劲
        await PowerCmd.Apply<MotivationPower>(
            target: Owner.Creature, 
            amount: DynamicVars.MotivationGain().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.MotivationGain().UpgradeValueBy(5M);
    }
}