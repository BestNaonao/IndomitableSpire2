using BaseLib.Hooks;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Extensions; // 引入刚才的追踪器
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// ReSharper disable once InconsistentNaming for Special Abbreviation
public abstract class DOTPower<TDerived> : DynamicVarSyncPower where TDerived :  DOTPower<TDerived>
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 【核心改造 1】：根据施加者（Applier）进行独立实例化。
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    
    // 每回合前后损失生命占最大生命的百分比
    protected virtual decimal Proportion => 0.01m;
    
    // --- BaseLib 血条预测配置 ---
    // 1. 血条的实际颜色
    protected abstract Color ForecastBarColor { get; }
    // 2. 致死时的文字颜色
    protected abstract Color ForecastLethalTextColor { get; }
    // 3. 排序层级
    protected abstract int ForecastOrder { get; }
    
    // 注册需要精确维护的四个动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("TotalLostHpExact", 0m), // 总累计小数伤害（后台静默运行）
        new("TotalLostHpInt", 0m),   // 总累计已扣除的整数伤害
        new("NextLostHpPercent", 0m),// 下次损失百分比（用于UI显示）
        new("NextLostHpInt", 0m)     // 下次即将损失的整数生命（用于UI显示）
    ];
    
    private DynamicVar TotalLostHpExact => DynamicVars["TotalLostHpExact"];
    private DynamicVar TotalLostHpInt => DynamicVars["TotalLostHpInt"];
    
    // 获取带小数保留的下一次伤害
    private decimal ExactNextDamage => Owner.MaxHp * Amount * Proportion;
    // 获取下一次即将造成的真实整数伤害
    private int GetNextDamage => Math.Max(0, (int)Math.Floor(TotalLostHpExact.BaseValue + ExactNextDamage) - TotalLostHpInt.IntValue);
    
    // 【修改】：重写基类的抽象方法，替代原先的 Update()
    protected override void SyncDynamicVars()
    {
        DynamicVars["NextLostHpPercent"].BaseValue = Amount * 100 * Proportion;
        DynamicVars["NextLostHpInt"].BaseValue = GetNextDamage;
        InvokeDisplayAmountChanged();
    }
    
    // 核心伤害逻辑提取
    protected virtual async Task TriggerDamage()
    {
        if (Amount <= 0 || Owner.IsDead) return;
        
        // 1. 计算精确伤害并入池
        TotalLostHpExact.BaseValue += ExactNextDamage;
        
        // 2. 提取需要扣除的整数部分
        var damageToDeal = (int)Math.Floor(TotalLostHpExact.BaseValue) - TotalLostHpInt.IntValue;
        if (damageToDeal > 0)
        {
            TotalLostHpInt.BaseValue += damageToDeal;
            Flash();
            
            // 造成无视格挡、不受力量影响的绝对伤害（模仿 Poison）
            var results = await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(), Owner, damageToDeal, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            
            // 【核心改造 2】：统计实际造成的未被格挡伤害（虽然我们是真实伤害，但这样写最严谨）
            var actualDamageDealt = results.Sum(r => r.TotalDamage + r.OverkillDamage);
            if (actualDamageDealt > 0 && Applier?.Player?.PlayerCombatState != null)
            {
                // 将造成的伤害上报给施加者的专属记录字典。注意：这里使用奇异递归模板模式，这样“起火”和“进水”会自动分开记录！
                Applier.Player.PlayerCombatState.RecordPowerDamage<TDerived>(actualDamageDealt);
                MainFile.Logger.Info($"Total {typeof(TDerived).Name} Damage Given by {Applier.Name} this Combat: " +
                                     $"{Applier.Player.PlayerCombatState.GetTotalPowerDamage<TDerived>()}");
            }
        }
        
        // 3. 层数衰减与UI更新
        if (Owner.IsAlive)
        {
            await PowerCmd.Decrement(this);
            SyncDynamicVars();
        }
        else await Cmd.CustomScaledWait(0.1f, 0.25f);
    }
    
    // 回合开始时和结束时各触发一次
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner) && side == Owner.Side) await TriggerDamage();
    }
    
    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner) && side == Owner.Side) await TriggerDamage();
    }
    
    // 【新增】：实现 BaseLib 对血条的接口要求的方法
    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        var damage = GetNextDamage;
        if (damage > 0)
        {
            yield return new HealthBarForecastSegment(
                Amount: damage,
                Color: ForecastLethalTextColor, // 【核心】将致死文字颜色传给 Color
                Direction: HealthBarForecastDirection.FromRight,
                Order: ForecastOrder,
                OverlayMaterial: null, // 我们不需要特殊的 Shader 材质
                OverlaySelfModulate: ForecastBarColor // 【核心】将血条颜色传给 OverlaySelfModulate
            );
        }
    }
}