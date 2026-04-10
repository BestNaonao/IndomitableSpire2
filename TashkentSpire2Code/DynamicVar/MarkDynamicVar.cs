using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class MarkDynamicVar : DynamicVar
{
    public const string Key = "TashkentSpire2-Mark";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public MarkDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}