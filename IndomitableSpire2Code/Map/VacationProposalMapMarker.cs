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
    
    // 只能标记在休息处上
    public override bool CanMarkPointType(MapPointType type) => type == MapPointType.RestSite;
    
    public override MapMarkerViewModel CreateViewModel() => new ViewModel(this);
    
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