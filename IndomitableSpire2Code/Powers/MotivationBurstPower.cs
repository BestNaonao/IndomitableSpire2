using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class MotivationBurstPower : IndomitablePower
{
    private const int OverflowThreshold = 20;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // UI上显示：距离下一次转化还差多少溢出量
    public override int DisplayAmount => OverflowThreshold - (GetInternalData<Data>().OverflowAccumulated % OverflowThreshold);
    public override bool IsInstanced => true; // 允许有内部独立数据

    protected override object InitInternalData() => new Data();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("OverflowThreshold", OverflowThreshold)];

    // 【核心接口】：专供 MotivationPower 调用的溢出处理方法
    public async Task ProcessOverflow(decimal overflowAmount, Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        data.OverflowAccumulated += (int)overflowAmount;

        // 计算可以触发几次转化（每 20 点 1 次）
        var triggers = data.OverflowAccumulated / OverflowThreshold - data.TriggerCount;

        if (triggers > 0)
        {
            Flash(); // 闪烁本能力图标
            
            // 每次触发给予等同于当前能力层数的活力
            var vigorToGain = Amount * triggers;
            await PowerCmd.Apply<VigorPower>(Owner, vigorToGain, applier, cardSource);
            
            data.TriggerCount += triggers;
        }

        // 更新 UI 上的剩余次数显示
        InvokeDisplayAmountChanged();
    }

    // 内部数据类：记录累积的溢出量和已触发的次数
    private class Data
    {
        public int OverflowAccumulated;
        public int TriggerCount;
    }
}