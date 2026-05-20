using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class InterceptedPower : IndomitableTemporaryPower<StrengthPower>
{
    // 设为 true：BaseLib 会自动将 X 层截击转换为扣除 X 点力量，且我们的扩展会将其标记为 Debuff
    protected override bool InvertInternalPowerAmount => true;
    
    // 如果没有特定绑定的单张卡牌，返回 null 即可（文本已由 CustomPowerModel 自动处理）
    public override AbstractModel OriginModel => null!;
}