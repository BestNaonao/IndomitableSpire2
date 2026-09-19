using BaseLib.Config;

namespace TashkentSpire2.TashkentSpire2Code.Config;

public sealed class TashkentConfig : SimpleModConfig
{
    [ConfigHoverTip]
    public static bool EnableSpineModels { get; set; } = true;

    public static bool ReplaceHandWithLegInMultiplayer { get; set; } = false;

    public static bool SpawnAncientsSovetskySoyuz { get; set; } = true;
}
