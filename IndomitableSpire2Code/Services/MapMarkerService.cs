using IndomitableSpire2.IndomitableSpire2Code.Map;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Services;

/// <summary>可扩展的展示注册表；不持有 Godot 节点，不反向调用遗物钩子。</summary>
public static class MapMarkerService
{
    private static readonly Dictionary<string, Func<IRunState, MapPoint, MapMarker>> Factories = new()
    {
        ["EyeInTheSky"] = (r, p) => new EyeInTheSkyMapMarker(r, p),
        ["VacationProposal"] = (r, p) => new VacationProposalMapMarker(r, p)
    };
    
    public static event Action<IRunState>? Changed;
    
    public static void Register(string id, Func<IRunState, MapPoint, MapMarker> factory) => Factories[id] = factory;
    
    public static IReadOnlyList<MapMarker> CreateMarkers(IRunState run, MapPoint point) =>
        Factories.Values.Select(factory => factory(run, point)).ToArray();
    
    public static void NotifyChanged(IRunState run)
    {
        if (Changed == null) return;
        foreach (var @delegate in Changed.GetInvocationList())
        {
            var listener = (Action<IRunState>)@delegate;
            try { listener(run); }
            catch (Exception exception) { MainFile.Logger.Warn($"Map marker view refresh failed: {exception}"); }
        }
    }
}