using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

public sealed class VacationProposalMapMarker(IRunState run, MapPoint point) : MapMarker(run, point)
{
    public const string MarkerId = "VacationProposal";
    public override string Id => MarkerId;
    
    public override MapMarkerViewModel CreateViewModel() => new ViewModel(this);
    
    public override void OnAdded() => MapCommand.SetPointType(Run, Point, MapPointType.RestSite, notifyViews: false);
    
    public override void OnPointTypeChanged(MapPointType previous, MapPointType current)
    {
        // 其他效果改变休息处类型时，其额外休息权益随标记结束，不会在改回休息处后复活。
        if (current != MapPointType.RestSite) MapCommand.Remove(this, notifyViews: false);
    }
    
    private sealed class ViewModel(VacationProposalMapMarker marker) : MapMarkerViewModel(marker)
    {
        public override string TexturePath => "map_vacation_proposal_marker.png".MapIconPath();
        
        public override bool Visible => marker.Point.PointType == MapPointType.RestSite;
        
        public override MapMarkerHoverContent CreateHoverContent() => new MapMarkerTips([
            new HoverTip(new LocString("relics", "INDOMITABLESPIRE2-VACATION_PROPOSAL.title"),
                new LocString("relics", "INDOMITABLESPIRE2-VACATION_PROPOSAL.marker"))
        ]);
    }
}