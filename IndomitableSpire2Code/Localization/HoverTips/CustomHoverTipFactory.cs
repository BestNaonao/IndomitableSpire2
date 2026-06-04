using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;

public static class CustomHoverTipFactory
{
    /// <summary>
    /// 获取任意 Intent 的 HoverTip。
    /// 每次调用生成新实例以保证 Godot 资源的安全性，但反射和文本拼接过程通过泛型缓存实现了零开销。
    /// </summary>
    public static IHoverTip FromIntent<TIntent>() where TIntent : AbstractIntent, new()
    {
        // 1. 极速获取缓存的纯文本元数据（零反射开销）
        var meta = IntentMetadataCache<TIntent>.Instance;
        
        // 2. 将加载纹理的动作交给原版的 PreloadManager 或 ResourceLoader，
        // 它们内部拥有安全的、与 Godot 生命周期绑定的资源缓存池。
        Texture2D? texture = null;
        if (!string.IsNullOrEmpty(meta.AssetPath))
        {
            texture = PreloadManager.Cache.GetTexture2D(meta.AssetPath);
        }
        
        // 3. 每次都 new 一个新的 HoverTip，彻底告别 ObjectDisposedException
        return new HoverTip(meta.Title, meta.Description, texture);
    }
    
    /// <summary>
    /// 纯 C# 元数据结构，不持有任何 Godot 非托管资源
    /// </summary>
    private class IntentMetadata
    {
        public required LocString Title;
        public required LocString Description;
        public string? AssetPath;
    }
    
    /// <summary>
    /// 泛型静态类缓存：利用 C# JIT 特性为每种 Intent 缓存反射结果和本地化字符串。
    /// </summary>
    private static class IntentMetadataCache<TIntent> where TIntent : AbstractIntent, new()
    {
        // ReSharper disable once StaticFieldInGenericType
        public static readonly IntentMetadata Instance;
        
        static IntentMetadataCache()
        {
            var intent = new TIntent();
            
            // 唯一需要反射的地方
            var prefix = typeof(TIntent)
                .GetProperty("IntentPrefix", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(intent) as string ?? intent.IntentType.ToString().ToUpperInvariant();
            
            // 组装纯净的文本元数据
            Instance = new IntentMetadata
            {
                Title = new LocString("intents", $"{prefix}.title"),
                Description = new LocString("intents", $"{prefix}.description"),
                AssetPath = intent.AssetPaths.FirstOrDefault()
            };
        }
    }
}