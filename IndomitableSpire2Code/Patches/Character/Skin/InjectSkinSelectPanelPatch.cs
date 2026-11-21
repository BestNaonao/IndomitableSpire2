using System.Reflection;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>负责皮肤选择面板的纯 UI 注入。消费皮肤偏好选择补丁注入的角色模型，这里只负责显示当前选择的皮肤模型。</summary>
[HarmonyPatch]
public static class InjectSkinSelectPanelPatch
{
    private const string ScenePath = "res://IndomitableSpire2/scenes/screens/char_select/skin_select_panel.tscn";
    
    // 目标是四个模式界面的原版的角色选择方法。
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyScreens.Methods(nameof(ICharacterSelectButtonDelegate.SelectCharacter));
    
    // 完成原版选择角色（包括同步）之后再插入皮肤选择面板。
    [HarmonyPostfix]
    private static void ShowPanel(Control __instance, NCharacterSelectButton charSelectButton, StartRunLobby ____lobby)
    {
        var compact = __instance is NCustomRunScreen; // 自定义模式采用迷你布局。
        // 自定义模式和恐惧模式没有 InfoPanel，直接覆盖在界面本体上的空闲区域中。
        var overlay = compact || __instance is NDreadScreen;
        var host = overlay ? __instance : __instance.GetNode<Control>("%InfoPanel");
        var panel = host.GetNodeOrNull<SkinSelectPanel>("SkinSelectPanel"); // 没有面板就创建，否则复用旧面板。
        // 守卫条件，按钮锁定或者角色不是不挠时隐藏面板。
        if (charSelectButton.IsLocked || charSelectButton.Character is not Indomitable ||
            ____lobby.LocalPlayer.character is not Indomitable selected)
        {
            panel?.Hide();
            return;
        }
        if (panel == null)
        {
            panel = GD.Load<PackedScene>(ScenePath).Instantiate<SkinSelectPanel>(); // 面板的懒加载
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            host.AddChild(panel);
            if (overlay)
            {
                // 自定义大厅左侧玩家列表旁和恐惧大厅右侧各有一块预览空间。
                panel.SetAnchorsAndOffsetsPreset(compact ? Control.LayoutPreset.TopLeft : Control.LayoutPreset.CenterRight);
                panel.Position += compact ? new Vector2(540, 306) : new Vector2(-786, -171);
                var content = panel.GetNode<Control>("VBoxContainer");
                content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
                content.Position = Vector2.Zero;
                content.Scale = Vector2.One * (compact ? 0.5f : 1.0f);
            }
            else
                panel.Position = new Vector2(400, 0);
        }
        panel.ShowSelection(____lobby, charSelectButton, selected);
    }
}