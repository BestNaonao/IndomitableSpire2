using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class NPlayerHandTashkent : Control, IOverlayScreen
{
    private RichTextLabel? _selectionHeader;
    private Control? _cardContainer;
    private NConfirmButton? _confirmButton;
    private NPeekButton? _peekButton;
    private Control? _leftArrow;
    private Control? _rightArrow;

    private TaskCompletionSource<int>? _selectionCompletionSource;
    private List<CardModel> _shellCards = new();
    private int _currentIndex = 0;
    private int _maxAmount = 0;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _confirmButton;

    public void AfterOverlayOpened() { }
    public void AfterOverlayShown() { }
    public void AfterOverlayHidden() { }
    public void AfterOverlayClosed() { }

    public static async Task<int> Show(List<CardModel> cards, string prompt, int maxAmount)
    {
        var scene = GD.Load<PackedScene>("res://TashkentSpire2/scenes/ui/tashkent_selection.tscn");
        var screen = scene.Instantiate<NPlayerHandTashkent>();
        
        screen._shellCards = cards;
        screen._maxAmount = maxAmount;
        screen._currentIndex = Math.Min(cards.Count - 1, maxAmount);

        NOverlayStack.Instance?.Push(screen);
        
        if (screen._selectionHeader != null)
        {
            screen._selectionHeader.Text = $"[center]{prompt}[/center]";
        }
        
        return await screen.WaitForResult();
    }

    private void ChangeIndex(int delta)
    {
        int nextIndex = _currentIndex + delta;

        if (nextIndex >= 0 && nextIndex <= _maxAmount && nextIndex < _shellCards.Count)
        {
            _currentIndex = nextIndex;
            UpdateDisplay();
        }
    }
    
    public override void _Ready()
    {
        _selectionHeader = GetNode<RichTextLabel>("%SelectionHeader");
        _cardContainer = GetNode<Control>("%SelectedHandCardContainer");
        _confirmButton = GetNode<NConfirmButton>("%SelectModeConfirmButton");
        _leftArrow = GetNode<Control>("LeftArrow");
        _rightArrow = GetNode<Control>("RightArrow");
        _peekButton = GetNode<NPeekButton>("%PeekButton");

        if (_peekButton != null)
        {
            _peekButton.Enable();
            if (_selectionHeader != null && _confirmButton != null && _leftArrow != null && _rightArrow != null && _cardContainer != null)
            {
                _peekButton.AddTargets(_selectionHeader, _confirmButton, _leftArrow, _rightArrow, _cardContainer);
            }
        }

        _confirmButton?.Enable();
        _confirmButton?.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(b => OnConfirmPressed()));

        if (_leftArrow != null)
        {
            _leftArrow.GuiInput += (input) => {
                if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left) {
                    ChangeIndex(-1);
                }
            };
        }

        if (_rightArrow != null)
        {
            _rightArrow.GuiInput += (input) => {
                if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left) {
                    ChangeIndex(1);
                }
            };
        }
    }

    private Task<int> WaitForResult()
    {
        _selectionCompletionSource = new TaskCompletionSource<int>();
        UpdateDisplay();
        return _selectionCompletionSource.Task;
    }

    private void UpdateDisplay()
    {
        if (_cardContainer != null)
        {
            foreach (Node child in _cardContainer.GetChildren())
            {
                child.QueueFreeSafely(); 
            }
        }

        if (_currentIndex >= 0 && _currentIndex < _shellCards.Count)
        {
            var cardModel = _shellCards[_currentIndex];
            var cardNode = NCard.Create(cardModel);
            _cardContainer?.AddChild(cardNode);
            cardNode?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        }

        if (_leftArrow != null) _leftArrow.Visible = _currentIndex > 0;
        if (_rightArrow != null) _rightArrow.Visible = _currentIndex < _maxAmount;
        _confirmButton?.Enable();
    }

    private void OnConfirmPressed()
    {
        _selectionCompletionSource?.TrySetResult(_currentIndex);
        NOverlayStack.Instance?.Remove(this); 
        this.QueueFreeSafely();
    }

    public override void _ExitTree()
    {
        if (_selectionCompletionSource != null && !_selectionCompletionSource.Task.IsCompleted)
        {
            _selectionCompletionSource.TrySetResult(0);
        }
    }
}