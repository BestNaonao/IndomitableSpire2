using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class TorpedoDynamicVar : DynamicVar
{
    public const string Key = "Tashkent_Torpedo";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public TorpedoDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}