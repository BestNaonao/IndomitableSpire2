using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class FloodingPower : DOTPower
{
    // 使用天蓝色作为进水的层数显示颜色
    public override Color AmountLabelColor => new("33CCFF");
    public override decimal Proportion => 0.01m;
    
    // --- BaseLib 血条预测配置 ---
    // 血条颜色：天蓝色
    public override Color ForecastBarColor => new("33CCFF"); 
    // 致死文本颜色：亮青色 (BaseLib 会自动压暗它来做描边)
    public override Color ForecastLethalTextColor => new("88FFFF");
    public override int ForecastOrder => 15;

    // 注册变量池，新增 DamageIncreasePercent 用于 UI 动态显示易伤比例
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("DamageIncreasePercent", 0m));
    
    protected override string SmartDescriptionLocKey => HasNonAttackIntent 
        ? $"{Id.Entry}.smartDescriptionFull"
        : $"{Id.Entry}.smartDescription";
    
    public override string CustomBigIconPath => 
        "res://IndomitableSpire2/images/powers/big/flooding_power.png";
    public override string CustomPackedIconPath =>
        "res://IndomitableSpire2/images/powers/packed/flooding_power_packed.tres";

    protected override void Update()
    {
        DynamicVars["DamageIncreasePercent"].BaseValue = Amount * 5m;
        base.Update();
    }

    // 判断当前意图列表中是否包含【非攻击】且【非死亡攻击】的意图
    private bool HasNonAttackIntent => Owner.Monster != null && Owner.Monster.NextMove.Intents.Any(intent =>
        intent.IntentType != IntentType.Attack && intent.IntentType != IntentType.DeathBlow);
    
    // ========== 核心机制：基于意图的动态易伤乘区 ==========
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 确保受到攻击的是拥有者怪物本身，且是享受力量加成的正常攻击
        var isPoweredAttack = props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
        // 有非攻击意图的怪物在每层进水下增加 5% 承受伤害
        return target != Owner || !isPoweredAttack || !HasNonAttackIntent
            ? 1m : 1m + Amount * 0.05m;  
    }
}