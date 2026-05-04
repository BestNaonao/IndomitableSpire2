using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class ShotDynamicVar : DynamicVar
{
    public const string Key = "TashkentSpire2-Shot";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public ShotDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}