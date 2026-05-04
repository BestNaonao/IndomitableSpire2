using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

[Pool(typeof(IndomitableRelicPool))]
public abstract class IndomitableRelic : CustomRelicModel
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