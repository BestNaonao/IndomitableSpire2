using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public abstract class IndomitableEnchantment : CustomEnchantmentModel
{
    protected override string CustomIconPath => 
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".EnchantmentImagePath();
}