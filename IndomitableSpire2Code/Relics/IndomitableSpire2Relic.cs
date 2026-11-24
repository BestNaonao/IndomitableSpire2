using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

/// <summary>在原版极速模式的遗物黑名单中登记此遗物的无状态接口。</summary>
internal interface IExcludedFromBlitz;

/// <summary>
/// 负责统一处理 Mod 内定义的遗物的纹理资源和极速模式资格。
/// </summary>
public abstract class IndomitableSpire2Relic : CustomRelicModel
{
    protected IndomitableSpire2Relic()
    {
        if (this is IExcludedFromBlitz) RelicLists.notAllowedInBlitz.Add(Id); // 原型构造时 Id 已根据运行时类型确定
    }
    
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