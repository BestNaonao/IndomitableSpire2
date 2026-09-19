using MegaCrit.Sts2.Core.Localization.DynamicVars;

using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace TashkentSpire2.TashkentSpire2Code;

public class LoadDynamicVar : DynamicVar
{
    public const string Key = "TashkentSpire2-Load";

    public static readonly string LocKey = Key.ToUpperInvariant();

    public LoadDynamicVar(decimal baseValue) : base(Key, baseValue)
    {
        //this.WithTooltip(LocKey);
    }

    public static IHoverTip GetHoverTip()
    {
        return new HoverTip(
            new LocString("static_hover_tips", $"{LocKey}.title"),
            new LocString("static_hover_tips", $"{LocKey}.description"));
    }
}
