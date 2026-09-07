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
		var spineNode = GetNode<Node2D>("SpineSprite");
		spineNode.Scale = Vector2.One;

		var sprite = new MegaSprite(spineNode);
		this.RunWhenSpineReady(sprite, _ => PlayAnimation("normal", true));
	}
}
