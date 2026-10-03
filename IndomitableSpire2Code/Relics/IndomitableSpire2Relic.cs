using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

/// <summary>
/// 负责统一处理 Mod 内定义的遗物的纹理资源。
/// </summary>
public abstract class IndomitableSpire2Relic : CustomRelicModel
{
    // 大图标（通常用于查看遗物大图和通关结算）
    protected override string BigIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    // 小图标
    public override string PackedIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PackedRelicTresPath();
    // 轮廓图
    protected override string PackedIconOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".OutlineRelicTresPath();
}