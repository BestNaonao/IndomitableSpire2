using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>只在一次原版调用期间借用按钮的模型。退出调用后，按钮继续标识同一个不挠角色。</summary>
internal readonly record struct SkinButtonModel(NCharacterSelectButton Button, CharacterModel Original)
{
    private static readonly AccessTools.FieldRef<NCharacterSelectButton, CharacterModel> CharacterField =
        AccessTools.FieldRefAccess<NCharacterSelectButton, CharacterModel>("_character");
    
    /// <summary>借用的核心原语。替换按钮的角色模型，并返回一张记录性质的“借条”。</summary>
    internal static SkinButtonModel? Replace(NCharacterSelectButton button, CharacterModel skin)
    {
        if (button.Character == skin) return null; // 没借不还，天然安全可重入
        var previous = new SkinButtonModel(button, button.Character);
        CharacterField(button) = skin;
        return previous;
    }
    
    /// <summary>归还的核心原语。</summary>
    internal void Restore() => CharacterField(Button) = Original;
}