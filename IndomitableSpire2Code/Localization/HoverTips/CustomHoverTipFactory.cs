using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using System.Reflection;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;

public static class CustomHoverTipFactory
{
    /// <summary>
    /// 获取任意 Intent 的静态 HoverTip。
    /// 第一次调用时自动生成，后续调用零开销直接从泛型缓存获取。
    /// </summary>
    public static IHoverTip FromIntent<TIntent>() where TIntent : AbstractIntent, new()
    {
        return IntentCache<TIntent>.Tip;
    }
    
    /// <summary>
    /// 泛型静态类缓存：利用 C# JIT 特性，每种 TIntent 都会拥有独立的静态字段。
    /// 完美替代 Dictionary，避免了任何多线程并发问题和哈希查找开销。
    /// </summary>
    private static class IntentCache<TIntent> where TIntent : AbstractIntent, new()
    {
        // ReSharper disable once StaticFieldInGenericType
        // 设计意图：每种 Intent 类型独立缓存其 HoverTip，利用泛型静态构造函数的"每类型执行一次"特性
        public static readonly IHoverTip Tip;
        
        static IntentCache()
        {
            var intent = new TIntent();
            // 1. 唯一需要反射的地方：获取 protected 的 IntentPrefix。
            // 由于放在静态构造函数中，整个游戏生命周期对每种 Intent 只会执行一次反射。
            var prefixProp = typeof(TIntent).GetProperty(
                "IntentPrefix", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            
            // 兜底策略：如果获取失败，就将 IntentType 枚举转为大写字符串（例如 SleepIntent -> SLEEP）
            var prefix = prefixProp?.GetValue(intent) as string ?? 
                         intent.IntentType.ToString().ToUpperInvariant();
            
            // 2. 组装纯净的静态 LocString（不传入 targets 和 owner）
            var title = new LocString("intents", $"{prefix}.title");
            var description = new LocString("intents", $"{prefix}.description");
            
            // 3. 完美绕过 SpritePath 反射：利用公共的 AssetPaths！
            // 原版的 AssetPaths 已经帮我们把 SpritePath 包装进了 ImageHelper.GetImagePath 中。
            Texture2D? texture = null;
            var assetPath = intent.AssetPaths.FirstOrDefault();
            if (!string.IsNullOrEmpty(assetPath))
                texture = PreloadManager.Cache.GetTexture2D(assetPath);
            
            Tip = new HoverTip(title, description, texture);
        }
    }
}