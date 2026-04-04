using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class OnFirePower : DOTPower // 继承自你的基类
{
    // 使用鲜艳的橙色/红色作为数字颜色
    public override Color AmountLabelColor => new("FFA200");
    public override decimal Proportion => 0.005m;
    
    // TODO: 重写智能描述以区分玩家和怪物
    // protected override string SmartDescriptionLocKey => 
    
    public override string CustomBigIconPath => 
        "res://IndomitableSpire2/images/powers/big/on_fire_power.png";
    public override string CustomPackedIconPath =>
        "res://IndomitableSpire2/images/powers/packed/on_fire_power_packed.tres";
}