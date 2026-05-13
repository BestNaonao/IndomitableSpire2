using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class NSelectedHandCardContainerTashkent : Control
{
    public NPlayerHandTashkent? Hand { get; set; }

    private NCard? _currentCardNode;

    public override void _Ready()
    {
    }

    public void SetCard(CardModel cardModel)
    {
        Clear();

        _currentCardNode = NCard.Create(cardModel);
        
        if (_currentCardNode == null) return;

        this.AddChild(_currentCardNode);

        _currentCardNode.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);

        _currentCardNode.Position = Vector2.Zero;

        _currentCardNode.Scale = Vector2.One * 0.8f;
        
        var tween = CreateTween();
        tween?.TweenProperty(_currentCardNode, "scale", Vector2.One, 0.15f)
            ?.SetTrans(Tween.TransitionType.Back)
            ?.SetEase(Tween.EaseType.Out);
    }

    public void Clear()
    {
        foreach (Node child in GetChildren())
        {
            child.QueueFreeSafely();
        }
        _currentCardNode = null;
    }
}