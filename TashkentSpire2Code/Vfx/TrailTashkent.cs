using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class TrailTashkent : NCardTrail
{
	public override void _Ready()
	{
		base._Ready();
		GD.Print($"[TrailTashkent] Ready on {Name}");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		Width = Mathf.Lerp(Width, 1.0f, 0.05f);
	}
}
