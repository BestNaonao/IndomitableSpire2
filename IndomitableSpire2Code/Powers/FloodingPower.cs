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

    // 注册变量池，新增 DamageIncreasePercent 用于 UI 动态显示易伤比例
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("DamageIncreasePercent", 0m));
    
    // TODO: 重写智能描述以区分玩家和怪物
    // protected override string SmartDescriptionLocKey => 
    
    public override string CustomBigIconPath => 
        "res://IndomitableSpire2/images/powers/big/flooding_power.png";
    public override string CustomPackedIconPath =>
        "res://IndomitableSpire2/images/powers/packed/flooding_power_packed.tres";

    protected override void Update()
    {
        DynamicVars["DamageIncreasePercent"].BaseValue = Amount * 5m;
        base.Update();
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (canonicalPower.Id != Id || target != Owner)
            return base.TryModifyPowerAmountReceived(canonicalPower, target, amount, applier, out modifiedAmount);
        var nextAmount = Amount + amount;
        DynamicVars["DamageIncreasePercent"].BaseValue = nextAmount * 5m;
        // 基础逻辑
        return base.TryModifyPowerAmountReceived(canonicalPower, target, amount, applier, out modifiedAmount);
    }
    
    // ========== 核心机制：基于意图的动态易伤乘区 ==========
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 确保受到攻击的是拥有者怪物本身，且是享受力量加成的正常攻击
        var isPoweredAttack = props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
        if (target != Owner || !isPoweredAttack || Owner.Monster == null)
            return 1m;
        
        // 判断当前意图列表中是否包含【非攻击】且【非致死攻击】的意图，每层进水增加 5% 承受伤害 (0.05m)
        return Owner.Monster.NextMove.Intents.Any(intent => 
                intent.IntentType != IntentType.Attack && intent.IntentType != IntentType.DeathBlow) ? 
            1m + Amount * 0.05m : 1m;
    }
}