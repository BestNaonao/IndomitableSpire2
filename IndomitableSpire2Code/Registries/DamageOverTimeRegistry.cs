using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Concurrent;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Registries;

/// 持续伤害能力注册表（单例）
public sealed class DamageOverTimeRegistry
{
    private static DamageOverTimeRegistry? _instance;
    public static DamageOverTimeRegistry Instance => 
        _instance ??= new DamageOverTimeRegistry();
    
    /// 按优先级排序的提供者列表
    private readonly List<IDamageOverTimeProvider> _providers = [];
    
    /// 获取所有已注册的 DOT 类型
    private HashSet<string> RegisteredTypes => _providers.Select(p => p.DamageTypeId).ToHashSet();

    private IDamageOverTimeProvider? GetProvider(string damageTypeId)
    {
        return _providers.FirstOrDefault(p => p.DamageTypeId == damageTypeId);
    }
    
    /// 血条 -> 前景控件映射
    private readonly ConcurrentDictionary<NHealthBar, Dictionary<string, Control>> 
        _foregroundControls = new();
    
    /// 前景控件纹理缓存
    private Texture2D? _foregroundTexture;
    
    /// 单例禁止外部调用构造函数
    private DamageOverTimeRegistry() { }
    
    /// 注册持续伤害提供者
    public void Register(IDamageOverTimeProvider provider)
    {
        if (_providers.Any(p => p.DamageTypeId == provider.DamageTypeId))
        {
            throw new InvalidOperationException(
                $"DotRegistry: DamageTypeId '{provider.DamageTypeId}' already registered by {_providers.First(p => p.DamageTypeId == provider.DamageTypeId).DisplayName}");
        }
        _providers.Add(provider);
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// 获取所有提供者（按优先级排序）
    public IReadOnlyList<IDamageOverTimeProvider> GetProviders() => _providers.AsReadOnly();
    
    /// 获取或创建前景控件
    public Control GetOrCreateForeground(NHealthBar healthBar, string damageTypeId)
    {
        var controls = _foregroundControls.GetOrAdd(healthBar, _ => new Dictionary<string, Control>());
        
        if (controls.TryGetValue(damageTypeId, out var existing))
            return existing;
        
        var foreground = CreateForegroundControl(healthBar, damageTypeId);
        controls[damageTypeId] = foreground;
        return foreground;
    }
    
    /// 创建前景控件
    private Control CreateForegroundControl(NHealthBar healthBar, string damageTypeId)
    {
        var mask = healthBar.GetNodeOrNull<Control>("%HpForegroundContainer/Mask");
        var poisonForeground = healthBar.GetNodeOrNull<Control>("%PoisonForeground");
        
        if (mask == null || poisonForeground == null)
        {
            MainFile.Logger.Error($"DotRegistry: Could not find Mask or PoisonForeground for {damageTypeId}");
            return new Control();
        }
        
        // 检查是否已存在（场景预添加）
        var existingNode = mask.GetNodeOrNull<Control>(damageTypeId);
        if (existingNode != null) return existingNode;
        
        // 获取当前提供者的优先级
        var currentProvider = GetProvider(damageTypeId);
        var currentPriority = currentProvider?.Priority ?? int.MaxValue;
        
        // 动态创建
        var foreground = new NinePatchRect
        {
            Name = damageTypeId,
            Texture = _foregroundTexture ??= (Texture2D)((NinePatchRect)poisonForeground).Get("texture"),
            PatchMarginLeft = ((NinePatchRect)poisonForeground).PatchMarginLeft,
            PatchMarginTop = ((NinePatchRect)poisonForeground).PatchMarginTop,
            PatchMarginRight = ((NinePatchRect)poisonForeground).PatchMarginRight,
            PatchMarginBottom = ((NinePatchRect)poisonForeground).PatchMarginBottom,
            Visible = false,
            SelfModulate = currentProvider!.ForegroundColor,
            ClipContents = true
        };
        
        // 按注册顺序排列节点
        var allChildren = mask.GetChildren().ToList();
        mask.AddChild(foreground);
        
        var poisonIndex = allChildren.IndexOf(poisonForeground);
        if (poisonIndex >= 0)
        {
            // 找到 Poison 之后所有 DOT 节点的位置
            var dotNodesIndexesAfterPoison = allChildren
                .Where((child, idx) => idx > poisonIndex && RegisteredTypes.Contains(child.Name))
                .Select(child => allChildren.IndexOf(child))
                .OrderBy(idx => idx)
                .ToList();
            
            MainFile.Logger.Info($"Found DotIndexes: [{string.Join(", ", dotNodesIndexesAfterPoison)}]");
            
            // 找到第一个优先级 >= 当前优先级的节点位置
            var insertIndex = poisonIndex + 1; // 默认插在 Poison 之后
        
            foreach (var existingIndex in dotNodesIndexesAfterPoison)
            {
                var existingProvider = GetProvider(allChildren[existingIndex].Name);

                if (existingProvider == null || existingProvider.Priority < currentPriority) continue;
                insertIndex = existingIndex;
                break;
            }
        
            mask.MoveChild(foreground, insertIndex);
            MainFile.Logger.Info($"DotRegistry: Moved {damageTypeId} to index {insertIndex} (Priority={currentPriority}), PoisonIndex={poisonIndex}");
        }
        
        MainFile.Logger.Info($"DotRegistry: Created foreground control for {damageTypeId}");
        return foreground;
    }
    
    /// 清理血条的所有前景控件
    public void Cleanup(NHealthBar healthBar)
    {
        if (!_foregroundControls.TryRemove(healthBar, out var controls)) return;
        foreach (var control in controls.Values) control.QueueFree();
        MainFile.Logger.Info($"DotRegistry: Cleaned up foreground controls for health bar");
    }
    
    /// 获取生物身上所有激活的持续伤害提供者
    public List<(IDamageOverTimeProvider Provider, int Damage)> GetActiveDotSources(Creature creature)
    {
        return _providers
            .Where(p => p.HasPower(creature))
            .Select(p => (Provider: p, Damage: p.CalculateNextDamage(creature)))
            .Where(t => t.Damage > 0)
            .ToList();
    }
}