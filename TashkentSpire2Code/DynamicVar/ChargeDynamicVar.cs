using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class ChargeDynamicVar : DynamicVar
{
    public const string Key = "TashkentSpire2-Charge";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public ChargeDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}