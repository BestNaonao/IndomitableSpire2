using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using TashkentSpire2.TashkentSpire2Code.Workshop;

namespace TashkentSpire2.TashkentSpire2Code.Nodes;

[GlobalClass]
public partial class TashkentWorkshopVoteButton : NButton
{
    private const string TitleKey = "TASHKENTSPIRE2-WORKSHOP_VOTE.title";
    private readonly IWorkshopVoteClient _client = new SteamWorkshopVoteClient();
    private WorkshopVoteSession _session = null!;
    private TextureRect _background = null!;
    private bool _screenOpen = true;
    private bool _selected;
    private bool _interactable = true;
    private double _accountPoll;

    public override void _Ready()
    {
        _background = GetNode<TextureRect>("Background");
        _session = new WorkshopVoteSession(_client, message => MainFile.Logger.Warn(message));
        _session.Changed += RefreshVisibility;
        ConnectSignals();
        Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(OnReleased));
        RefreshVisibility();
    }

    public void OpenScreen()
    {
        _screenOpen = true;
        _interactable = true;
        _selected = false;
        _session.Reset();
        RefreshVisibility();
    }

    public void CloseScreen()
    {
        _screenOpen = false;
        _selected = false;
        _session.Reset();
        RefreshVisibility();
    }

    public void SetCharacterSelected(bool selected)
    {
        _selected = selected;
        if (selected && _screenOpen && _session.State == WorkshopVoteState.Unknown)
            _ = TaskHelper.RunSafely(_session.RefreshAsync());
        RefreshVisibility();
    }

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        RefreshVisibility();
    }

    public override void _Process(double delta)
    {
        if (!_screenOpen || !_selected)
            return;
        _accountPoll += delta;
        if (_accountPoll < 1d)
            return;
        _accountPoll = 0d;

        bool available = _client.TryGetCurrentUser(out ulong userId);
        if ((!available && _session.UserId != 0) || (available && userId != _session.UserId))
        {
            _session.Reset();
            if (available)
                _ = TaskHelper.RunSafely(_session.RefreshAsync());
        }
    }

    public override void _ExitTree()
    {
        _screenOpen = false;
        _session.Changed -= RefreshVisibility;
        _session.Reset();
        NHoverTipSet.Remove(this);
        base._ExitTree();
    }

    private void RefreshVisibility()
    {
        if (!IsInsideTree())
            return;
        bool canShow = _screenOpen && _selected && _interactable
            && _session.State is WorkshopVoteState.Available or WorkshopVoteState.Submitting;
        Visible = canShow;
        SetEnabled(canShow && _session.State == WorkshopVoteState.Available);
        if (!canShow)
            NHoverTipSet.Remove(this);
    }

    private void OnReleased(NClickableControl button)
    {
        if (IsEnabled && IsVisibleInTree() && _screenOpen && _selected && _interactable
            && NModalContainer.Instance?.OpenModal == null && _session.State == WorkshopVoteState.Available)
            _ = TaskHelper.RunSafely(_session.VoteUpAsync(_session.UserId));
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        Scale = Vector2.One * 1.06f;
        _background.SelfModulate = StsColors.gold;
        NHoverTipSet.Remove(this);
        var hoverTip = new HoverTip(new LocString("settings_ui", TitleKey)) { ShouldOverrideTextOverflow = true };
        var hoverTipSet = NHoverTipSet.CreateAndShow(this, hoverTip, HoverTipAlignment.Right);
        if (hoverTipSet == null)
            return;
        var container = hoverTipSet.GetNodeOrNull<VFlowContainer>("textHoverTipContainer");
        if (container == null)
            return;
        foreach (var tip in container.GetChildren().OfType<Control>())
        {
            var description = tip.GetNodeOrNull<RichTextLabel>("%Description");
            if (description == null)
                continue;
            description.CustomMinimumSize = Vector2.Zero;
            description.ResetSize();
            tip.ResetSize();
        }
        container.ResetSize();
        hoverTipSet.SetAlignment(this, HoverTipAlignment.Right);
        hoverTipSet.SetFollowOwner();
    }

    protected override void OnUnfocus()
    {
        Scale = Vector2.One;
        if (GodotObject.IsInstanceValid(_background))
            _background.SelfModulate = Colors.White;
        NHoverTipSet.Remove(this);
    }

    protected override void OnPress()
    {
        base.OnPress();
        Scale = Vector2.One * 0.95f;
    }

    protected override void OnRelease() => Scale = IsFocused ? Vector2.One * 1.06f : Vector2.One;
    protected override void OnEnable() { base.OnEnable(); Modulate = Colors.White; }
    protected override void OnDisable() { base.OnDisable(); Modulate = new Color(0.65f, 0.65f, 0.65f); }
}
