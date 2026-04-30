using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers; // 确保引入了卡牌的命名空间

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ZigzagManeuverPower : IndomitableTemporaryPower
{
    // 底层实际操作的能力类型：敏捷
    public override PowerModel InternallyAppliedPower => ModelDb.Power<DexterityPower>();
    
    // 绑定来源卡牌
    public override AbstractModel OriginModel => ModelDb.Card<ZigzagManeuver>();
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];
    
    protected override Func<Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc =>
        (target, amount, applier, source, silent) => 
            // 获得临时敏捷，直接传入 amount 即可
            PowerCmd.Apply<DexterityPower>(target, amount, applier, source, silent);
}