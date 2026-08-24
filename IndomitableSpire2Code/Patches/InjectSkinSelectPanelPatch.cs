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
    /// 1：选中角色时，实例化并显示皮肤面板
    /// </summary>
    [HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void SelectCharacterPostfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        // 巧妙利用我们设计的基类进行类型过滤
        if (characterModel is Indomitable)
        {
            if (!GodotObject.IsInstanceValid(_panelInstance))
            {
                var scene = ResourceLoader.Load<PackedScene>(ScenePath);
                _panelInstance = scene.Instantiate<SkinSelectPanel>();
                
                // 将 UI 挂载在 InfoPanel 内，使其跟随左侧的文字描述面板
                var infoPanel = __instance.GetNodeOrNull<Control>("%InfoPanel");
                if (infoPanel != null)
                {
                    infoPanel.AddChildSafely(_panelInstance);
                    // 调整挂载后的坐标（可能需要根据实际渲染效果微调）
                    _panelInstance.Position = new Vector2(400, 0); 
                }
            }
            
            // 每次切到角色时，确保面板是可交互的（防止之前点过准备卡在不可交互状态），并将其内部记忆的皮肤状态同步给大厅
            _panelInstance.SetInteractable(true);
            _panelInstance.ShowAndSync(__instance);
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
}