using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PlayerExtensions
{
    /// <summary>
    /// 获取玩家干劲的快捷方法
    /// </summary>
    public static int GetMotivationAmount(this Player player) => 
        player.Creature.GetPower<MotivationPower>()?.DisplayAmount ?? 0;

    /// <summary>
    /// 仅为不挠（含所有皮肤）播放卡牌台词和语音，不阻塞卡牌结算。
    /// </summary>
    /// <param name="player">打出卡牌的玩家。</param>
    /// <param name="cardPlay">单次打出卡牌的实例。</param>
    /// <param name="cardEntry">卡牌 Id.Entry，读取 cards 表的 {cardEntry}.banter；null 时不显示对话。</param>
    /// <param name="vfxColor">对话气泡颜色；null 时不显示对话。</param>
    /// <param name="sfxPath">传给 SfxCmd 的音频路径；null 时不播放语音。</param>
    /// <param name="volume">音量，默认 1。</param>
    /// <param name="duration">原版时长类型，默认按台词长度计算。</param>
    /// <param name="exactDurationSeconds">精确基础秒数；非 null 时优先于 duration，且不受快速模式缩短。</param>
    /// <param name="additionalDurationSeconds">基础时长之外追加的秒数。</param>
    public static void PlayIndomitableCardBanter(
        this Player player,
        CardPlay cardPlay,
        string? cardEntry = null,
        VfxColor? vfxColor = VfxColor.Gold,
        string? sfxPath = null,
        float volume = 1f,
        VfxDuration duration = VfxDuration.Custom,
        double? exactDurationSeconds = null,
        double additionalDurationSeconds = 0d)
    {
        if (player.Character is not Indomitable || cardPlay.PlayIndex != 0) return;
        // 参数中条目和颜色不为空时播放台词
        if (cardEntry != null && vfxColor is { } color)
        {
            var talk = CustomTalkCmd.Play(new LocString("cards", $"{cardEntry}.banter"), player.Creature, color);
            if (exactDurationSeconds is { } seconds)
                talk.WithExactDuration(seconds);
            else
                talk.WithDuration(duration);
            talk.WithAdditionalDuration(additionalDurationSeconds).Execute();
        }
        // 参数中音频路径不为空时播放语音
        if (sfxPath != null)
            SfxCmd.Play(sfxPath, volume);
    }
}