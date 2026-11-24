using System.Reflection;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>定位四种选人大厅的补丁目标，并刷新大厅中的不挠皮肤按钮。</summary>
internal static class SkinLobbyHelper
{
    private static readonly Type[] Screens =
        [typeof(NCharacterSelectScreen), typeof(NBlitzScreen), typeof(NDreadScreen), typeof(NCustomRunScreen)];
    
    internal static IEnumerable<MethodBase> ScreenMethods(string name) =>
        Screens.Select(screen => AccessTools.DeclaredMethod(screen, name));
    
    private static readonly AccessTools.FieldRef<NCharacterSelectButton, CharacterModel> CharacterField =
        AccessTools.FieldRefAccess<NCharacterSelectButton, CharacterModel>("_character");
    private static readonly AccessTools.FieldRef<NCharacterSelectButton, TextureRect> IconField =
        AccessTools.FieldRefAccess<NCharacterSelectButton, TextureRect>("_icon");
    
    private static readonly AccessTools.FieldRef<NDreadScreen, CharacterModel?> Rotating =
        AccessTools.FieldRefAccess<NDreadScreen, CharacterModel?>("_rotatingCharacter");
    private static readonly AccessTools.FieldRef<NDreadScreen, CharacterModel?> DuringAnimation =
        AccessTools.FieldRefAccess<NDreadScreen, CharacterModel?>("_rotatingDisplayCharacterOverride");
    private static readonly AccessTools.FieldRef<NDreadScreen, CharacterModel?> AfterAnimation =
        AccessTools.FieldRefAccess<NDreadScreen, CharacterModel?>("_rotatingPostAnimationCharacterSelect");
    private static readonly AccessTools.FieldRef<NDreadScreen, NCharacterSelectButton> RotationButton =
        AccessTools.FieldRefAccess<NDreadScreen, NCharacterSelectButton>("_rotatingCharacterButton");
    
    /// <summary>寻找指定容器下的代表不挠的角色选择按钮。</summary>
    internal static NCharacterSelectButton? FindFamilyButton(Node container) =>
        container.GetChildren().OfType<NCharacterSelectButton>().FirstOrDefault(button => button.Character is Indomitable);
    
    // 有待切换的目标时，轮换按钮仍可能显示上一位。断链还会在清除展示覆盖后提前选中普通按钮。这两条路径均须等 InitRotatingCharacterButton 完成。
    internal static bool IsWaitingForRotationVisuals(NDreadScreen screen, NCharacterSelectButton button) =>
        AfterAnimation(screen) != null && (button == RotationButton(screen) || DuringAnimation(screen) == null);
    
    /// <summary>原地替换普通按钮的模型，保留位置、焦点、选中状态和队友标记。</summary>
    internal static void RefreshFamilyButton(Node container, Indomitable skin)
    {
        if (FindFamilyButton(container) is not { } button || button.Character == skin)
            return;
        CharacterField(button) = skin;
        SetIcon(button, skin);
    }
    
    /// <summary>刷新屏幕上的皮肤选择按钮，并且对恐惧模式额外刷新轮换角色的按钮。</summary>
    internal static void RefreshSkinButtons(Node container, Indomitable skin)
    {
        RefreshFamilyButton(container, skin);
        if (container.GetAncestorOfType<NDreadScreen>() is { } dread)
            RefreshDreadSkinButtons(dread, skin);
    }
    
    // 将指定角色选择按钮的材质替换成对应皮肤的角色选择头像。
    private static void SetIcon(NCharacterSelectButton button, Indomitable skin) =>
        IconField(button).Texture = button.IsLocked ? skin.CharacterSelectLockedIcon : skin.CharacterSelectIcon;
    
    // 刷新恐惧模式的轮换角色按钮。
    private static void RefreshDreadSkinButtons(NDreadScreen screen, Indomitable skin)
    {
        if (Rotating(screen) is Indomitable) Rotating(screen) = skin;
        if (DuringAnimation(screen) is Indomitable) DuringAnimation(screen) = skin;
        if (AfterAnimation(screen) is Indomitable) AfterAnimation(screen) = skin;
        // 连胜动画期间先展示上一位，结束后才展示下一位。右侧按钮必须保留轮换身份。
        if ((DuringAnimation(screen) ?? Rotating(screen)) is Indomitable && RotationButton(screen).Visible)
            SetIcon(RotationButton(screen), skin);
    }
}