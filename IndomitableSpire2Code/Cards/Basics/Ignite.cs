using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Basics;

public sealed class Ignite() : IndomitableCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<OnFirePower>()];
    
    // 注册变量：8点伤害，3层起火
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DamageVar(8M, ValueProp.Move),
        new PowerVar<OnFirePower>(3M),
        new MotivationRequireVar(30M)
    ];
    
    // 核心限制：重写 IsPlayable 属性
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(choiceContext);
        
        // 2. 如果目标存活，施加起火
        if (cardPlay.Target is { IsAlive: true })
            await PowerCmd.Apply<OnFirePower>(
                target: cardPlay.Target, 
                amount: DynamicVars["OnFirePower"].BaseValue, 
                applier: Owner.Creature, 
                cardSource: this
            );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：伤害 +3，起火层数 +1
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars["OnFirePower"].UpgradeValueBy(1M);
    }
}