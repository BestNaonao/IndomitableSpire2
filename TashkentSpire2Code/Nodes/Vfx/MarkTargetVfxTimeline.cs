namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Cosmetic timing only. Fire Control replaces/suppresses ordinary mark pulses.</summary>
internal sealed class MarkTargetVfxTimeline
{
    internal const float MarkDuration = 0.42f;
    internal const float FireControlDuration = 0.9f;
    private float _markAge = MarkDuration;
    private float _fireControlAge = FireControlDuration;

    internal bool IsFireControlActive => _fireControlAge < FireControlDuration;
    internal bool IsActive => IsFireControlActive || _markAge < MarkDuration;

    internal void PlayMarkAdded()
    {
        if (!IsFireControlActive)
            _markAge = 0f;
    }

    internal void PlayFireControl()
    {
        _markAge = MarkDuration;
        _fireControlAge = 0f;
    }

    internal void Advance(float delta)
    {
        delta = Math.Max(0f, delta);
        _markAge = Math.Min(MarkDuration, _markAge + delta);
        _fireControlAge = Math.Min(FireControlDuration, _fireControlAge + delta);
    }

    internal void Clear()
    {
        _markAge = MarkDuration;
        _fireControlAge = FireControlDuration;
    }

    internal float Scale
    {
        get
        {
            float progress = IsFireControlActive
                ? Math.Clamp(_fireControlAge / 0.28f, 0f, 1f)
                : Math.Clamp(_markAge / MarkDuration, 0f, 1f);
            float remainder = 1f - progress;
            return 1f + (IsFireControlActive ? 0.85f : 0.55f) * remainder * remainder * remainder;
        }
    }

    internal float Opacity(bool hasMark)
    {
        float resting = hasMark ? 0.16f : 0f;
        if (IsFireControlActive)
        {
            float fade = Math.Clamp((FireControlDuration - _fireControlAge) / 0.3f, 0f, 1f);
            float confirmation = 0.82f + 0.12f * MathF.Sin(_fireControlAge * MathF.PI * 12f);
            return resting + (confirmation - resting) * fade;
        }

        float pulse = 1f - Math.Clamp(_markAge / MarkDuration, 0f, 1f);
        return resting + (0.64f - resting) * pulse * pulse;
    }
}
