using BaseLib.Abstracts;
using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Character;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

[Pool(typeof(IndomitableRelicPool))]
public abstract class IndomitableRelic : CustomRelicModel
{
    // public override string PackedIconPath
    // {
    //     get
    //     {
    //         var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    //         return ResourceLoader.Exists(path) ? path : "relic.png".RelicImagePath();
    //     }
    // }
    //
    // protected override string PackedIconOutlinePath
    // {
    //     get
    //     {
    //         var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    //         return ResourceLoader.Exists(path) ? path : "relic_outline.png".RelicImagePath();
    //     }
    // }
    //
    // protected override string BigIconPath
    // {
    //     get
    //     {
    //         var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    //         return ResourceLoader.Exists(path) ? path : "relic.png".BigRelicImagePath();
    //     }
    // }
}