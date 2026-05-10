using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class InterceptedPower : IndomitableTemporaryPower<StrengthPower>
{
    protected override bool IsPositive => false;
    
    // 如果没有特定绑定的单张卡牌，返回 null 即可（文本已由 CustomPowerModel 自动处理）
    public override AbstractModel OriginModel => null!;
}