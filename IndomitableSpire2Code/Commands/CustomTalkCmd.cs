using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

/// <summary>
/// 创建可自定义配置时长的对话气泡的指令
/// </summary>
public static class CustomTalkCmd
{
    /// <summary>
    /// 默认情况下，使用朴素字符计数计算持续时间并创建对话气泡命令。配置命令后调用 <see cref="CustomTalkCommand.Execute"/>。
    /// </summary>
    public static CustomTalkCommand Play(LocString line, Creature speaker, VfxColor vfxColor)
    {
        return new CustomTalkCommand(line, speaker, vfxColor);
    }
}

/// <summary>
/// 用于构建对话气泡的生成器，其基本持续时间可以支持原版的 <see cref="VfxDuration"/> 或精确的秒数，并可选择额外的持续时间。
/// </summary>
public sealed class CustomTalkCommand
{
    private const double MinimumDurationSeconds = 0.5d;
    private const double FastModeDurationReductionSeconds = 0.5d;
    private const double StandardSecondsPerCharacter = 0.12d;
    private const double FastSecondsPerCharacter = 0.1d;
    
    private readonly LocString _line;
    private readonly Creature _speaker;
    private readonly VfxColor _vfxColor;
    
    private VfxDuration? _vanillaDuration = VfxDuration.Custom;
    private double? _exactDurationSeconds;
    private double _additionalDurationSeconds;
    
    /// <summary>
    /// 最近一次执行时将持续时间转移到了对话气泡中。发言者死亡时为空，且对话气泡不会被生成。
    /// </summary>
    public double? ResolvedDurationSeconds { get; private set; }
    
    /// <summary>
    /// 最近一次执行创建的对话气泡。
    /// </summary>
    public NSpeechBubbleVfx? SpeechBubble { get; private set; }
    
    public CustomTalkCommand(LocString line, Creature speaker, VfxColor vfxColor)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(speaker);
        _line = line;
        _speaker = speaker;
        _vfxColor = vfxColor;
    }
    
    /// <summary>
    /// Uses a vanilla duration. <see cref="VfxDuration.Custom"/> retains the
    /// vanilla behavior of calculating the duration from the localized text length.
    /// </summary>
    public CustomTalkCommand WithDuration(VfxDuration duration)
    {
        ValidateDuration(duration);
        _vanillaDuration = duration;
        _exactDurationSeconds = null;
        return this;
    }
    
    /// <summary>
    /// Uses a vanilla duration and adds a fixed number of seconds to it.
    /// </summary>
    public CustomTalkCommand WithDuration(VfxDuration duration, double additionalSeconds)
    {
        ValidateSeconds(additionalSeconds, nameof(additionalSeconds));
        return WithDuration(duration).WithAdditionalDuration(additionalSeconds);
    }
    
    /// <summary>
    /// Uses an exact base duration. Unlike vanilla enum durations, this value is
    /// not shortened by Fast Mode. Values below the vanilla minimum of 0.5 seconds
    /// are raised to that minimum when the command executes.
    /// </summary>
    public CustomTalkCommand WithDuration(double seconds)
    {
        ValidateSeconds(seconds, nameof(seconds));
        _exactDurationSeconds = seconds;
        _vanillaDuration = null;
        return this;
    }
    
    /// <summary>
    /// Uses an exact base duration. This is the named equivalent of
    /// <see cref="WithDuration(double)"/> and retains the vanilla 0.5-second minimum.
    /// </summary>
    public CustomTalkCommand WithExactDuration(double seconds)
    {
        return WithDuration(seconds);
    }
    
    /// <summary>
    /// Adds a fixed number of seconds after resolving the selected base duration.
    /// Calling this method again replaces the previous additional duration.
    /// </summary>
    public CustomTalkCommand WithAdditionalDuration(double seconds)
    {
        ValidateSeconds(seconds, nameof(seconds));
        _additionalDurationSeconds = seconds;
        return this;
    }
    
    /// <summary>
    /// Creates and attaches the configured speech bubble, then returns this command
    /// so its resolved duration and resulting bubble can be inspected.
    /// </summary>
    public CustomTalkCommand Execute()
    {
        SpeechBubble = null;
        ResolvedDurationSeconds = null;
        
        if (_speaker.IsDead) return this;
        
        var formattedText = _line.GetFormattedText();
        var baseDurationSeconds = Math.Max(
            MinimumDurationSeconds,
            _exactDurationSeconds ?? ResolveVanillaDuration(
                _vanillaDuration ?? VfxDuration.Custom,
                formattedText));
        var durationSeconds = baseDurationSeconds + _additionalDurationSeconds;
        if (!double.IsFinite(durationSeconds))
            throw new InvalidOperationException("The resolved speech-bubble duration must be finite.");
        
        ResolvedDurationSeconds = durationSeconds;
        SpeechBubble = NSpeechBubbleVfx.Create(formattedText, _speaker, durationSeconds, _vfxColor);
        if (SpeechBubble is not null)
            _speaker.GetVfxContainer()?.AddChildSafely(SpeechBubble);
        return this;
    }
    
    private static double ResolveVanillaDuration(VfxDuration duration, string formattedText)
    {
        var fastMode = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast;
        if (duration == VfxDuration.Custom)
            return GetRawCharacterCount(formattedText) *
                   (fastMode ? FastSecondsPerCharacter : StandardSecondsPerCharacter);
        
        var durationSeconds = duration switch
        {
            VfxDuration.None => 0d,
            VfxDuration.VeryShort => 1d,
            VfxDuration.Short => 1.5d,
            VfxDuration.Standard => 1.75d,
            VfxDuration.Long => 2.25d,
            VfxDuration.VeryLong => 3d,
            VfxDuration.Forever => 999999999d,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Unsupported VFX duration.")
        };
        return fastMode ? durationSeconds - FastModeDurationReductionSeconds : durationSeconds;
    }
    
    private static int GetRawCharacterCount(string bbcodeText)
    {
        var textWithoutBbCode = Regex.Replace(bbcodeText, "\\[/?[^\\]]+\\]", "");
        return textWithoutBbCode.Replace("\n", "").Replace("\r", "").Replace(" ", "").Length;
    }
    
    private static void ValidateDuration(VfxDuration duration)
    {
        _ = duration switch
        {
            VfxDuration.None => duration,
            VfxDuration.VeryShort => duration,
            VfxDuration.Short => duration,
            VfxDuration.Standard => duration,
            VfxDuration.Long => duration,
            VfxDuration.VeryLong => duration,
            VfxDuration.Custom => duration,
            VfxDuration.Forever => duration,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Unsupported VFX duration."),
        };
    }
    
    private static void ValidateSeconds(double seconds, string paramName)
    {
        if (!double.IsFinite(seconds) || seconds < 0d)
        {
            throw new ArgumentOutOfRangeException(paramName, seconds, 
                "Duration must be a finite, non-negative number of seconds.");
        }
    }
}