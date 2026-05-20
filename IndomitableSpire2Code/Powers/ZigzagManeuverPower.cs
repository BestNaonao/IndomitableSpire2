using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers; // 确保引入了卡牌的命名空间

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ZigzagManeuverPower : IndomitableTemporaryPower<DexterityPower>
{
    // 默认为 false，无需任何反转
    protected override bool InvertInternalPowerAmount => false;
    
    // 绑定来源卡牌
    public override AbstractModel OriginModel => ModelDb.Card<ZigzagManeuver>();
}