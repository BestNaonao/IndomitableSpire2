using BaseLib.Config;

namespace TashkentSpire2.TashkentSpire2Code.Config;

public enum FjordMosaicMode
{
    Hands,
    Feet
}

[HoverTipsByDefault]
public sealed class TashkentConfig : SimpleModConfig
{
    public static FjordMosaicMode MultiplayerModeModel { get; set; } = FjordMosaicMode.Hands;
}