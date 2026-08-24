using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace IndomitableSpire2.IndomitableSpire2Code.Actions;

/// <summary>
/// “认真模式”按钮专用同步 Action。它只会操作发起玩家自己的全神贯注能力。
/// </summary>
public sealed class EarnestModeButtonAction : GameAction
{
    public Player Player { get; }
    public uint CreatureCombatId { get; }
    
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;
    
    public EarnestModeButtonAction(Player player)
    {
        Player = player;
        CreatureCombatId = player.Creature.CombatId
            ?? throw new InvalidOperationException("Earnest mode can only be requested during combat.");
    }
    
    public EarnestModeButtonAction(Player player, uint creatureCombatId)
    {
        Player = player;
        CreatureCombatId = creatureCombatId;
    }
    
    protected override async Task ExecuteAction()
    {
        try
        {
            var combatState = Player.Creature.CombatState;
            var creature = combatState?.GetCreature(CreatureCombatId);
            
            // CreatureCombatId 和 Player 必须互相对应，不能借按钮操作其他玩家的能力。
            if (creature?.Player != Player || creature.GetPower<FullConcentrationPower>() is not { } concentration)
                return;
            
            var choiceContext = new GameActionPlayerChoiceContext(this);
            await concentration.EnterEarnestMode(choiceContext, Player);
            concentration.InvokeExecutionFinished();
        }
        finally
        {
            EarnestModeButton.NotifyActionResolved();
        }
    }
    
    public override INetAction ToNetAction()
    {
        return new NetEarnestModeButtonAction
        {
            CreatureCombatId = CreatureCombatId
        };
    }
}

public struct NetEarnestModeButtonAction : INetAction
{
    public uint CreatureCombatId;
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(CreatureCombatId);
    }
    
    public void Deserialize(PacketReader reader)
    {
        CreatureCombatId = reader.ReadUInt();
    }
    
    public GameAction ToGameAction(Player player)
    {
        return new EarnestModeButtonAction(player, CreatureCombatId);
    }
}