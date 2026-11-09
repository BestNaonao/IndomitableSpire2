namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>效果状态由遗物保存；地图开关只改变本机的持续闪烁，不改变效果。</summary>
public interface IMapActiveRelic
{
    void RefreshMapStatus(bool isMapOpen);
}