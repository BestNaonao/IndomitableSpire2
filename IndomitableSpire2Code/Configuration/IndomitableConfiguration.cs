using BaseLib.Config;
using IndomitableSpire2.IndomitableSpire2Code.Registries;

namespace IndomitableSpire2.IndomitableSpire2Code.Configuration;

/// <summary>
/// 配置中心
/// </summary>
[ConfigHoverTipsByDefault]
public sealed class IndomitableConfiguration : SimpleModConfig
{
    /// <summary>
    /// 特殊卡牌展示对话开关
    /// </summary>
    public static bool PlayDialogue { get; set; } = true;
    
    /// <summary>
    /// 特殊卡牌播放语音开关
    /// </summary>
    public static bool PlaySoundEffects { get; set; } = true;
    
    /// <summary>
    /// 同种卡牌的特殊台词是否每场战斗仅播放一次
    /// </summary>
    private static bool _playSpecialLineOncePerCombat;
    
    public static bool PlaySpecialLineOncePerCombat
    {
        get => _playSpecialLineOncePerCombat;
        set
        {
            _playSpecialLineOncePerCombat = value;
            if (!value) SpecialLinesPlaybackRegistry.Reset();
        }
    }
}