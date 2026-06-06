using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// 1. 专属上下文：承载谁点的、点了哪个能力、怎么点的（左/右键/手柄）
public record IndomitableClickContext(Player Player, AbstractModel Model, IndomitableClickContext.Payload Extra = default)
{
    public struct Payload : IPacketSerializable
    {
        public bool IsController { get; private set; }
        public string? Meta { get; private set; }
        
        public Payload(bool isController = false, string? meta = null)
        {
            IsController = isController;
            Meta = meta;
        }
        
        public void Serialize(PacketWriter writer)
        {
            writer.WriteBool(IsController);
            writer.WriteBool(Meta != null);
            if (Meta != null)
                writer.WriteString(Meta);
        }

        public void Deserialize(PacketReader reader)
        {
            IsController = reader.ReadBool();
            Meta = reader.ReadBool() ? reader.ReadString() : null;
        }
    }
}

// 2. 专属接口：你的 Mod 里所有需要点击响应的能力，都必须实现这个接口！
// 这是防止与其他 Mod 产生“二次点击碰撞”的核心绝缘层。
public interface IIndomitableClickablePower
{
    // 本地判定：是否允许向服务器发送点击请求？（比如：只有当前回合玩家才能点）
    bool CanHandleClickLocal(IndomitableClickContext context) => true;
    
    // 全局执行：当所有玩家都收到点击信号后，统一执行的游戏逻辑
    Task OnClick(PlayerChoiceContext choiceContext, IndomitableClickContext clickContext);
}