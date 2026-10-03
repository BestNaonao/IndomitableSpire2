using BaseLib.Abstracts;
using BaseLib.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Potions;

/// <summary>
/// 负责统一处理 Mod 内定义的药水的纹理资源。
/// </summary>
public abstract class IndomitableSpire2Potion : CustomPotionModel
{
    public override string CustomPackedImagePath =>
        $"res://IndomitableSpire2/images/potions/packed/{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres";

    public override string CustomPackedOutlinePath =>
        $"res://IndomitableSpire2/images/potions/packed_outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres";
}