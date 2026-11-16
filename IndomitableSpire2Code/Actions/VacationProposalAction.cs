using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Actions;

/// <summary>模仿 VoteForMapCoordAction：由原版房主转发并排序，只同步最后确认的数据，不同步预览 UI。</summary>
public sealed class VacationProposalAction(Player player, int proposalId, int act, MapCoord? source,
    MapCoord destination, int mapGeneration) : GameAction
{
    public override ulong OwnerId => player.NetId;
    // 和地图投票一样可在战斗期间打开地图使用，不必等战斗结束。
    public override GameActionType ActionType => GameActionType.Any;
    
    protected override Task ExecuteAction()
    {
        // 与地图投票一样拒绝旧地图请求；服务还会检查阶段、出发点、实例、余量与目标实时类型。
        if (RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount == mapGeneration)
            VacationProposalService.TryCommit(player, proposalId, act, source, destination);
        return Task.CompletedTask;
    }
    
    public override INetAction ToNetAction() => new NetVacationProposalAction
    {
        ProposalId = proposalId,
        Act = act,
        Source = source,
        Destination = destination,
        MapGeneration = mapGeneration
    };
}

public struct NetVacationProposalAction : INetAction
{
    public int ProposalId;
    public int Act;
    public MapCoord? Source;
    public MapCoord Destination;
    public int MapGeneration;
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ProposalId);
        writer.WriteInt(Act);
        writer.WriteBool(Source.HasValue);
        if (Source is { } source)
        {
            writer.WriteInt(source.col); writer.WriteInt(source.row);
        }
        writer.WriteInt(Destination.col); writer.WriteInt(Destination.row);
        writer.WriteInt(MapGeneration);
    }
    
    public void Deserialize(PacketReader reader)
    {
        ProposalId = reader.ReadInt();
        Act = reader.ReadInt();
        Source = reader.ReadBool() ? new MapCoord(reader.ReadInt(), reader.ReadInt()) : null;
        Destination = new MapCoord(reader.ReadInt(), reader.ReadInt());
        MapGeneration = reader.ReadInt();
    }
    
    public GameAction ToGameAction(Player player) =>
        new VacationProposalAction(player, ProposalId, Act, Source, Destination, MapGeneration);
}