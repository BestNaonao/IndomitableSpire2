using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

[GlobalClass]
public partial class NMerchantCharacterTashkent : NMerchantCharacter
{
	public override void _Ready()
	{
		// The static fallback intentionally has no SpineSprite. Returning here keeps
		// the original merchant adaptation, which displayed its Sprite2D as-is and
		// suppressed the vanilla merchant animation setup.
		var spineNode = GetNodeOrNull<Node2D>("SpineSprite");
		if (!GodotObject.IsInstanceValid(spineNode) || spineNode.GetClass() != "SpineSprite")
			return;

		spineNode.Scale = Vector2.One;

		var sprite = new MegaSprite(spineNode);
		this.RunWhenSpineReady(sprite, _ => PlayAnimation("normal", true));
	}
}
