using BaseLib.Cards.Variables;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class HypnotizedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 自定义的变量名称前缀
    private const string BlockVarName = "HypnotizedBlock";
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new(BlockVarName + "Base", 0m),    // 应对 CalculatedVar 初始化检查的占位符（不参与实际计算）
        new(BlockVarName + "Extra", 0m),   // 应对 CalculatedVar 初始化检查的占位符（不参与实际计算）
        // 使用 BaseLib 的 CustomCalculatedBlockVar，指定名字为 BlockVarName
        new CustomCalculatedBlockVar(BlockVarName, ValueProp.Unpowered)
            .WithMultiplier((power, _) => 20m + 20m * power.CombatState.PlayerCreatures.Count(c => c.IsAlive))
    ];
    
    // 用于精准区分“欲催眠状态”和“已催眠状态”
    private bool IsSleeping { get; set; }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsSleeping 
        ? Array.Empty<IHoverTip>() : [CustomHoverTipFactory.FromIntent<SleepIntent>()];
    
    // 【修改 2】动态文本切换：根据是否已入眠，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsSleeping
        ? $"{Id.Entry}.smartDescriptionSleeping"
        : $"{Id.Entry}.smartDescriptionAwake";
    
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        
        if (!IsSleeping)
        {
            // Tm_1 结束：怪物进入催眠状态
            IsSleeping = true;
            await ApplySleepState();
        }
        else
        {
            // Tm_2 结束：怪物处于睡眠状态，回合结束时扣减层数
            await PowerCmd.Decrement(this);
            if (Amount <= 0) 
                await PowerCmd.Remove(this);
            else 
                await ApplySleepState();
        }
    }
    
    // 提取出的核心逻辑复用方法
    private async Task ApplySleepState()
    {
        Flash();
        // 1. 核心修改：调用自定义的生物睡眠扩展
        Owner.SleepInternal(SleepMove, null);
        // 获得格挡，动态计算当前回合应赋予的格挡值：类型强转并调用 CalculateCustom
        if (DynamicVars[BlockVarName] is CustomCalculatedBlockVar customVar)
            await CreatureCmd.GainBlock(Owner, customVar.CalculateCustom(Owner), ValueProp.Unpowered, null);
    }
    
    private static async Task SleepMove(IReadOnlyList<Creature> targets) => await Task.CompletedTask;
    
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && IsSleeping)
        {
            await PowerCmd.Decrement(this);
            if (Amount <= 0) await PowerCmd.Remove(this);
        }
    }
    
    // 利用原生生命周期钩子处理清理逻辑
    public override Task AfterRemoved(Creature oldOwner)
    {
        IsSleeping = false;
        return base.AfterRemoved(oldOwner);
    }
}