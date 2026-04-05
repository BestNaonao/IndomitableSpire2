using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class RetreatDynamicVar : DynamicVar
{
    public const string Key = "Tashkent_Retreat";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public RetreatDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}