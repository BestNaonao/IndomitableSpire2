using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Rewards;

public static class AllyRewardPreviewIcons
{
    // 顶栏金币原始图集区域 81×75（含 margin 为 90×77），高于奖励列表的 64×64 版本。
    public const string Gold = "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_gold.tres";
    // 原版能力的 256×256 原图，而非 power_atlas 中的 48/64 像素小图。
    public const string Strength = "res://images/powers/strength_power.png";
    public const string Dexterity = "res://images/powers/dexterity_power.png";
    // 复用原版 256×256 的抽牌图案，仅表示抽牌奖励，不表示延迟到下回合。
    public const string DrawCards = "res://images/powers/draw_cards_next_turn_power.png";
    // 搜刮/战后奖励界面使用的 128×128 卡牌奖励原图；数量表示奖励次数，不是候选牌数。
    public const string CardReward = "res://images/ui/reward_screen/reward_icon_card.png";
    
    public static string Energy(Player recipient)
    {
        // 按收奖者角色取图。不挠的 BigEnergyIconPath 指向 256×256 Original；
        // 原版角色则使用其 74×74 的原始能量图集，不用 sprite_fonts 的 24×24 文本图。
        var pool = recipient.Character.CardPool;
        return pool is CustomCardPoolModel custom && !string.IsNullOrEmpty(custom.BigEnergyIconPath)
            ? custom.BigEnergyIconPath : pool.EnergyIconPath;
    }
    
    /// <summary>其他能力奖励可复用此方法，也可直接在条目中提供自定义高分辨率图标路径。</summary>
    public static string Power(PowerModel power) => 
        power is CustomPowerModel custom && !string.IsNullOrEmpty(custom.CustomBigIconPath) 
            ? custom.CustomBigIconPath : $"res://images/powers/{power.Id.Entry.ToLowerInvariant()}.png";
}