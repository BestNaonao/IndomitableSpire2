using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Multiplayer;

/// <summary>
/// 每店每玩家一次的赠品操作。发送者就是赠品所有者；货位和遗物用于验证各端的确定性抽取。
/// 消息只唤醒购买钩子，不独立发放遗物；领取/跳过仍使用原版奖励集消息。
/// </summary>
public sealed class LuckyBagGiftMessage : ICustomTargetedMessage
{
    public string OperationId = ""; // 操作 ID，初始化为空串避免反序列化前为 null；发送端填 GUID("N")，用于校验与错误诊断。
    public LuckyBagRelicRule Rule;
    public uint? EntryId;
    public ModelId RelicId = null!;
    public RunLocation Location { get; set; }
    public bool ShouldBroadcast => true;
    public bool IsRewardMessage => false;
    
    public void Serialize(PacketWriter writer)
    {
        writer.Write(Location);
        writer.WriteString(OperationId);
        writer.WriteInt((int)Rule);
        writer.WriteBool(EntryId.HasValue);
        if (EntryId.HasValue) writer.WriteUInt(EntryId.Value);
        writer.WriteString(RelicId.Category);
        writer.WriteString(RelicId.Entry);
    }
    
    public void Deserialize(PacketReader reader)
    {
        Location = reader.Read<RunLocation>();
        OperationId = reader.ReadString();
        Rule = (LuckyBagRelicRule)reader.ReadInt();
        EntryId = reader.ReadBool() ? reader.ReadUInt() : null;
        RelicId = new ModelId(reader.ReadString(), reader.ReadString());
    }
    
    // 框架反序列化后的回调处理，把消息和传输层提供的发送者 ID 转交同步器。数据传输对象保持"薄"，全部逻辑在同步器里。
    public void HandleMessage(ulong senderId) => LuckyBagGiftSynchronizer.Receive(this, senderId);
}