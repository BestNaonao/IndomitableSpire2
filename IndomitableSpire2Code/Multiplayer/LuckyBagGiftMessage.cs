using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Multiplayer;

/// <summary>
/// 商店钩子只在购物端运行，因此先通知各端建立相同的预览奖励集，
/// 再交给原版 RewardsSetSynchronizer 同步选择/跳过，保持后续奖励编号一致。
/// </summary>
public sealed class LuckyBagGiftMessage : ICustomTargetedMessage
{
    public RelicModel Relic = null!;
    public LuckyBagRelicRule Rule;
    public bool FromPool;
    public RunLocation Location { get; private set; }
    public bool ShouldBroadcast => true;
    public bool IsRewardMessage => true;
    
    internal static void Send(RelicModel relic, LuckyBagRelicRule rule, bool fromPool)
    {
        var run = RunManager.Instance;
        if (run.NetService.Type is not (NetGameType.Host or NetGameType.Client)) return;
        CustomTargetedMessageWrapper.Send(new LuckyBagGiftMessage
        {
            Relic = relic, Rule = rule, FromPool = fromPool, Location = run.RunLocationTargetedBuffer.CurrentLocation
        });
    }
    
    public void Serialize(PacketWriter writer)
    {
        writer.Write(Location);
        writer.Write(Relic.ToSerializable());
        writer.WriteInt((int)Rule);
        writer.WriteBool(FromPool);
    }
    
    public void Deserialize(PacketReader reader)
    {
        Location = reader.Read<RunLocation>();
        Relic = RelicModel.FromSerializable(reader.Read<SerializableRelic>());
        Rule = (LuckyBagRelicRule)reader.ReadInt();
        FromPool = reader.ReadBool();
    }
    
    public void HandleMessage(ulong senderId)
    {
        if (Rule is not (LuckyBagRelicRule.SkipAndReturn or LuckyBagRelicRule.SkipAndDestroy)) return;
        if (RunManager.Instance.DebugOnlyGetState()?.GetPlayer(senderId) is not { } player) return;
        TaskHelper.RunSafely(CustomRelicCmd.OfferGift(Relic, player, Rule, FromPool, synchronize: false));
    }
}