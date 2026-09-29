namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 由角色模型实现，指定催眠爆发时循环播放的 Spine 动画。
/// 未实现接口、名称为空或模型没有指定动画时，会尝试使用 sleep；两者都不存在则跳过动画。
/// </summary>
public interface IHypnotizedAnimationProvider
{
    string? HypnotizedAnimationName { get; }
}