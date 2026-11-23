using BaseLib.Patches.UI;
using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public enum IndomitableSkin
{
    Default,
    Maid,
    RaceQueen
}

/// <summary>
/// 皮肤定义类：完全剥离视觉资产与玩法数据
/// </summary>
public class SkinDefinition
{
    public IndomitableSkin Id { get; init; }
    public Color MapDrawingColor { get; init; }
    public string VisualPath { get; init; } = "";
    public string RestSiteAnimPath { get; init; } = "";
    public string MerchantAnimPath { get; init; } = "";
    public string IconTexturePath { get; init; } = "";
    public string IconPath { get; init; } = "";
    public string IconOutlineTexturePath { get; init; } = "";
    public string ArmPaperTexturePath { get; init; } = "";
    public string ArmRockTexturePath { get; init; } = "";
    public string ArmScissorsTexturePath { get; init; } = "";
    public string CharacterSelectSfx { get; init; } = "";
    public string CharacterSelectIconPath { get; init; } = "";
    public Func<string> ArmPointingTexturePathFunc { get; init; } = () => "";
    public RelicIconData YummyCookieIconData { get; init; } = new(
        "res://IndomitableSpire2/images/relics/big/yummy_cookie_indomitable_default.png",
        "res://IndomitableSpire2/images/relics/packed/yummy_cookie_indomitable_default.tres",
        "res://IndomitableSpire2/images/relics/outline/yummy_cookie_indomitable_default.tres");
}

public static class SkinManager
{
    // 1. 皮肤资产配置表 (阶段一的内容)
    private static readonly Dictionary<IndomitableSkin, SkinDefinition> Skins = new()
    {
        [IndomitableSkin.Default] = new SkinDefinition
        {
            Id = IndomitableSkin.Default,
            MapDrawingColor = Colors.FloralWhite,
            VisualPath = "res://IndomitableSpire2/scenes/characters/indomitable.tscn",
            RestSiteAnimPath = "res://IndomitableSpire2/scenes/characters/indomitable_rest_site.tscn",
            MerchantAnimPath = "res://IndomitableSpire2/scenes/merchant/indomitable_merchant.tscn",
            IconTexturePath = "res://IndomitableSpire2/images/charui/character_icon_indomitable.png",
            IconPath = "res://IndomitableSpire2/scenes/characters/indomitable_icon.tscn",
            IconOutlineTexturePath = "res://IndomitableSpire2/images/ui/character_icon_indomitable_outline.png",
            ArmPaperTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_paper.png",
            ArmRockTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_rock.png",
            ArmScissorsTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_scissors.png",
            CharacterSelectSfx = "res://IndomitableSpire2/sfx/characters/indomitable/chumotaici_ex.wav",
            CharacterSelectIconPath = "res://IndomitableSpire2/images/charui/char_select_indomitable.png",
            ArmPointingTexturePathFunc = () => $"res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_point{new Random().Next(1, 4)}.png", 
            YummyCookieIconData = new RelicIconData(
                "res://IndomitableSpire2/images/relics/big/yummy_cookie_indomitable_default.png",
                "res://IndomitableSpire2/images/relics/packed/yummy_cookie_indomitable_default.tres",
                "res://IndomitableSpire2/images/relics/outline/yummy_cookie_indomitable_default.tres")
        },
        [IndomitableSkin.Maid] = new SkinDefinition
        {
            Id = IndomitableSkin.Maid,
            MapDrawingColor = Colors.Black,
            VisualPath = "res://IndomitableSpire2/scenes/characters/indomitable_maid.tscn",
            RestSiteAnimPath = "res://IndomitableSpire2/scenes/characters/indomitable_maid_rest_site.tscn",
            MerchantAnimPath = "res://IndomitableSpire2/scenes/merchant/indomitable_maid_merchant.tscn",
            IconTexturePath = "res://IndomitableSpire2/images/charui/character_icon_indomitable_maid.png",
            IconPath = "res://IndomitableSpire2/scenes/characters/indomitable_maid_icon.tscn",
            IconOutlineTexturePath = "res://IndomitableSpire2/images/ui/character_icon_indomitable_maid_outline.png",
            ArmPaperTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_maid_paper.png",
            ArmRockTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_maid_rock.png",
            ArmScissorsTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_maid_scissors.png",
            CharacterSelectSfx = "res://IndomitableSpire2/sfx/characters/indomitable/maid_pifumiaoshu.wav",
            CharacterSelectIconPath = "res://IndomitableSpire2/images/charui/char_select_indomitable_maid.png",
            ArmPointingTexturePathFunc = () => $"res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_maid_point{new Random().Next(1, 4)}.png",
            YummyCookieIconData = new RelicIconData(
                "res://IndomitableSpire2/images/relics/big/yummy_cookie_indomitable_maid.png",
                "res://IndomitableSpire2/images/relics/packed/yummy_cookie_indomitable_maid.tres",
                "res://IndomitableSpire2/images/relics/outline/yummy_cookie_indomitable_maid.tres")
        },
        [IndomitableSkin.RaceQueen] = new SkinDefinition
        {
            Id = IndomitableSkin.RaceQueen,
            MapDrawingColor = Colors.HotPink,
            VisualPath = "res://IndomitableSpire2/scenes/characters/indomitable_race_queen.tscn",
            RestSiteAnimPath = "res://IndomitableSpire2/scenes/characters/indomitable_race_queen_rest_site.tscn",
            MerchantAnimPath = "res://IndomitableSpire2/scenes/merchant/indomitable_race_queen_merchant.tscn",
            IconTexturePath = "res://IndomitableSpire2/images/charui/character_icon_indomitable_race_queen.png",
            IconPath = "res://IndomitableSpire2/scenes/characters/indomitable_race_queen_icon.tscn",
            IconOutlineTexturePath = "res://IndomitableSpire2/images/ui/character_icon_indomitable_race_queen_outline.png",
            ArmPaperTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_paper.png",
            ArmRockTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_rock.png",
            ArmScissorsTexturePath = "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_scissors.png",
            CharacterSelectSfx = "res://IndomitableSpire2/sfx/characters/indomitable/login_2.wav",
            CharacterSelectIconPath = "res://IndomitableSpire2/images/charui/char_select_indomitable_race_queen.png",
            ArmPointingTexturePathFunc = () => $"res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_point{new Random().Next(1, 4)}.png",
            YummyCookieIconData = new RelicIconData(
                "res://IndomitableSpire2/images/relics/big/yummy_cookie_indomitable_race_queen.png",
                "res://IndomitableSpire2/images/relics/packed/yummy_cookie_indomitable_race_queen.tres",
                "res://IndomitableSpire2/images/relics/outline/yummy_cookie_indomitable_race_queen.tres")
        }   // 暂时没有新的手模，暂时用默认的替代
    };
    
    public static SkinDefinition GetDefinition(IndomitableSkin skin) => Skins[skin];
    
    // ==========================================
    // 2. 本地状态管理 (阶段二的核心)
    // ==========================================
    
    // // 记录每个玩家 (NetId) 正在使用的皮肤
    // public static readonly Dictionary<ulong, IndomitableSkin> PlayerSkins = new();
    //
    // // 获取特定玩家的皮肤
    // public static IndomitableSkin GetPlayerSkin(ulong? netId) => !netId.HasValue
    //     ? IndomitableSkin.Default : PlayerSkins.GetValueOrDefault(netId.Value, IndomitableSkin.Default);
}