using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.UI;

/// <summary>Decorates the existing reward screen; never creates or claims rewards.</summary>
public partial class NTashkentBattleRewardsController : Node
{
    private static readonly Dictionary<int, float> NameplateWidths = new();
    private NRewardsScreen _screen = null!;
    private TashkentCharacter _character = null!;
    private Control _rewards = null!;
    private Control _view = null!;
    private LocManager? _localization;
    private NRelicInventory? _relicInventory;
    private Control? _topBarBackground;
    private Tween? _rewardEntrance;
    private bool _hasPresented;
    public static NTashkentBattleRewardsController? Find(NRewardsScreen screen) =>
        screen.GetNodeOrNull<NTashkentBattleRewardsController>("TashkentBattleRewardsController");

    public static void Attach(NRewardsScreen screen, TashkentCharacter character)
    {
        if (Find(screen) != null) return;
        // Resolve the entire scene before changing the original screen. A missing
        // resource must leave the vanilla reward interface usable.
        var scene = ResourceLoader.Load<PackedScene>(TashkentBattleRewardsAssets.ScenePath);
        if (scene == null) return;
        var view = scene.Instantiate<Control>();
        var controller = new NTashkentBattleRewardsController
        {
            Name = "TashkentBattleRewardsController", _screen = screen,
            _character = character, _view = view, _rewards = screen.GetNode<Control>("Rewards")
        };
        screen.AddChildSafely(view);
        screen.MoveChildSafely(view, 0);
        screen.AddChildSafely(controller);
    }

    public override void _Ready()
    {
        _localization = LocManager.Instance;
        _localization?.SubscribeToLocaleChange(UpdateLocale);
        _screen.Resized += UpdateLayout;
        var globalUi = NRun.Instance?.GlobalUi;
        _relicInventory = globalUi?.RelicInventory;
        _topBarBackground = globalUi?.TopBar.GetNodeOrNull<Control>("BgImage");
        if (_relicInventory != null)
        {
            _relicInventory.Resized += QueueLayout;
            _relicInventory.RelicsChanged += QueueLayout;
        }
        StyleProceedButton();
        UpdateLocale();
        UpdateLayout();
        // Prepare before the first visible frame; don't flash the old centered
        // reward layout while the overlay stack is opening this screen.
        _view.Call("prepare_entrance");
    }

    private void UpdateLocale()
    {
        if (!GodotObject.IsInstanceValid(_view)) return;
        string language = _localization?.Language ?? "eng";
        var strings = TashkentBattleRewardsAssets.LoadPreviewStrings(language);
        foreach (string key in strings.Keys.ToArray())
        {
            if (LocString.Exists("gameplay_ui", key))
                strings[key] = new LocString("gameplay_ui", key).GetFormattedText();
        }
        Font? font = FontManager.GetSubstituteFont(language, FontType.Bold)
            ?? ResourceLoader.Load<Font>("res://fonts/kreon_bold.ttf");
        string skinKey = _character.Id.Entry + ".skinName";
        string skin = LocString.GetIfExists("characters", skinKey)?.GetFormattedText() ?? "";
        _view.Call("configure", (int)_character.CurrentSkin, new Godot.Collections.Dictionary<string, string>(strings),
            font, _character.Title.GetFormattedText(), skin, MaximumNameplateWidth((int)_character.CurrentSkin));
    }

    private static float MaximumNameplateWidth(int skin)
    {
        if (NameplateWidths.TryGetValue(skin, out float cached)) return cached;
        float widest = 0;
        foreach (string language in new[] { "zhs", "eng", "jpn", "kor" })
        {
            var text = TashkentBattleRewardsAssets.LoadPreviewStrings(language);
            Font? font = FontManager.GetSubstituteFont(language, FontType.Bold)
                ?? ResourceLoader.Load<Font>("res://fonts/kreon_bold.ttf");
            if (font == null) continue;
            widest = Mathf.Max(widest, font.GetStringSize(text[TashkentBattleRewardsAssets.LocPrefix + "character"], fontSize: 44).X);
            widest = Mathf.Max(widest, font.GetStringSize(text[TashkentBattleRewardsAssets.LocPrefix + "previewSkin" + skin], fontSize: 23).X);
        }
        NameplateWidths[skin] = widest;
        return widest;
    }

    public void UpdateLayout()
    {
        if (!GodotObject.IsInstanceValid(_view)) return;
        if (_rewardEntrance?.IsValid() == true && _rewardEntrance.IsRunning())
        {
            _rewardEntrance.Kill();
            if (!_screen.IsComplete) _rewards.Modulate = Colors.White;
            _view.Call("finish_entrance");
        }
        _view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        float safeTop = SafeTop(out float headerMinimum);
        _view.Call("set_safe_top", safeTop, headerMinimum);
        _view.Call("update_layout");
        _rewards.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        float scale = _view.Get("layout_scale").AsSingle();
        _rewards.Scale = Vector2.One * (scale * TashkentBattleRewardsAssets.RewardWindowScale);
        _rewards.Position = _view.Get("layout_origin").AsVector2() + TashkentBattleRewardsAssets.RewardOrigin * scale;
        var button = _screen.GetNode<NProceedButton>("ProceedButton");
        // Finish vanilla's enable/disable colors as well as its old position;
        // simply killing this tween would leave an enabled button dark gray.
        var buttonTween = AccessTools.Field(typeof(NProceedButton), "_animTween")?.GetValue(button) as Tween;
        if (buttonTween?.IsValid() == true && buttonTween.IsRunning()) buttonTween.CustomStep(2.0);
        buttonTween?.Kill();
        button.Position = ProceedPosition(!button.IsEnabled);
    }

    private void QueueLayout() => Callable.From(UpdateLayoutIfAlive).CallDeferred();

    private void UpdateLayoutIfAlive()
    {
        if (IsInsideTree()) UpdateLayout();
    }

    private float SafeTop(out float headerMinimum)
    {
        float unit = Mathf.Min(_screen.Size.X / 1920f, _screen.Size.Y / 1080f);
        float bottom = 0f;
        Transform2D toScreen = _screen.GetGlobalTransformWithCanvas().AffineInverse();
        if (GodotObject.IsInstanceValid(_topBarBackground))
        {
            Transform2D transform = toScreen * _topBarBackground!.GetGlobalTransformWithCanvas();
            bottom = Mathf.Max(bottom, (transform * new Vector2(0, _topBarBackground.Size.Y)).Y);
        }
        if (GodotObject.IsInstanceValid(_relicInventory) && _relicInventory!.GetParent() is CanvasItem parent)
        {
            // Only reserve the first row, as requested. Wrapped relic rows do
            // not push down this layout; ignore temporarily hidden positions.
            if (_relicInventory.GetChildCount() > 0 && _relicInventory.GetChild(0) is Control firstRelic)
            {
                Vector2 firstRowBottom = _relicInventory.GetDefaultPosition()
                    + new Vector2(0, firstRelic.Position.Y + firstRelic.Size.Y);
                bottom = Mathf.Max(bottom, (toScreen * parent.GetGlobalTransformWithCanvas() * firstRowBottom).Y);
            }
        }
        if (bottom <= 0f) bottom = 168f * unit;
        headerMinimum = bottom + 12f * unit;
        return Mathf.Max(168f * unit, bottom) + 12f * unit;
    }

    public Vector2 ProceedPosition(bool hidden)
    {
        // NProceedButton owns its scale (enable, hover, press, unfocus). Keep
        // its native size instead of fighting those tweens on small windows.
        return _screen.Size - new Vector2(337f, 160f)
            + (hidden ? new Vector2(400f, 0) : Vector2.Zero);
    }

    public void ShowPresentation()
    {
        _view.Show();
        UpdateLayout();
    }

    public void AnimatePresentation(ref Tween? vanillaFade)
    {
        // Keep vanilla AfterOverlayShown's state/enable calls, replacing only
        // its +100px / Back-overshoot visual tween after it has been created.
        if (_screen.IsComplete)
        {
            _hasPresented = true;
            _view.Call("finish_entrance");
            return;
        }
        vanillaFade?.Kill();
        UpdateLayout();
        if (_hasPresented)
        {
            _view.Call("finish_entrance");
            _rewards.Modulate = Colors.White;
            return;
        }
        _hasPresented = true;
        _view.Call("play_entrance");
        Vector2 target = _rewards.Position;
        float scale = _view.Get("layout_scale").AsSingle();
        _rewards.Position = target - new Vector2(36f * scale, 0);
        _rewards.Modulate = new Color(1, 1, 1, 0);
        float entranceSpeed = _view.Get("entrance_speed_scale").AsSingle();
        _rewardEntrance = _screen.CreateTween().SetParallel().SetSpeedScale(entranceSpeed);
        _rewardEntrance.TweenProperty(_rewards, "position", target, 0.30)
            .SetDelay(0.12).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _rewardEntrance.TweenProperty(_rewards, "modulate", Colors.White, 0.30).SetDelay(0.12);
        // Vanilla completion / overlay-hide cancels this same tween, so quick
        // reward claims cannot resurrect an already completed reward window.
        vanillaFade = _rewardEntrance;
        var button = _screen.GetNode<NProceedButton>("ProceedButton");
        if (button.IsEnabled)
        {
            var image = button.GetNode<Control>("Image");
            Vector2 buttonTarget = ProceedPosition(false);
            button.Position = buttonTarget + new Vector2(40f, 0);
            image.SelfModulate = new Color(1, 1, 1, 0);
            var buttonEntrance = button.CreateTween().SetParallel().SetSpeedScale(entranceSpeed);
            buttonEntrance.TweenProperty(button, "position", buttonTarget, 0.333)
                .SetDelay(0.25).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            buttonEntrance.TweenProperty(image, "self_modulate", Colors.White, 0.333).SetDelay(0.25);
            // Store in vanilla's own slot: disable/enable/resize will cancel or
            // finish it normally, with no new input lock or completion wait.
            AccessTools.Field(typeof(NProceedButton), "_animTween")?.SetValue(button, buttonEntrance);
        }
    }

    public void HidePresentation()
    {
        _view.Call("finish_entrance");
        _view.Hide();
    }

    private void StyleProceedButton()
    {
        NProceedButton button = _screen.GetNode<NProceedButton>("ProceedButton");
        var image = button.GetNode<TextureRect>("Image");
        image.Texture = ResourceLoader.Load<Texture2D>(TashkentBattleRewardsAssets.ButtonPath);
        image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shadow = image.GetNodeOrNull<TextureRect>("Shadow");
        if (shadow != null) shadow.Texture = image.Texture;
        var outline = image.GetNodeOrNull<TextureRect>("Outline");
        if (outline != null) outline.Texture = ResourceLoader.Load<Texture2D>(TashkentBattleRewardsAssets.OutlinePath);
        var label = image.GetNode<Label>("Label");
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeColorOverride("font_color", new Color("332515"));
        label.AddThemeColorOverride("font_outline_color", new Color("ffdf81"));
        label.AddThemeConstantOverride("outline_size", 1);
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 0);
        // Retain the original MegaLabel, UpdateText, IsSkip, hotkeys, focus,
        // shader, enable/disable and multiplayer readiness behavior unchanged.
    }

    public override void _ExitTree()
    {
        _localization?.UnsubscribeToLocaleChange(UpdateLocale);
        if (GodotObject.IsInstanceValid(_screen))
            _screen.Resized -= UpdateLayout;
        if (GodotObject.IsInstanceValid(_relicInventory))
        {
            _relicInventory!.Resized -= QueueLayout;
            _relicInventory.RelicsChanged -= QueueLayout;
        }
        _rewardEntrance?.Kill();
    }
}
