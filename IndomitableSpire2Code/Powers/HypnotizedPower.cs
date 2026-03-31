using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class HypnotizedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    private NSleepingVfx? _sleepingVfx;
    // 用于精准区分“欲催眠状态”和“已催眠状态”
    public bool IsSleeping { get; private set; }
    
    // 【修改 2】动态文本切换：根据是否已入眠，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsSleeping
        ? $"{Id.Entry}.smartDescriptionSleeping"
        : $"{Id.Entry}.smartDescriptionAwake";
    
    public override string CustomBigIconPath => 
        "res://IndomitableSpire2/images/powers/big/hypnotized_power.png";
    public override string CustomPackedIconPath =>
        "res://IndomitableSpire2/images/powers/packed/hypnotized_power_packed.tres";

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
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
            {
                // 直接移除能力，触发 AfterRemoved 钩子进行清理
                await PowerCmd.Remove(this);
            }
            else
            {
                // 健壮性处理：剩余层数大于 0，继续维持睡眠、重置格挡、清理多余眩晕特效
                await ApplySleepState();
            }
        }
    }

    // 提取出的核心逻辑复用方法
    private async Task ApplySleepState()
    {
        Flash();
        // 1. 施加 Stun 挂起下一回合意图，并重新给予 50 点格挡
        await CreatureCmd.Stun(Owner, SleepMove);
        await CreatureCmd.GainBlock(Owner, 50m, ValueProp.Move, null);
        
        // 2. 等待短暂延迟，确保 NStunnedVfx 的 CallDeferred 执行完毕
        await Cmd.CustomScaledWait(0.01f, 0.05f);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode != null)
        {
            // 3. 精准抹除当前怪物的眩晕特效
            Node? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            if (vfxContainer != null)
            {
                var targetPos = creatureNode.GetTopOfHitbox(); 
                foreach (var child in vfxContainer.GetChildren())
                {
                    if (child is NStunnedVfx stunVfx && stunVfx.GlobalPosition.DistanceTo(targetPos) < 10.0f)
                    {
                        stunVfx.QueueFreeSafely();
                    }
                }
            }

            // 4. 位置回退策略与挂载睡眠特效 (仅在尚未挂载时创建)
            if (_sleepingVfx == null)
            {
                var sleepPosNode = creatureNode.GetSpecialNode<Marker2D>("%SleepVfxPos");
                var intentPosNode = creatureNode.GetSpecialNode<Marker2D>("%IntentPos");

                var spawnPos = creatureNode.GetTopOfHitbox(); 
                if (sleepPosNode != null) spawnPos = sleepPosNode.GlobalPosition;
                else if (intentPosNode != null) spawnPos = intentPosNode.GlobalPosition;

                _sleepingVfx = NSleepingVfx.Create(spawnPos);

                if (_sleepingVfx != null) 
                {
                    creatureNode.AddChildSafely(_sleepingVfx);
                    _sleepingVfx.GlobalPosition = spawnPos;
                }
            }
        }
    }

    private async Task SleepMove(IReadOnlyList<Creature> targets) => await Task.CompletedTask;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && IsSleeping)
        {
            await PowerCmd.Decrement(this);
            if (Amount <= 0)
            {
                // 同样直接移除能力，依赖 AfterRemoved 钩子兜底
                await PowerCmd.Remove(this);
            }
        }
    }

    // 利用原生生命周期钩子处理清理逻辑
    public override Task AfterRemoved(Creature oldOwner)
    {
        IsSleeping = false;

        if (_sleepingVfx == null) return Task.CompletedTask;
        _sleepingVfx.Stop();
        _sleepingVfx.QueueFreeSafely(); 
        _sleepingVfx = null;

        // 因为原本的钩子签名是 Task，且里面只有同步代码，我们返回 CompletedTask 消除飘绿警告
        return Task.CompletedTask;
    }
}