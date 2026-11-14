using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// 专属上下文：承载操作玩家、具体模型以及鼠标/手柄来源。
public record IndomitableClickContext(Player Player, AbstractModel Model, IndomitableClickContext.Payload Extra = default)
{
    public struct Payload : IPacketSerializable
    {
        public bool IsController { get; private set; } // 输入是否来自手柄
        public string? Meta { get; private set; } // 备用元数据
        
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

/// <summary>
/// 参考 RitsuLib RightClick 的模型分发、本地过滤与执行时复查分层；不依赖 RitsuLib。真正改变玩法的交互通过原版行动队列同步。
/// </summary>
public interface IIndomitableClickableModel
{
    // 点击操作是否需要在多人游戏中进行网络同步。
    bool SynchronizeClick => true;
    
    // 本地是否需要处理点击事件，用于本地加入网络队列前的预处理检查。
    bool CanHandleClickLocal(IndomitableClickContext context) => true;
    
    // 是否允许执行，用于在网络同步到达客户端后的执行时检查。
    bool CanExecuteClick(IndomitableClickContext context) => true;
    
    // 异步点击的处理逻辑
    Task OnClick(PlayerChoiceContext? choiceContext, IndomitableClickContext clickContext);
}