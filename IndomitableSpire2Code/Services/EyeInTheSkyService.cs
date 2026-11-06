using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Patches.Saves;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Services;

/// <summary>整局共享的地图事实。保存在 RunState 上，避免重复遗物和移除遗物改变已经确定的情报。</summary>
public static class EyeInTheSkyService
{
    public const int SpreadLimit = 3;
    private const double UpperFraction = 2.0 / 3.0;
    private static readonly ConditionalWeakTable<IRunState, Survey> Surveys = new();
    private static readonly ConditionalWeakTable<AbstractRoom, Arrival> Arrivals = new();
    private static readonly System.Reflection.FieldInfo EncounterRng = AccessTools.Field(typeof(EncounterModel), "_rng");
    
    /// <summary>记录一个被标记节点的“剧本”，包括具体的遭遇 ID 和生成该遭遇的专用种子。</summary>
    public readonly record struct Plan(string Encounter, ulong Seed);
    
    /// <summary>当前阶段（Act）的全局侦察状态。</summary>
    public sealed class Survey
    {
        public int Act { get; set; } = -1;
        public HashSet<string> Marks { get; init; } = []; // 记录已标记坐标
        public Dictionary<string, Plan> Plans { get; init; } = []; // 记录预定的遭遇
        public Dictionary<string, Plan> Observed { get; init; } = []; // 记录中途拾取遗物时“补票”记录的真实遭遇
        public int Step { get; set; }
        public int LastFloor { get; set; } = -1; // 用于防重入
    }
    
    /// <summary>单次进入房间的瞬态快照。</summary>
    private sealed class Arrival(bool marked, int floor)
    {
        public bool Marked { get; } = marked; // 进入房间前保存房间是否被标记
        public int Floor { get; } = floor;
        public bool Started { get; set; }
        public bool HasSpread { get; set; }
        // 同名遗物的不同实例各结算一次；不能按 ModelId 去重。允许多个不同遗物重复扣血，但是共享同一次“扩散”机会。
        public HashSet<EyeInTheSky> Notified { get; } = new(ReferenceEqualityComparer.Instance);
    }
    
    public static void RegisterSave()
    {
        // BaseLib 同时把扩展 RunState 字段写入本地存档和联机 SerializableRun 数据包。
        ExtendedSaveTypes.RegisterSavedValue<IRunState, string>("Indomitable.EyeInTheSky.v1",
            run => Surveys.TryGetValue(run, out var state) ? JsonSerializer.Serialize(state) : null,
            (run, json) =>
            {
                Surveys.Remove(run);
                if (!string.IsNullOrEmpty(json)) Surveys.Add(run, JsonSerializer.Deserialize<Survey>(json)!);
            }, 
            (json, writer) => writer.WriteString(json), reader => reader.ReadString());
    }
    
    // 按 NetId 排序获取所有持有者，确保多人游戏中逻辑执行顺序的确定性。
    public static EyeInTheSky[] Holders(IRunState run) => 
        run.Players.OrderBy(p => p.NetId).SelectMany(p => p.Relics.OfType<EyeInTheSky>()).ToArray();
    
    public static string Key(MapCoord coord) => $"{coord.col},{coord.row}";
    
    // 安全地收集地图上所有有效节点，包括可能存在的第二个 Boss 节点，并使用 DistinctBy 去重。
    public static IEnumerable<MapPoint> Points(ActMap map) => map.GetAllMapPoints()
        .Append(map.StartingMapPoint).Append(map.BossMapPoint)
        .Concat(map.SecondBossMapPoint is { } second ? [second] : [])
        .DistinctBy(p => p.coord);
    
    private static IEnumerable<MapPoint> RowMajorOrdered(IEnumerable<MapPoint> points) => 
        points.OrderBy(p => p.coord.row).ThenBy(p => p.coord.col);
    
    // 快速判断某节点是否在当前阶段的标记集合中。
    public static bool IsMarked(IRunState run, MapPoint point) => 
        Surveys.TryGetValue(run, out var s) && s.Act == run.CurrentActIndex && s.Marks.Contains(Key(point.coord));
    
    /// <summary>地图初始化与初始标记。</summary>
    public static void EnsureMap(IRunState run, ActMap? map, int act)
    {
        if (map == null || Holders(run).Length == 0) return; // 防卫条件: 传入存在的地图并且当前游戏中有玩家持有遗物
        var state = Surveys.GetOrCreateValue(run); // 获取或创建全局状态
        if (state.Act == act) return; // 防止同一阶段重复初始化
        state.Act = act; state.Marks.Clear(); state.Plans.Clear(); state.Observed.Clear(); state.Step = 0; state.LastFloor = -1;
        var all = Points(map).ToArray();
        var threshold = Mathf.Lerp(all.Min(p => p.coord.row), all.Max(p => p.coord.row), UpperFraction);
        var candidates = RowMajorOrdered(
            all.Where(p => p.coord.row >= threshold && p.PointType != MapPointType.RestSite)).ToList();
        if (candidates.Count == 0) return;
        var rng = new Rng(GetSeed(run, act, "initial"));
        Mark(run, map, state, candidates[rng.NextInt(candidates.Count)]);
    }
    
    /// <summary>在玩家进入房间前，记录该房间在进入前是否被标记以及当前楼层。如果已存在则复用，确保多遗物实例看到相同的快照。</summary>
    public static void BeforeEnter(IRunState run, AbstractRoom room)
    {
        if (run.CurrentMapPoint is not { } point) return;
        EnsureMap(run, run.Map, run.CurrentActIndex);
        Arrivals.GetValue(room, _ => new Arrival(IsMarked(run, point), run.TotalFloor)); // GetValue 在键不存在时会新建值。
    }
    
    /// <summary>由遗物自己的 AfterRoomEntered 领取一次结算；返回的标记始终是进入前的状态。</summary>
    public static bool TryBeginRelicEntry(IRunState run, AbstractRoom room, EyeInTheSky relic, out bool marked)
    {
        marked = false;
        if (!Arrivals.TryGetValue(room, out var arrival) || !Surveys.TryGetValue(run, out var state) ||
            state.Act != run.CurrentActIndex) return false; // 防御时序错乱——没到达过、状态错误——没全局状态或者跨层残留——阶段不同。
        if (!arrival.Started)
        {
            if (state.LastFloor == arrival.Floor) return false; // 防重入：同一楼层的重复触发（如事件子房间）被忽略
            state.LastFloor = arrival.Floor;
            arrival.Started = true;
        }
        if (!arrival.Notified.Add(relic)) return false; // 确保同一遗物只触发一次
        marked = arrival.Marked;
        return true;
    }
    
    /// <summary>每件遗物完成自身结算后通知服务；第一次通知扩散，其余通知不再占用名额。</summary>
    public static void SpreadAfterEntry(IRunState run, AbstractRoom room)
    {
        if (!Arrivals.TryGetValue(room, out var arrival) || !arrival.Started || arrival.HasSpread ||
            !Surveys.TryGetValue(run, out var state) || state.Act != run.CurrentActIndex) return;
        arrival.HasSpread = true; // 仿乐观锁的处理方式，以防多遗物重入
        Spread(run, run.Map, state);
    }
    
    public static MapPoint[] Frontier(ActMap map, IReadOnlySet<string> marks) => RowMajorOrdered(Points(map)
        .Where(p => marks.Contains(Key(p.coord))) // 过滤标记的节点
        .SelectMany(p => p.parents.Concat(p.Children)) // 获取父节点和子节点
        .Where(p => p.PointType != MapPointType.RestSite && !marks.Contains(Key(p.coord))) // 非休息处且未标记
        .DistinctBy(p => p.coord)).ToArray();
    
    private static void Spread(IRunState run, ActMap map, Survey state)
    {
        // 先快照整个旧边界再抽取：同一步新标记不会成为扩散源，多个源共享三个名额。
        var choices = Frontier(map, state.Marks).ToList();
        var rng = new Rng(GetSeed(run, state.Act, $"spread:{state.Step++}"));
        for (var i = 0; i < SpreadLimit && choices.Count > 0; i++)
        {
            var index = rng.NextInt(choices.Count);
            var point = choices[index];
            choices.RemoveAt(index);
            Mark(run, map, state, point);
        }
    }
    
    private static bool Visited(IRunState run, MapPoint point) => 
        run.CurrentMapPoint == point || run is RunState actual && actual.VisitedMapCoords.Contains(point.coord);
    
    private static void Mark(IRunState run, ActMap map, Survey state, MapPoint point)
    {
        // 已标记则跳过，然后处理战斗房间的逻辑，已进入的房间不会被新情报替换；进入时恰好扩散到此节点也不会追溯改写正在进行的战斗。
        if (!state.Marks.Add(Key(point.coord)) || !point.PointType.IsCombat() || Visited(run, point)) return;
        var type = point.PointType switch { MapPointType.Elite => RoomType.Elite, MapPointType.Boss => RoomType.Boss, _ => RoomType.Monster };
        // 在原版 RoomSet 的独立副本上取样，绝不推进真实遭遇队列或悬停时消耗游戏 RNG。
        var actCopy = ActModel.FromSave(run.Act.ToSave());
        var reservations = Points(map)
            .Count(p => state.Plans.TryGetValue(Key(p.coord), out var plan) && !Visited(run, p) && 
                        ModelDb.GetById<EncounterModel>(ModelId.Deserialize(plan.Encounter)).RoomType == type);
        // 模拟消耗：之前已经预定的同类型房间，需要在这里预先“MarkRoomVisited”，以保证 PullNextEncounter 抽到的队列顺序与原版完全一致。
        for (var i = 0; i < reservations; i++) actCopy.MarkRoomVisited(type);
        var encounter = type == RoomType.Boss
            ? (point == map.SecondBossMapPoint ? run.Act.SecondBossEncounter : null) ?? run.Act.BossEncounter
            : actCopy.PullNextEncounter(type);
        state.Plans[Key(point.coord)] = new Plan(encounter.Id.ToString(), GetSeed(run, state.Act, $"monsters:{Key(point.coord)}"));
    }
    
    public static Plan? GetPlan(IRunState run, MapPoint point) =>
        Surveys.TryGetValue(run, out var state) && state.Act == run.CurrentActIndex
            ? state.Plans.TryGetValue(Key(point.coord), out var plan)
                ? plan : state.Observed.TryGetValue(Key(point.coord), out var observed)
                    ? observed : null : null;
    
    public static MonsterModel[] PreviewMonsters(IRunState run, MapPoint point)
    {
        if (GetPlan(run, point) is { } plan) // 有预设计划
        {
            var encounter = CreateEncounter(plan);
            encounter.GenerateMonstersWithSlots(run);
            // 必须保留原版遭遇的槽位：例如幽灵花园鳗按 first/third 选择 tall，其余选 short。
            return encounter.MonstersWithSlots.Select((m, index) => PreparePreviewMonster(m.Item1, 
                m.Item2, seed: unchecked(plan.Seed + (ulong)index))).ToArray();
        }
        // 历史“补票”：中途拾起可能标记此前已经走过的战斗。此时只读原版记录的实际怪物，
        // 不按现在的队列重新生成。GetHistoryEntryFor 只按 row 索引，所以必须先验证 col/row 确实走过。
        if (!point.PointType.IsCombat() || !Visited(run, point)) return [];
        var history = run.GetHistoryEntryFor(new MapLocation(point.coord, run.CurrentActIndex));
        return history?.Rooms.FirstOrDefault(r => r.RoomType.IsCombatRoom())?.MonsterIds
            .Select((id, index) => PreparePreviewMonster(ModelDb.GetById<MonsterModel>(id).ToMutable(), null,
                GetSeed(run, run.CurrentActIndex, $"history-portrait:{Key(point.coord)}:{index}")))
            .ToArray() ?? [];
    }
    
    public static EncounterModel CreateEncounter(Plan plan)
    {
        var encounter = ModelDb.GetById<EncounterModel>(ModelId.Deserialize(plan.Encounter)).ToMutable();
        EncounterRng.SetValue(encounter, new Rng(plan.Seed));
        return encounter;
    }
    
    /// <summary>给独立预览模型补充皮肤读取的上下文；不创建或加入真实战斗。</summary>
    private static MonsterModel PreparePreviewMonster(MonsterModel monster, string? slot, ulong seed)
    {
        // 参考原版 NBestiaryLayoutDefault / NBestiaryLayoutDecimillipede：
        // 直接构造 Creature 并挂 NullCombatState，供 SetupSkins 读取槽位和展示数据。
        // 不调用 CombatState.CreateCreature，避免影响现在的游戏和战斗。
        monster.Rng = new Rng(seed);
        monster.RunRng = new RunRngSet(new Rng(seed));
        _ = new Creature(monster, CombatSide.Enemy, slot) { CombatState = NullCombatState.Instance };
        return monster;
    }
    
    public static void RestoreEncounterSeed(EncounterModel encounter, IRunState run)
    {
        // 战后存档重建 Encounter 的路径也要使用同一种子。事件脚本的战斗不在修改范围内。
        if (run.CurrentRoom is not CombatRoom room || room.Encounter != encounter || room.ParentEventId != null ||
            run.CurrentMapPoint is not { } point || !point.IsCombatPoint()) return;
        if (GetPlan(run, point) is { } plan && encounter.Id.ToString() == plan.Encounter)
            EncounterRng.SetValue(encounter, new Rng(plan.Seed));
        else if (Surveys.TryGetValue(run, out var state) && state.Act == run.CurrentActIndex && EncounterRng.GetValue(encounter) == null)
        {
            // 读取原版 GenerateMonstersWithSlots 的默认种子。只记录，仍由原版初始化；
            // 如果此次扩散标记了当前战斗，悬停展示实际已生成的遭遇，不预定一场替代战斗。
            state.Observed[Key(point.coord)] = new Plan(encounter.Id.ToString(),
                unchecked(run.Rng.Seed + (ulong)run.TotalFloor + StringHelper.GetDeterministicHashCode(encounter.Id.Entry)));
        }
    }
    
    private static ulong GetSeed(IRunState run, int act, string purpose)
    {
        // FNV-1a 是稳定字符串散列；不使用进程随机化的 GetHashCode，也不按玩家/遗物数量播种。
        return $"{run.Rng.Seed}:EyeInTheSky:{act}:{purpose}".Aggregate(14695981039346656037UL, 
            (current, c) => unchecked((current ^ c) * 1099511628211UL));
    }
}