using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class OnFirePower : DOTPower<OnFirePower> // 继承自你的基类
{
    // 使用鲜艳的橙色/红色作为数字颜色
    public override Color AmountLabelColor => new("FFA200");
    protected override decimal Proportion => 0.005m;
    
    // --- BaseLib 血条预测配置 ---
    // 血条颜色：橙色
    protected override Color ForecastBarColor => new("FFA200"); 
    // 致死文本颜色：金色
    protected override Color ForecastLethalTextColor => new("FFD700");
    protected override int ForecastOrder => 10;
}