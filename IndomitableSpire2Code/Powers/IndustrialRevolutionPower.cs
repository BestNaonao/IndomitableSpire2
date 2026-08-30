using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class IndustrialRevolutionPower : IndomitablePower, IHasSecondAmount
{
    private const int Threshold = 4;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 允许有多个实例与内部独立数据
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    protected override object InitInternalData() => new RevolutionData();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ThresholdVar( Threshold)];
    
    // 第一个展示数：距离下一次触发还差几张牌
    public override int DisplayAmount => Threshold - GetInternalData<RevolutionData>().CardsGenerated % Threshold;
    // 第二个展示数：目前积攒了多少张牌将获得“重放”
    public string GetSecondAmount() => GetInternalData<RevolutionData>().PendingReplays.ToString();
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];
    
    // 拦截卡牌生成事件（效仿原版武器库 ArsenalPower）
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        // 确保是玩家生成的卡
        if (creator == null || creator.Creature != Owner) 
            return Task.CompletedTask;
        
        // 过滤不参与计数的卡池
        var poolName = card.Pool.GetType().Name;
        if (poolName is "CurseCardPool" or "StatusCardPool" or "DeprecatedCardPool" or "MockCardPool") 
            return Task.CompletedTask;
        
        var data = GetInternalData<RevolutionData>();
        data.CardsGenerated++;
        
        // 计算是否有新的转化
        var triggers = data.CardsGenerated / Threshold - data.TriggerCount;
        if (triggers > 0)
        {
            Flash(); // 闪烁本能力图标
            // 每触发1次，获得等同于能力层数（Amount）的“待重放”充能
            data.PendingReplays += triggers * Amount;
            data.TriggerCount += triggers;
        }
        
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    
    // 1. 同步拦截钩子：直接修改卡牌的打出次数
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        var data = GetInternalData<RevolutionData>();
        // 如果是玩家自己的卡，且还有待触发的重放充能
        if (card.Owner != Owner.Player || data.PendingReplays <= 0) return playCount;
        data.PendingReplays--;  // 消耗一层充能
        card.BaseReplayCount += 1;  // ModifyCardPlayCount 只是让本次打出变成了2次。必须在这里给底层数据加上 BaseReplayCount。
        InvokeDisplayAmountChanged();   // 刷新 UI 的数字
        NCard.FindOnTable(card)?.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);  // 重新读取文本
        return playCount + 1;           // 告诉引擎：这张牌的最终打出次数 +1
    }
    
    // 2. 异步视觉/结算钩子：只有当你在上面的方法里修改了次数，引擎才会调用这里
    public override Task AfterModifyingCardPlayCount(CardModel card)
    { 
        Flash(); // 闪烁工业革命的图标
        return Task.CompletedTask;
    }
    
    // 内部数据类
    private class RevolutionData
    {
        public int CardsGenerated;
        public int TriggerCount;
        public int PendingReplays; // 积攒的待触发“重放”次数
    }
}