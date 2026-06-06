using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace IndomitableSpire2.IndomitableSpire2Code.Actions;

public class IndomitableClickPowerAction : GameAction
{
    public Player Player { get; }
    public IndomitableClickContext.Payload Extra { get; }
    public bool WasEnqueuedInCombat { get; }
    public ModelId ModelId { get; }
    public uint CreatureCombatId { get; }
    
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => WasEnqueuedInCombat ? GameActionType.CombatPlayPhaseOnly : GameActionType.NonCombat;
    
    // 构造函数：本地触发时组装
    public IndomitableClickPowerAction(IndomitableClickContext context, bool isCombatInProgress)
    {
        WasEnqueuedInCombat = isCombatInProgress;
        Player = context.Player;
        Extra = context.Extra;
        ModelId = context.Model.Id;

        if (context.Model is PowerModel { Owner.CombatId: not null } power)
        {
            CreatureCombatId = power.Owner.CombatId.Value;
        }
    }

    // 构造函数：网络反序列化时还原
    public IndomitableClickPowerAction(Player player, ModelId modelId, uint creatureCombatId, IndomitableClickContext.Payload extra, bool isCombatInProgress)
    {
        Player = player;
        ModelId = modelId;
        CreatureCombatId = creatureCombatId;
        Extra = extra;
        WasEnqueuedInCombat = isCombatInProgress;
    }

    // 全局同步执行点
    protected override async Task ExecuteAction()
    {
        var combatState = Player.Creature.CombatState;
        if (WasEnqueuedInCombat && combatState is null) return;

        // 根据同步过来的 CombatId 和 ModelId 找回那个被点击的能力
        var model = combatState!.GetCreature(CreatureCombatId)?.Powers.FirstOrDefault(p => p.Id == ModelId);
        
        // 极度安全的强转判定
        if (model is not IIndomitableClickablePower clickable) return;

        var choiceContext = new GameActionPlayerChoiceContext(this);
        
        // 触发具体的点击逻辑
        await clickable.OnClick(choiceContext, new IndomitableClickContext(Player, model, Extra));
        model.InvokeExecutionFinished();
    }

    public override INetAction ToNetAction()
    {
        return new NetIndomitableClickPowerAction
        {
            ModelId = ModelId,
            CreatureCombatId = CreatureCombatId,
            Extra = Extra,
            WasEnqueuedInCombat = WasEnqueuedInCombat
        };
    }
}

// 纯网络数据包结构体
public struct NetIndomitableClickPowerAction : INetAction
{
    public ModelId ModelId;
    public uint CreatureCombatId;
    public IndomitableClickContext.Payload Extra;
    public bool WasEnqueuedInCombat;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFullModelId(ModelId);
        writer.WriteUInt(CreatureCombatId);
        writer.Write(Extra);
        writer.WriteBool(WasEnqueuedInCombat);
    }

    public void Deserialize(PacketReader reader)
    {
        ModelId = reader.ReadFullModelId();
        CreatureCombatId = reader.ReadUInt();
        Extra = reader.Read<IndomitableClickContext.Payload>();
        WasEnqueuedInCombat = reader.ReadBool();
    }

    public GameAction ToGameAction(Player player)
    {
        return new IndomitableClickPowerAction(player, ModelId, CreatureCombatId, Extra, WasEnqueuedInCombat);
    }
}