namespace IndomitableSpire2.IndomitableSpire2Code.Registries;

/// <summary>
/// 特殊台词播放的注册中心，用于管理和控制每种卡牌在战斗内是否播放特殊台词。
/// </summary>
public static class SpecialLinesPlaybackRegistry
{
    private static readonly Lock Sync = new();
    private static readonly HashSet<string> PlayedCardEntries = [];
    
    public static bool TryRecord(string cardEntry)
    {
        lock (Sync) return PlayedCardEntries.Add(cardEntry);
    }
    
    public static bool Contains(string cardEntry)
    {
        lock (Sync) return PlayedCardEntries.Contains(cardEntry);
    }
    
    public static void Reset()
    {
        lock (Sync) PlayedCardEntries.Clear();
    }
}