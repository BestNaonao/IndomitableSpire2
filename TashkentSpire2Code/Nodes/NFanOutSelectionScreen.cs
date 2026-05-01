using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;

namespace TashkentSpire2.TashkentSpire2Code.Nodes;

public partial class NFanOutSelectionScreen : Control, IOverlayScreen
{
    private List<CardModel> _models = new();
    private List<NHandCardHolder> _holders = new();
    private TaskCompletionSource<CardModel?> _tcs = new();
    private Control _cardContainer = null!; 
    
    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _holders.Count > 0 ? _holders[_holders.Count / 2] : this;

    public void AfterOverlayOpened() { }
    public void AfterOverlayClosed() { }
    public void AfterOverlayShown() { }
    public void AfterOverlayHidden() { }

    public static async Task<CardModel?> Show(IReadOnlyList<CardModel> models)
    {
        var screen = new NFanOutSelectionScreen();
        screen._models = models.ToList();

        NOverlayStack.Instance?.Push(screen);
        
        return await screen._tcs.Task;
    }

    public override void _Ready()
    {
        this.SetAnchorsPreset(LayoutPreset.FullRect);
        
        _cardContainer = new Control();

        _cardContainer.SetAnchorsPreset(LayoutPreset.CenterBottom); 
        AddChild(_cardContainer);

        for (int i = 0; i < _models.Count; i++)
        {
            var nCard = NCard.Create(_models[i]);
            var holder = NHandCardHolder.Create(nCard, null); 
            
            _cardContainer.AddChild(holder);
            _holders.Add(holder);
            
            holder.Connect(NCardHolder.SignalName.Pressed, Callable.From(() => OnCardSelected(holder)));
        }

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        int count = _holders.Count;
        if (count == 0) return;
        
        Vector2 scale = HandPosHelper.GetScale(count);

        for (int i = 0; i < count; i++)
        {
            var holder = _holders[i];

            Vector2 position = HandPosHelper.GetPosition(count, i);
            float angle = HandPosHelper.GetAngle(count, i);

            holder.SetTargetPosition(position);
            holder.SetTargetAngle(angle);
            holder.SetTargetScale(scale);
        }
    }

    private void OnCardSelected(NHandCardHolder holder)
    {
        _tcs.TrySetResult(holder.CardModel);
        Close();
    }

    private void Close()
    {
        NOverlayStack.Instance?.Remove(this);
        QueueFree();
    }
}