using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class NPlayerHandTashkent : Control, IOverlayScreen
{
    public const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_selection.tscn";
    public static IEnumerable<string> AssetPaths => [ScenePath,
        "res://TashkentSpire2/images/ui/shell_selection/tashkent_handle.png",
        "res://TashkentSpire2/images/ui/shell_selection/confirm_button.png"];
    private RichTextLabel? _selectionHeader;
    private Control? _cardContainer;
    private NConfirmButton? _confirmButton;
    private NPeekButton? _peekButton;
    private Control? _leftArrow;
    private Control? _rightArrow;
    private Control? _shellSelector;
    private int _availableAmmo;
    private int _focusThreshold = -1;
    private bool _overlayActive;
    private bool _closing;
    private Func<string>? _promptTextFactory;
    private const string SliderLocPrefix = "TASHKENTSPIRE2-SHELL_SELECTOR.";

    private TaskCompletionSource<int>? _selectionCompletionSource;
    private List<CardModel> _shellCards = new();
    private int _currentIndex = 0;
    private int _maxAmount = 0;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _confirmButton;

    public void AfterOverlayOpened() { }
    public void AfterOverlayShown()
    {
        if (_closing) return;
        _overlayActive = true;
        Show();
        ConfigureSlider();
        (_confirmButton as NConfirmButtonTashkent)?.RefreshCaption();
        if (_selectionHeader != null && _promptTextFactory != null)
            _selectionHeader.Text = $"[center]{_promptTextFactory()}[/center]";
        _peekButton?.Enable();
        _confirmButton?.Enable();
    }

    public void AfterOverlayHidden()
    {
        _overlayActive = false;
        // Restore the combat peek state before hiding or closing this overlay.
        if (_peekButton?.IsPeeking == true) _peekButton.SetPeeking(false);
        _confirmButton?.Disable();
        _peekButton?.Disable();
        Hide();
    }

    public void AfterOverlayClosed()
    {
        if (_closing) return;
        _closing = true;
        AfterOverlayHidden();
        _selectionCompletionSource?.TrySetResult(0);
        this.QueueFreeSafely();
    }

    public static async Task<int> Show(List<CardModel> cards, string prompt, int maxAmount,
        int? availableAmmo = null, int? focusThreshold = null, Func<string>? promptTextFactory = null)
    {
        if (cards.Count == 0) return 0;
        var scene = GD.Load<PackedScene>(ScenePath);
        var screen = scene.Instantiate<NPlayerHandTashkent>();
        
        screen._shellCards = cards;
        screen._maxAmount = Math.Clamp(maxAmount, 0, Math.Min(6, Math.Max(0, cards.Count - 1)));
        screen._availableAmmo = Math.Max(0, availableAmmo ?? maxAmount);
        screen._focusThreshold = focusThreshold ?? -1;
        screen._promptTextFactory = promptTextFactory ?? (() => prompt);
        screen._currentIndex = screen._maxAmount;
        screen._selectionCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (NOverlayStack.Instance == null)
        {
            screen.Free();
            return 0;
        }
        NOverlayStack.Instance.Push(screen);
        
        return await screen.WaitForResult();
    }

    private void ChangeIndex(int delta)
    {
        SelectIndex(_currentIndex + delta);
    }

    private void SelectIndex(int index)
    {
        if (_closing || !_overlayActive) return;
        int nextIndex = Math.Clamp(index, 0, _maxAmount);
        if (nextIndex == _currentIndex) return;
        _currentIndex = nextIndex;
        UpdateDisplay();
    }
    
    public override void _Ready()
    {
        _selectionHeader = GetNode<RichTextLabel>("%SelectionHeader");
        _cardContainer = GetNode<Control>("%SelectedHandCardContainer");
        _confirmButton = GetNode<NConfirmButton>("%SelectModeConfirmButton");
        _leftArrow = GetNode<Control>("LeftArrow");
        _rightArrow = GetNode<Control>("RightArrow");
        _peekButton = GetNode<NPeekButton>("%PeekButton");
        _shellSelector = GetNode<Control>("%ShellSelector");
        _shellSelector.Connect("selection_changed", Callable.From<int>(SelectIndex));
        ConfigureSlider();

        if (_peekButton != null)
        {
            _peekButton.Enable();
            if (_selectionHeader != null && _confirmButton != null && _leftArrow != null && _rightArrow != null && _cardContainer != null)
            {
                _peekButton.AddTargets(_selectionHeader, _confirmButton, _leftArrow, _rightArrow, _cardContainer);
                _peekButton.AddTargets(_shellSelector);
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
        UpdateDisplay();
        return _selectionCompletionSource!.Task;
    }

    private void ConfigureSlider()
    {
        if (_shellSelector == null) return;
        var strings = new Godot.Collections.Dictionary<string, string>();
        strings["available"] = new LocString("gameplay_ui", SliderLocPrefix + "available").GetRawText();
        string language = LocManager.Instance?.Language ?? "eng";
        Font? font = FontManager.GetSubstituteFont(language, FontType.Bold)
            ?? ResourceLoader.Load<Font>("res://TashkentSpire2/fonts/kreon_bold.ttf");
        _shellSelector.Call("configure", _availableAmmo, _maxAmount, _focusThreshold, _currentIndex, strings, font);
    }

    public override void _Input(InputEvent input)
    {
        if (!_overlayActive || _closing || !IsVisibleInTree() ||
            _peekButton?.IsPeeking == true || NOverlayStack.Instance?.Peek() != this)
            return;
        if (input.IsActionPressed(MegaInput.left)) ChangeIndex(-1);
        else if (input.IsActionPressed(MegaInput.right)) ChangeIndex(1);
        else return;
        // Consume here so native HSlider directional input cannot step twice.
        GetViewport().SetInputAsHandled();
    }

    private void UpdateDisplay()
    {
        if (_cardContainer != null)
        {
            foreach (Node child in _cardContainer.GetChildren())
            {
                _cardContainer.RemoveChild(child);
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
        _shellSelector?.Call("set_selection", _currentIndex);
        _confirmButton?.Enable();
    }

    private void OnConfirmPressed()
    {
        if (!_overlayActive || _closing || _peekButton?.IsPeeking == true ||
            NOverlayStack.Instance?.Peek() != this ||
            _selectionCompletionSource?.TrySetResult(_currentIndex) != true) return;
        NOverlayStack.Instance?.Remove(this); 
    }

    public override void _ExitTree()
    {
        if (_selectionCompletionSource != null && !_selectionCompletionSource.Task.IsCompleted)
        {
            _selectionCompletionSource.TrySetResult(0);
        }
    }
}
