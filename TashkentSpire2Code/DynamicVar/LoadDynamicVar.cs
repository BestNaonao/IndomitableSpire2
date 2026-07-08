using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class LoadDynamicVar : DynamicVar
{
    public const string Key = "TashkentSpire2-Load";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public LoadDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        //this.WithTooltip(LocKey);
    }
}