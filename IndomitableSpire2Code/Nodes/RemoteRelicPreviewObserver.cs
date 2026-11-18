using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>原版展开预览只在 Ready 创建一次遗物栏；随玩家模型事件刷新已打开的窗口。</summary>
public sealed class RemoteRelicPreviewObserver : IDisposable
{
    private readonly NMultiplayerPlayerExpandedState _screen;
    private readonly Player _player;
    private readonly Control _container;
    private readonly Action<NRelicBasicHolder> _released;
    private readonly Action _navigation;
    
    public RemoteRelicPreviewObserver(NMultiplayerPlayerExpandedState screen, Player player, Control container)
    {
        _screen = screen;
        _player = player;
        _container = container;
        _released = AccessTools.MethodDelegate<Action<NRelicBasicHolder>>(
            AccessTools.DeclaredMethod(typeof(NMultiplayerPlayerExpandedState), "OnRelicHolderReleased"), screen);
        _navigation = AccessTools.MethodDelegate<Action>(
            AccessTools.DeclaredMethod(typeof(NMultiplayerPlayerExpandedState), "UpdateNavigation"), screen);
        _player.RelicObtained += Refresh; // 订阅玩家模型的获得遗物事件
        _player.RelicRemoved += Refresh; // 订阅玩家模型的移除遗物事件
        _screen.TreeExiting += Dispose; // 生命周期：观察者与窗口同生共死
    }
    
    public void Dispose()
    {
        _player.RelicObtained -= Refresh;
        _player.RelicRemoved -= Refresh;
        _screen.TreeExiting -= Dispose;
    }
    
    /// <summary>订阅式刷新遗物栏。不是实时刷新、不关心具体遗物，而是根据玩家所有遗物全量重建。</summary>
    private void Refresh(RelicModel _)
    {
        if (!_screen.IsInsideTree() || _screen.IsQueuedForDeletion()) return;
        var focused = _screen.GetViewport().GuiGetFocusOwner(); // 视口当前 GUI 焦点 owner
        var hadFocus = focused != null && _container.IsAncestorOf(focused); // 遗物栏是否是当前焦点
        foreach (var child in _container.GetChildren())
        {
            _container.RemoveChild(child); child.QueueFree();
        }
        foreach (var relic in _player.Relics)
        {
            var holder = NRelicBasicHolder.Create(relic); // 模仿原版多人扩展状态栏中填充遗物栏的方法构造和装配。
            if (holder == null) continue;
            _container.AddChild(holder);
            holder.Connect(NClickableControl.SignalName.Released, Callable.From(_released));
            holder.MouseDefaultCursorShape = Control.CursorShape.Help;
        }
        _navigation(); // 重算焦点
        if (hadFocus && _container.GetChildCount() > 0) // 若重建前焦点在栏内、且现在至少有一个子节点
            _container.GetChild<Control>(0).GrabFocus(); // 就让第一个子节点grab 焦点
    }
}