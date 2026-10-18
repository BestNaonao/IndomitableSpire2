using System.Globalization;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization;

/// <summary>
/// {aircraftPrefix:strikeFighterIcons()} 默认一枚；{Amount:strikeFighterIcons()} 读取数量，
/// {任意已注册变量:strikeFighterIcons(2)} 显式指定数量。五种 formatter 语法相同。
/// </summary>
public abstract class AircraftIconsFormatter(string formatterName) : IAutoRegisterFormatSpecifier
{
    public string Name { get; set; } = formatterName;
    public bool CanAutoDetect { get; set; }
    
    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        int count;
        if (!string.IsNullOrEmpty(formattingInfo.FormatterOptions))
        {
            if (!int.TryParse(formattingInfo.FormatterOptions, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out count)) 
                return false;
        }
        else
        {
            count = formattingInfo.CurrentValue switch
            {
                CalculatedVar variable => Convert.ToInt32(variable.Calculate(null)),
                DynamicVar variable => Convert.ToInt32(variable.PreviewValue),
                int value => value,
                decimal value => Convert.ToInt32(value),
                // Prefix 与能量格式保持兼容；无数量时默认一枚。
                _ => 1
            };
        }
        var icon = AircraftIcons.All.Single(icon => icon.FormatterName == Name);
        formattingInfo.Write(AircraftIcons.Inline(icon, count));
        return true;
    }
}

public sealed class CarrierAircraftIconsFormatter() : AircraftIconsFormatter("carrierAircraftIcons");
public sealed class StrikeFighterIconsFormatter() : AircraftIconsFormatter("strikeFighterIcons");
public sealed class TorpedoBomberIconsFormatter() : AircraftIconsFormatter("torpedoBomberIcons");
public sealed class DiveBomberIconsFormatter() : AircraftIconsFormatter("diveBomberIcons");
public sealed class LevelBomberIconsFormatter() : AircraftIconsFormatter("levelBomberIcons");

/// <summary>
/// 仅为缺失的 aircraftPrefix 提供默认前缀。由 LocManager 的 formatter 初始化补丁注册，
/// 因此无需经过 LocString.GetFormattedText（该方法可能已被调用方内联）。
/// </summary>
public sealed class AircraftIconPrefixSource : ISource
{
    public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        if (selectorInfo.SelectorIndex != 0 || selectorInfo.SelectorText != "aircraftPrefix" ||
            selectorInfo.CurrentValue is not IReadOnlyDictionary<string, object> variables ||
            variables.ContainsKey("aircraftPrefix"))
            return false;
        
        selectorInfo.Result = string.Empty;
        return true;
    }
}