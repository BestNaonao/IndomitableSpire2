using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

public sealed class EyeInTheSkyMapMarker(IRunState run, MapPoint point, ActMap? generatedMap = null) : MapMarker(run, point)
{
    public const string MarkerId = "EyeInTheSky";
    public override string Id => MarkerId;
    
    public override MapMarkerViewModel CreateViewModel() => new ViewModel(this);
    
    // 原版先调用 AfterMapGenerated，再赋值 RunState.Map；初始投放必须使用钩子传入的新地图。
    public override void OnAdded() => EyeInTheSkyService.ReserveEncounter(Run, generatedMap ?? Run.Map, Point);
    
    public override void OnRemoved() => EyeInTheSkyService.ForgetEncounter(Run, Point);
    
    public override void OnPointTypeChanged(MapPointType previous, MapPointType current)
    {
        // 普通战斗 -> 精英也必须重新预定；休息处则只遗忘。通用命令不需要认识遭遇缓存。
        EyeInTheSkyService.ForgetEncounter(Run, Point);
        if (current.IsCombat()) EyeInTheSkyService.ReserveEncounter(Run, generatedMap ?? Run.Map, Point);
    }
    
    private sealed class ViewModel(EyeInTheSkyMapMarker marker) : MapMarkerViewModel(marker)
    {
        public override string TexturePath => "map_eye_in_the_sky_marker.png".MapIconPath();
        
        public override bool CanPreview => LocalContext.GetMe(marker.Run.Players)?.Relics
            .OfType<EyeInTheSky>().Any(r => !r.IsMelted) == true;
        
        public override MapMarkerHoverContent? CreateHoverContent() => marker.Point.PointType.IsCombat()
            ? new MapMarkerEnemies(EyeInTheSkyService.PreviewMonsters(marker.Run, marker.Point)) : null;
    }
}