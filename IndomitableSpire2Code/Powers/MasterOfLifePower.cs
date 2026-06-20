using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class MasterOfLifePower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 重写底层生命周期钩子：当生物的当前生命值发生变化时触发
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // 1. 确保是能力拥有者（即玩家）的生命值发生了变化
        // 2. 确保 delta > 0，这代表这是一次生命回复或生命上限提升导致的回血
        if (creature == Owner && delta > 0M)
        {
            Flash(); // 闪烁能力图标提供视觉反馈
            
            // 每次回血触发时，获得等于该能力层数的活力 (Vigor)
            await PowerCmd.Apply<VigorPower>(
                choiceContext: new ThrowingPlayerChoiceContext(), 
                target: Owner, 
                amount: Amount, 
                applier: Owner, 
                cardSource: null
            );
        }
    }
}