using BaseLib.Hooks;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// ReSharper disable once InconsistentNaming for Special Abbreviation
public abstract class DOTPower<TDerived> : DynamicVarSyncPower where TDerived : DOTPower<TDerived>
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 按施加者独立实例化（多人模式下区分玩家）
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    
    // 【改进】：重写 InitInternalData 挂载内部数据类型
    protected override object InitInternalData() => new DotData();
    
    protected virtual decimal Proportion => 0.01m;
    
    // --- BaseLib 血条预测配置 ---
    // 1. 血条的实际颜色
    protected abstract Color ForecastBarColor { get; }
    // 2. 致死时的文字颜色
    protected abstract Color ForecastLethalTextColor { get; }
    // 3. 排序层级
    protected abstract int ForecastOrder { get; }
    
    // 【改进】：精简 CanonicalVars，只保留展示用变量和 Applier 名称
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("NextLostHpPercent", 0m),   // 下次损失百分比
        new("NextLostHpInt", 0m),       // 下次即将损失的整数生命
        new StringVar("Applier")        // 施加者名称
    ];
    
    // 计算下次带小数保留的伤害与真实整数伤害
    private decimal ExactNextDamage => Owner.MaxHp * Amount * Proportion;
    private int GetNextDamage
    {
        get
        {
            var data = GetInternalData<DotData>();
            return Math.Max(0, (int)Math.Floor(data.TotalLostHpExact + ExactNextDamage) - data.TotalLostHpInt);
        }
    }
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Applier != null)
        {
            ((StringVar)DynamicVars["Applier"]).StringValue = Applier.Player != null
                ? PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, Applier.Player.NetId)
                : Applier.Name;
        }
        return Task.CompletedTask;
    }
    
    protected override void SyncDynamicVars()
    {
        DynamicVars["NextLostHpPercent"].BaseValue = Amount * 100 * Proportion;
        DynamicVars["NextLostHpInt"].BaseValue = GetNextDamage;
        InvokeDisplayAmountChanged();
    }
    
    // 核心伤害逻辑
    protected virtual async Task TriggerDamage()
    {
        if (Amount <= 0 || Owner.IsDead) return;
        var data = GetInternalData<DotData>();
        
        // 1. 计算精确伤害并累加至 DotData 内部变量
        data.TotalLostHpExact += ExactNextDamage;
        
        // 2. 提取需要扣除的整数部分
        var damageToDeal = (int)Math.Floor(data.TotalLostHpExact) - data.TotalLostHpInt;
        if (damageToDeal > 0)
        {
            data.TotalLostHpInt += damageToDeal;
            Flash();
            
            // 造成无视格挡、不受力量影响的绝对伤害（模仿 Poison）
            var results = await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(), Owner, damageToDeal, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            
            // 3. 统计实际造成伤害并写入 Applier 的战斗全局数据集中
            var actualDamageDealt = results.Sum(r => r.TotalDamage + r.OverkillDamage);
            if (actualDamageDealt > 0 && Applier?.Player?.PlayerCombatState != null)
            {
                // 将造成的伤害上报给施加者的专属记录字典。注意：这里使用奇异递归模板模式，这样“起火”和“进水”会自动分开记录！
                Applier.Player.PlayerCombatState.RecordPowerDamage<TDerived>(actualDamageDealt);
                MainFile.Logger.Info($"Total {typeof(TDerived).Name} Damage Given by {Applier.Name} this Combat: " +
                                     $"{Applier.Player.PlayerCombatState.GetTotalPowerDamage<TDerived>()}");
            }
        }
        
        // 4. 层数衰减与 UI 更新
        if (Owner.IsAlive)
        {
            await PowerCmd.Decrement(this);
            SyncDynamicVars();
        }
        else await Cmd.CustomScaledWait(0.1f, 0.25f);
    }
    
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
    
    // 【改进】：用于存储内部伤害计算状态的数据类型
    protected class DotData
    {
        public decimal TotalLostHpExact; // 总累计小数伤害
        public int TotalLostHpInt;       // 总累计已扣除的整数伤害
    }
}