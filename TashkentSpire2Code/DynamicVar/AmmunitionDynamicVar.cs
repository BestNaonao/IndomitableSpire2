using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code;

public class AmmunitionDynamicVar : DynamicVar
{
    public const string Key = "Tashkent_Ammunition";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public AmmunitionDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}