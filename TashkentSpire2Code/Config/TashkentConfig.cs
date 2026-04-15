using BaseLib.Config;

namespace TashkentSpire2.TashkentSpire2Code.Config;

public enum FjordMosaicMode
{
    手部模型,
    腿部模型
}

[HoverTipsByDefault]
public sealed class TashkentConfig : SimpleModConfig
{
    public static FjordMosaicMode 多人模式使用哪种模型 { get; set; } = FjordMosaicMode.手部模型;
}