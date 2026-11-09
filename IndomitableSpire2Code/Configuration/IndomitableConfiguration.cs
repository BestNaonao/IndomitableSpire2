using BaseLib.Config;
using IndomitableSpire2.IndomitableSpire2Code.Patches;
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
    /// 特殊卡牌播放专属音频的开关；关闭后，第 0 次结算仍可播放默认动画音效。
    /// </summary>
    public static bool PlaySoundEffects { get; set; } = true;
    
    /// <summary>
    /// 仅在本机按玩家分别管理卡牌特殊语音；关闭时所有不挠语音仍共用一个播放通道。
    /// </summary>
    private static bool _isolateSpecialVoiceByPlayer = true;
    
    public static bool IsolateSpecialVoiceByPlayer
    {
        get => _isolateSpecialVoiceByPlayer;
        set
        {
            if (_isolateSpecialVoiceByPlayer == value) return;
            _isolateSpecialVoiceByPlayer = value;
            // 运行中改动设置时，先清掉旧通道，下一句语音按新规则播放。
            IndomitableVoice.Stop();
        }
    }
    
    /// <summary>
    /// 启用与角色皮肤匹配的卡牌特殊动作；关闭时使用普通出牌动作，不影响战后胜利动画。
    /// </summary>
    public static bool PlaySpecialCardAnimations { get; set; } = true;
    
    /// <summary>舰载机特殊飞行演出；不影响一次 Attack 动作、命中特效、音效和伤害。</summary>
    public static bool PlayAircraftFlightAnimations { get; set; } = true;
    
    /// <summary>
    /// 玩家催眠爆发时是否播放特殊动画，仅影响视觉表现。
    /// </summary>
    public static bool PlayHypnotizedAnimation { get; set; } = true;
    
    /// <summary>连续保持 normal 多久后播放一次 motou（秒）；0 关闭自动播放，仅影响本机。</summary>
    [ConfigSlider(0, 300, 1)]
    public static int IdleAnimationDelaySeconds { get; set; } = 30;
    
    /// <summary>收到或发出 EndTurnPing 时，在本机为对应的存活不挠播放一次 motou。</summary>
    public static bool PlayIdleAnimationOnEndTurnPing { get; set; } = true;
    
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
    
    /// <summary>每次福袋触发时读取房主的规则；客户端的本地偏好不会覆盖房主。</summary>
    public static LuckyBagRelicRule LuckyBagRelicRule { get; set; } = LuckyBagRelicRule.Direct;
    
    /// <summary>只控制本机确认框；联机确认行动与节点冲突检查始终执行。</summary>
    public static bool ConfirmVacationProposal { get; set; } = true;
}