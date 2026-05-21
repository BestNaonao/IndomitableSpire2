using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Ancients;

public sealed class ScorchingFlameRain() : IndomitableCard(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
{
    // 注册变量：12点群体伤害，6层群体起火，30点干劲门槛
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(30M),
        new DamageVar(12M, ValueProp.Move),
        new CustomPowerVar<OnFirePower>(6M)
    ];
    
    // 添加“需求”提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.Require)];
    
    // 核心限制：复用点燃的判断逻辑
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        // 1. 群体伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_fire_burst") 
            .Execute(choiceContext);
        
        // 2. 群体起火
        await PowerCmd.Apply<OnFirePower>(
            targets: CombatState.HittableEnemies, 
            amount: DynamicVars.OnFire().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：伤害 +4，起火层数 +2
        DynamicVars.Damage.UpgradeValueBy(4M);
        DynamicVars.OnFire().UpgradeValueBy(2M);
    }
}