using BaseLib.Abstracts;
using BaseLib.Extensions;
using TashkentSpire2.TashkentSpire2Code.Extensions;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public abstract class TashkentPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}