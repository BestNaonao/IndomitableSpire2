using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public abstract class IndomitablePower : CustomPowerModel
{
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
    public override string CustomPackedIconPath => $"packed/{Id.Entry.RemovePrefix().ToLowerInvariant()}_packed.tres".PowerImagePath();
}