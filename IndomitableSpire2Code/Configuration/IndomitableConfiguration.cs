using BaseLib.Config;

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
}
