using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Extensions;

namespace TashkentSpire2.TashkentSpire2Code.Potions;

[Pool(typeof(TashkentPotionPool))]
public abstract class TashkentPotion : CustomPotionModel
{
    public override string CustomPackedImagePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PackedPotionImagePath();

    public override string CustomPackedOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres".PackedPotionImagePath();
}