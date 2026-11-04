using Godot;
using FileAccess = Godot.FileAccess;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.UI;

/// <summary>Resource/localization contract shared with the standalone Godot view.</summary>
public static class TashkentBattleRewardsAssets
{
    public const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_battle_rewards.tscn";
    public const string ButtonPath = "res://TashkentSpire2/images/ui/battle_rewards/proceed_button.svg";
    public const string OutlinePath = "res://TashkentSpire2/images/ui/battle_rewards/proceed_outline.svg";
    public const string LocPrefix = "TASHKENTSPIRE2-BATTLE_REWARDS.";
    public static readonly Vector2 RewardOrigin = new(240f, 226f);
    public const float RewardWindowScale = 0.96f;
    public static readonly string[] PaintingPaths = [
        "res://TashkentSpire2/images/ui/battle_rewards/tashigan_painting.png",
        "res://TashkentSpire2/images/ui/battle_rewards/tashigan_2_painting.png",
        "res://TashkentSpire2/images/ui/battle_rewards/tashigan_3_painting.png",
        "res://TashkentSpire2/images/ui/battle_rewards/tashigan_4_full_painting.png"
    ];

    public static Dictionary<string, string> LoadPreviewStrings(string language)
    {
        string path = $"res://TashkentSpire2/localization/{language}/gameplay_ui.json";
        if (!FileAccess.FileExists(path))
            path = "res://TashkentSpire2/localization/eng/gameplay_ui.json";
        return Json.ParseString(FileAccess.GetFileAsString(path)).AsGodotDictionary()
            .ToDictionary(pair => pair.Key.AsString(), pair => pair.Value.AsString());
    }
}
