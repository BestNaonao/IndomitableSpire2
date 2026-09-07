using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Random;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class RestSiteCharacter : NRestSiteCharacter
{
	private static readonly System.Reflection.FieldInfo HitboxField =
		AccessTools.Field(typeof(NRestSiteCharacter), "<Hitbox>k__BackingField")
		?? throw new MissingFieldException(typeof(NRestSiteCharacter).FullName, "<Hitbox>k__BackingField");

	public override void _Ready()
	{
		// NRestSiteCharacter binds these members in its private setup code. Reproduce
		// that setup because its vanilla animation initialization does not reliably
		// handle a custom SpineSprite.
		var instance = Traverse.Create(this);
		instance.Field("_controlRoot").SetValue(GetNode<Control>("ControlRoot"));

		var hitbox = GetNode<Control>("%Hitbox");
		HitboxField.SetValue(this, hitbox);
		instance.Field("_selectionReticle").SetValue(GetNode<NSelectionReticle>("%SelectionReticle"));
		instance.Field("_leftThoughtAnchor").SetValue(GetNode<Control>("%ThoughtBubbleLeft"));
		instance.Field("_rightThoughtAnchor").SetValue(GetNode<Control>("%ThoughtBubbleRight"));

		var spineSprite = GetNodeOrNull<Node2D>("SpineSprite");
		if (GodotObject.IsInstanceValid(spineSprite) && spineSprite.GetClass() == "SpineSprite")
		{
			var sprite = new MegaSprite((Variant)(GodotObject)spineSprite);
			this.RunWhenSpineReady(sprite, animationState =>
			{
				animationState.SetAnimation("sleep");

				using var track = animationState.GetCurrent(0);
				if (track is not null)
					track.SetTrackTime(track.GetAnimationEnd() * Rng.Chaotic.NextFloat());
			});
		}

		// Preserve vanilla rest-site focus and thought-bubble interactions.
		hitbox.Connect(Control.SignalName.FocusEntered, new Callable(this, "OnFocus"));
		hitbox.Connect(Control.SignalName.FocusExited, new Callable(this, "OnUnfocus"));
		hitbox.Connect(Control.SignalName.MouseEntered, new Callable(this, "OnFocus"));
		hitbox.Connect(Control.SignalName.MouseExited, new Callable(this, "OnUnfocus"));
	}
}
