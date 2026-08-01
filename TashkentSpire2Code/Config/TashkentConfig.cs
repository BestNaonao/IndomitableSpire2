using BaseLib.Config;

namespace TashkentSpire2.TashkentSpire2Code.Config;

public sealed class TashkentConfig : SimpleModConfig
{
    public static bool ReplaceHandWithLegInMultiplayer { get; set; } = false;

    public static bool SpawnAncientsSovetskySoyuz { get; set; } = true;
}