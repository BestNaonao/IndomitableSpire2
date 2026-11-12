using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Patches.Saves;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Map;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Services;

/// <summary>
/// 统一关系仓库：Run -> Act -> 坐标 -> 标记 ID；实例缓存只用于复用模型，不是第二份游戏状态。
/// 具体规则在 Marker，展示在 ViewModel/View，专用服务只保存各自的遭遇/遗物身份等数据。
/// </summary>
public static class MapMarkerService
{
    /// <summary>
    /// 唯一的游戏状态及会被序列化的东西。三层映射：阶段数 → 坐标 key → 该点的标记 ID 集合。值是字符串 ID 而不是对象，这是"关系即状态"的具体形态。
    /// </summary>
    public sealed class State
    {
        public Dictionary<int, Dictionary<string, SortedSet<string>>> Acts { get; set; } = [];
    }
    
    private sealed class Runtime
    {
        public State Data { get; } = new(); // 关系本体。属性只读（引用不可换），内容可变。
        public ConditionalWeakTable<MapPoint, Dictionary<string, MapMarker>> Models { get; } = new(); // 按节点缓存 id→模型。
    }
    
    private static readonly ConditionalWeakTable<IRunState, Runtime> Runs = new(); // 绑定一局游戏 run → 运行时数据，同样弱语义。
    
    private static readonly Dictionary<string, Func<IRunState, MapPoint, MapMarker>> Factories = new(StringComparer.Ordinal)
    {
        [EyeInTheSkyMapMarker.MarkerId] = (r, p) => new EyeInTheSkyMapMarker(r, p),
        [VacationProposalMapMarker.MarkerId] = (r, p) => new VacationProposalMapMarker(r, p)
    };
    
    /// <summary>注册可从稳定 ID 重建的模型。构造器必须无游戏副作用；ID 也是存档协议的一部分。</summary>
    public static void Register(string id, Func<IRunState, MapPoint, MapMarker> factory)
    {
        if (!Factories.TryAdd(id, factory)) throw new ArgumentException($"Map marker already registered: {id}");
    }
    
    /// <summary>
    /// 向 BaseLib 注册一个"挂在每个 Run 上的 string 存档槽"。Mod 初始化时调用一次。
    /// </summary>
    public static void RegisterSave() => ExtendedSaveTypes.RegisterSavedValue<IRunState, string>(
        "Indomitable.MapMarkers.v1",
        run => Runs.TryGetValue(run, out var state) ? JsonSerializer.Serialize(state.Data) : null,
        (run, json) =>
        {
            if (string.IsNullOrEmpty(json)) return;
            // 原版为读档创建新的 RunState；合并可兼容旧服务迁移回调先于/晚于本回调。
            foreach (var (act, points) in JsonSerializer.Deserialize<State>(json)!.Acts)
                foreach (var (key, ids) in points)
                    foreach (var id in ids) ImportLegacy(run, act, id, [key]);
        },
        (json, writer) => writer.WriteString(json), reader => reader.ReadString());
    
    // 导入只迁移关系，不运行 OnAdded，不重抽遭遇，也不再次改变房间的类型。
    internal static void ImportLegacy(IRunState run, int act, string id, IEnumerable<string> coordinates)
    {
        var data = Runs.GetOrCreateValue(run).Data;
        if (!data.Acts.TryGetValue(act, out var points)) data.Acts[act] = points = [];
        foreach (var key in coordinates)
        {
            if (!points.TryGetValue(key, out var ids))
                points[key] = ids = new SortedSet<string>(StringComparer.Ordinal);
            ids.Add(id);
        }
    }
    
    public static event Action<IRunState>? Changed; // 订阅者就是 NMapMarkerLayer。
    
    /// <summary>当前阶段指定节点是否存在特定 ID 的标记</summary>
    public static bool Contains(IRunState run, MapPoint point, string id) =>
        Runs.TryGetValue(run, out var runtime) && runtime.Data.Acts.TryGetValue(run.CurrentActIndex, out var points) &&
        points.TryGetValue(point.CoordKey(), out var ids) && ids.Contains(id);
    
    /// <summary>当前阶段所有存在特定 ID 的标记的节点键的集合</summary>
    public static IReadOnlySet<string> Coordinates(IRunState run, string id) =>
        Runs.TryGetValue(run, out var runtime) && runtime.Data.Acts.TryGetValue(run.CurrentActIndex, out var points)
            ? points.Where(p => p.Value.Contains(id))
                .Select(p => p.Key)
                .ToHashSet(StringComparer.Ordinal) 
            : [];
    
    /// <summary>获取指定节点的标记模型列表，对于缓存中未记录的会重建</summary>
    public static IReadOnlyList<MapMarker> GetMarkers(IRunState run, MapPoint point)
    {
        var runtime = Runs.GetOrCreateValue(run);
        if (!runtime.Data.Acts.TryGetValue(run.CurrentActIndex, out var points) ||
            !points.TryGetValue(point.CoordKey(), out var ids)) return [];
        var models = runtime.Models.GetOrCreateValue(point); // 读档不会命中缓存，必定重建
        var result = new List<MapMarker>();
        // Ordinal 排序使所有客户端与读档前后拥有相同顺序；未知 ID 保留存档但不实例化。
        foreach (var id in ids.Order(StringComparer.Ordinal))
        {
            if (!Factories.TryGetValue(id, out var factory)) continue;
            if (!models.TryGetValue(id, out var model) || model.Act != run.CurrentActIndex)
                models[id] = model = factory(run, point);
            result.Add(model);
        }
        return result;
    }
    
    /// <summary>添加一个标记模型，同时保存到全局关系和记录到缓存数据，关系重复则返回 false，成功添加返回 true。</summary>
    internal static bool Add(MapMarker marker)
    {
        // 验证是否是合规的标记模型，要求是工厂中记录并且属于当前阶段的。
        if (!Factories.ContainsKey(marker.Id)) throw new ArgumentException($"Unregistered map marker: {marker.Id}");
        if (marker.Act != marker.Run.CurrentActIndex) throw new InvalidOperationException("Cannot mark a previous act.");
        if (!marker.CanMark()) return false;
        if (Contains(marker.Run, marker.Point, marker.Id)) return false; // 保证幂等添加
        ImportLegacy(marker.Run, marker.Act, marker.Id, [marker.Point.CoordKey()]); // 保存到关系
        Runs.GetOrCreateValue(marker.Run).Models.GetOrCreateValue(marker.Point)[marker.Id] = marker; // 记录到缓存
        return true;
    }
    
    /// <summary>移除当前阶段的一个标记模型，不修改历史阶段。同时从全局关系和缓存数据中移除。</summary>
    internal static bool Remove(MapMarker marker)
    {
        if (marker.Act != marker.Run.CurrentActIndex || !Contains(marker.Run, marker.Point, marker.Id)) return false;
        var runtime = Runs.GetOrCreateValue(marker.Run);
        var points = runtime.Data.Acts[marker.Act];
        var ids = points[marker.Point.CoordKey()];
        ids.Remove(marker.Id); // 从关系中移除
        if (ids.Count == 0) points.Remove(marker.Point.CoordKey()); // 移除已经清空标记的节点关系
        runtime.Models.GetOrCreateValue(marker.Point).Remove(marker.Id); // 从缓存中移除
        return true;
    }
    
    // 地图恢复或节点类型改变后，按当前节点状态复查已有标记。GetMarkers 返回快照，允许 OnRemoved 移除关联标记。
    private static void RemoveIneligible(IRunState run, MapPoint point)
    {
        foreach (var marker in GetMarkers(run, point))
            if (!marker.CanMark()) MapCommand.Remove(marker, notifyViews: false);
    }
    
    // 原版先恢复 Run 的扩展字段，稍后才设置实际地图。此时所有客户端都能依实际类型清理旧存档。
    internal static void RevalidateMap(IRunState run, ActMap map)
    {
        if (map is null or NullActMap || !Runs.TryGetValue(run, out var runtime) ||
            !runtime.Data.Acts.TryGetValue(run.CurrentActIndex, out var markedPoints) ||
            markedPoints.Count == 0) return;
        foreach (var point in MapCommand.Points(map))
            if (markedPoints.ContainsKey(point.CoordKey())) RemoveIneligible(run, point);
    }
    
    /// <summary>节点类型改变后，移除与新类型不兼容的标记，再通知保留的标记。</summary>
    internal static bool SetPointType(IRunState run, MapPoint point, MapPointType type)
    {
        var previous = point.PointType;
        if (previous == type) return false;
        point.PointType = type;
        RemoveIneligible(run, point);
        foreach (var marker in GetMarkers(run, point))
            if (Contains(run, point, marker.Id)) marker.OnPointTypeChanged(previous, type);
        return true;
    }
    
    public static void NotifyChanged(IRunState run)
    {
        if (Changed == null) return;
        foreach (var listener in Changed.GetInvocationList().Cast<Action<IRunState>>())
            try { listener(run); }
            catch (Exception exception) { MainFile.Logger.Warn($"Map marker view refresh failed: {exception}"); }
    }
}