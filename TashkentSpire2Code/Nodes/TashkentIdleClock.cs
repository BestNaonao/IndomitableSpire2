namespace TashkentSpire2.TashkentSpire2Code.Nodes;

/// <summary>Presentation time only: never consumes combat RNG or changes model state.</summary>
internal sealed class TashkentIdleClock
{
    internal const double DelaySeconds = 10d;
    private double _elapsed;

    internal void Reset() => _elapsed = 0d;

    internal bool Advance(double delta, bool eligible)
    {
        if (!eligible)
        {
            Reset();
            return false;
        }
        _elapsed += Math.Max(0d, delta);
        if (_elapsed + 0.000001d < DelaySeconds)
            return false;
        Reset();
        return true;
    }
}
