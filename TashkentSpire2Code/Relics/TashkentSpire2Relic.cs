using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Extensions;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

// [Pool(typeof(IndomitableRelicPool))]
// public abstract class IndomitableSpire2Relic : CustomRelicModel
// {
//     public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
//
//     protected override string PackedIconOutlinePath =>
//         $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
//
//     protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
// }