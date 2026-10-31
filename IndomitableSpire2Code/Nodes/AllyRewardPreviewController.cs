using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>生命周期随本次 NCardPlay；只读本地预览，不发送游戏动作或修改模型。</summary>
public partial class AllyRewardPreviewController : Node
{
    internal const string NodeName = "AllyRewardPreview";
    private static readonly Func<NMouseCardPlay, bool> InMousePlayZone =
        AccessTools.MethodDelegate<Func<NMouseCardPlay, bool>>(AccessTools.Method(typeof(NMouseCardPlay), "IsCardInPlayZone"));
    private static readonly AccessTools.FieldRef<NCard, Creature?> PreviewTarget =
        AccessTools.FieldRefAccess<NCard, Creature?>("_previewTarget");
    private readonly Dictionary<Player, AllyRewardPreviewRow> _rows = []; // 卡牌->玩家->奖励。缓存已创建的 UI 行，用于复用节点。
    private bool _stopped; // 停止标志，防止在节点销毁过程中继续执行 _Process
    
    public override void _Process(double delta)
    {
        if (_stopped) return;
        var play = GetParent<NCardPlay>(); // 获取父节点，充当当前的“卡牌打出”上下文对象
        if (play.IsQueuedForDeletion() || !IsInstanceValid(play.Holder) || NCombatRoom.Instance is not { } room ||
            play.Holder.CardModel is not { } card || card.Pile?.Type != PileType.Hand ||
            play is NMouseCardPlay mouse && !InMousePlayZone(mouse) || !card.CanPlay())
        {
            ClearRows();
            return;
        }
        // 获取目标上下文数据，并收集重组数据，分组组织成字典的形式
        var target = play.Holder.CardNode is { } node ? PreviewTarget(node) : null;
        var groups = AllyRewardPreviewRules.Collect(card, new AllyRewardPreviewContext(target))
            .GroupBy(reward => reward.Recipient)
            .ToDictionary(group => group.Key, group => group.ToArray());
        // 清理没有新奖励的玩家的旧 UI 行
        foreach (var previous in _rows.Keys.Where(player => !groups.ContainsKey(player)).ToArray())
            RemoveRow(previous);
        // 更新或创建有效的 UI 行
        foreach (var (recipient, rewards) in groups)
        {
            var creature = room.GetCreatureNode(recipient.Creature);
            // 防御处理，确保玩家的可视化节点有效
            if (creature == null || !IsInstanceValid(creature) || creature.IsQueuedForDeletion() ||
                !creature.IsNodeReady() || creature.Visuals is not { } visuals)
            {
                RemoveRow(recipient);
                continue;
            }
            // 决定是复用还是新建
            if (!_rows.TryGetValue(recipient, out var row) || !IsInstanceValid(row) || row.GetParent() != creature)
            {
                RemoveRow(recipient); // 处理旧的脏数据
                row = new AllyRewardPreviewRow { Name = "AllyRewardPreviewRow" };
                creature.AddChild(row);
                _rows[recipient] = row;
            }
            row.SetRewards(rewards); // 更新 UI 内容
            row.Scale = AllyRewardPreviewSettings.MatchOrbScale && creature.OrbManager is { } orbs ? orbs.Scale : Vector2.One;
            // 两个标记都位于 Visuals 的坐标空间；先在全局坐标插值，再转回收奖者节点坐标。
            var midpoint = visuals.OrbPosition.GlobalPosition.Lerp(visuals.IntentPosition.GlobalPosition,
                AllyRewardPreviewSettings.MarkerBlend);
            row.Position = creature.GetGlobalTransform().AffineInverse() * midpoint +
                AllyRewardPreviewSettings.PositionOffset - row.Size * row.Scale * 0.5F;
        }
    }
    
    internal void Stop()
    {
        _stopped = true;
        SetProcess(false);
        ClearRows();
    }
    
    public override void _ExitTree() => ClearRows();
    
    private void ClearRows()
    {
        foreach (var player in _rows.Keys.ToArray()) RemoveRow(player);
    }
    
    private void RemoveRow(Player recipient)
    {
        if (!_rows.Remove(recipient, out var row) || !IsInstanceValid(row)) return;
        row.Hide();
        row.QueueFree();
    }
}