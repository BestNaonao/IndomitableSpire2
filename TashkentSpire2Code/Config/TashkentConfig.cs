using BaseLib.Config;

namespace TashkentSpire2.TashkentSpire2Code.Config;

public sealed class TashkentConfig : SimpleModConfig
{
    [ConfigHideInUI, ConfigIgnoreRestoreDefaults]
    public static string LastPlayedSkinId { get; set; } = "";

    [ConfigHoverTip]
    public static bool EnableSpineModels { get; set; } = true;

    [ConfigHoverTip]
    public static bool EnableCustomBattleRewards { get; set; } = true;

    public static bool ReplaceHandWithLegInMultiplayer { get; set; } = false;

    public static bool SpawnAncientsSovetskySoyuz { get; set; } = true;
}
