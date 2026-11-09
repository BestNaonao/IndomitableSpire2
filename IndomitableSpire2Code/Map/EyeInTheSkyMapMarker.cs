using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

public sealed class EyeInTheSkyMapMarker(IRunState run, MapPoint point) : MapMarker(run, point)
{
    public override string Id => "EyeInTheSky";
    
    public override string TexturePath => "map_eye_in_the_sky_marker.png".MapIconPath();
    
    public override bool Visible => EyeInTheSkyService.IsMarked(Run, Point);
    
    public override bool CanPreview => 
        LocalContext.GetMe(Run.Players)?.Relics.OfType<EyeInTheSky>().Any(r => !r.IsMelted) == true;
    
    public override MonsterModel[] PreviewMonsters() => 
        Point.PointType.IsCombat() ? EyeInTheSkyService.PreviewMonsters(Run, Point) : [];
}