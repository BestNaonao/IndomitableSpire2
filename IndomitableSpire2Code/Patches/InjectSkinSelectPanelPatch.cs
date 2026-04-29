using HarmonyLib;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class InjectSkinSelectPanelPatch
{
    private static SkinSelectPanel _panelInstance;

    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        // 巧妙利用我们设计的基类进行类型过滤
        if (characterModel is Indomitable)
        {
            if (!GodotObject.IsInstanceValid(_panelInstance))
            {
                var scene = ResourceLoader.Load<PackedScene>("res://IndomitableSpire2/scenes/screens/char_select/skin_select_panel.tscn");
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
            // 强制面板将其内部记忆的皮肤状态同步给大厅！
            _panelInstance.ShowAndSync(__instance);
        }
        else
        {
            // 点击别的角色时，优雅地隐藏该组件
            if (GodotObject.IsInstanceValid(_panelInstance))
            {
                _panelInstance.Visible = false;
            }
        }
    }
}