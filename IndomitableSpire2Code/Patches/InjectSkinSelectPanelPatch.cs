using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public static class InjectSkinSelectPanelPatch
{
    private static SkinSelectPanel? _panelInstance;
    private const string ScenePath = "res://IndomitableSpire2/scenes/screens/char_select/skin_select_panel.tscn";
    
    /// <summary>
    /// 0：选中角色前，先暂停播放进行中的不挠的语音
    /// </summary>
    [HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPrefix]
    public static void SelectCharacterPrefix() => IndomitableVoice.Stop();
    
    /// <summary>
    /// 1：选中角色时，实例化并显示皮肤面板
    /// </summary>
    [HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void SelectCharacterPostfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        if (characterModel is Indomitable selectedSkin)
        {
            // 确保 SkinSelectPanel 挂载在 InfoPanel 下。
            var infoPanel = __instance.GetNodeOrNull<Control>("%InfoPanel");
            if (infoPanel is null) return;
            if (!GodotObject.IsInstanceValid(_panelInstance) || _panelInstance.GetParent() != infoPanel)
            {
                if (GodotObject.IsInstanceValid(_panelInstance))
                    _panelInstance.QueueFreeSafely();
                // 读取我们写好的自定义场景
                var scene = ResourceLoader.Load<PackedScene>(ScenePath);
                if (scene is null) return;
                // 实例化场景，将 UI 挂载在 InfoPanel 内，使其跟随左侧的文字描述面板
                _panelInstance = scene.Instantiate<SkinSelectPanel>();
                infoPanel.AddChildSafely(_panelInstance);
                _panelInstance.Position = new Vector2(400f, 0f);
            }
            // 每次切到角色时，确保面板是可交互的（防止之前点过准备卡在不可交互状态），并将其内部记忆的皮肤状态同步给大厅
            _panelInstance.SetInteractable(true);
            _panelInstance.ShowAndSync(__instance, selectedSkin);
        }
        // 点击别的角色时，优雅地隐藏该组件
        else if (GodotObject.IsInstanceValid(_panelInstance)) 
            _panelInstance.Visible = false;
    }
    
    /// <summary>
    /// 2：玩家点击“准备 (Embark)”时，禁用皮肤切换
    /// </summary>
    [HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnEmbarkPressed))]
    [HarmonyPostfix]
    public static void OnEmbarkPressedPostfix(NCharacterSelectScreen __instance)
    {
        if (GodotObject.IsInstanceValid(_panelInstance)) _panelInstance.SetInteractable(false);
    }
    
    /// <summary>
    /// 3：玩家点击“取消准备 (Unready)”时，恢复皮肤切换
    /// </summary>
    [HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnUnreadyPressed))]
    [HarmonyPostfix]
    public static void OnUnreadyPressedPostfix(NCharacterSelectScreen __instance)
    {
        if (GodotObject.IsInstanceValid(_panelInstance)) _panelInstance.SetInteractable(true);
    }
    
    // 退出选择角色界面时也应该中止语音的播放
    [HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuClosed))]
    [HarmonyPrefix]
    public static void OnSubmenuClosedPrefix() => IndomitableVoice.Stop();
}