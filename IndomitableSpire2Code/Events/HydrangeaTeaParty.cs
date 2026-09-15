using BaseLib.Abstracts;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Potions;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Events;

/// <summary>
/// 紫阳花茶会：接受一位姐姐的邀请并获得对应卡牌，再从她准备的两瓶药水中选择一瓶。
/// CustomEventModel 自动将本事件注册到通用事件池，三个分支最终汇合到同一结尾。
/// </summary>
public sealed class HydrangeaTeaParty : CustomEventModel
{
    private const string IllustriousPage = "ILLUSTRIOUS_BRANCH";
    private const string VictoriousPage = "VICTORIOUS_BRANCH";
    private const string FormidablePage = "FORMIDABLE_BRANCH";
    private const string DescriptionBackdropScenePath = 
        "res://IndomitableSpire2/scenes/vfx/events/events_description_backdrop.tscn";
    
    public override string CustomInitialPortraitPath => 
        "res://IndomitableSpire2/images/events/TeaParty.png";
    
    public override string CustomBackgroundScenePath => 
        "res://IndomitableSpire2/scenes/vfx/events/hydrangea_tea_party.tscn";
    
    /// <summary>
    /// 默认事件布局只预加载立绘，因此显式追加茶会背景和描述底框场景，供入场时从缓存创建。
    /// </summary>
    public override IEnumerable<string> GetAssetPaths(IRunState runState) => 
        base.GetAssetPaths(runState).Append(CustomBackgroundScenePath).Append(DescriptionBackdropScenePath);
    
    /// <summary>
    /// 保留原版标题、描述和选项，将等比铺满的背景放入文字后方的全屏特效容器。
    /// </summary>
    public override void OnRoomEnter()
    {
        ArgumentNullException.ThrowIfNull(Node);
        var background = CreateBackgroundScene().Instantiate<Control>();
        // VfxContainer 已抵消原版事件根节点的垂直偏移，满锚点场景可直接覆盖整个屏幕。
        Node.GetNode<Control>("%VfxContainer").AddChildSafely(background);
        // 默认立绘使用固定尺寸和额外缩放；由新场景统一控制图片比例与裁切，避免重复绘制。
        Node.GetNode<TextureRect>("%Portrait").Hide();
        AddDescriptionBackdrop();
    }
    
    /// <summary>
    /// 将皮肤选择样式的九宫格底框挂在正文后方，自动跟随文字尺寸和原版淡入动画。
    /// </summary>
    private void AddDescriptionBackdrop()
    {
        ArgumentNullException.ThrowIfNull(Node);
        var description = Node.GetNode<RichTextLabel>("%EventDescription");
        // 正文原本至少高 280 像素；取消固定留白，让 FitContent 按当前阶段的实际文字高度收缩。
        description.CustomMinimumSize = new Vector2(description.CustomMinimumSize.X, 0);
        description.AddThemeStyleboxOverride("normal", new StyleBoxEmpty
        {
            ContentMarginLeft = 20,
            ContentMarginTop = 12,
            ContentMarginRight = 20,
            ContentMarginBottom = 12
        });
        // 底框满锚点贴合正文控件，ShowBehindParent 使其位于文字下、茶会背景上且不遮挡输入。
        var backdrop = PreloadManager.Cache.GetScene(DescriptionBackdropScenePath).Instantiate<NinePatchRect>();
        description.AddChildSafely(backdrop);
    }
    
    /// <summary>
    /// 奖励名称直接取自卡牌和药水的本地化，避免事件文本与实际物品名称不一致。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("IllustriousCard", ModelDb.Card<IllustriousAegis>().Title),
        new StringVar("VictoriousCard", ModelDb.Card<VictoriousSong>().Title),
        new StringVar("FormidableCard", ModelDb.Card<FormidablePressure>().Title),
        new StringVar("ShieldPotion", ModelDb.Potion<ShieldPotion>().Title.GetFormattedText()),
        new StringVar("RegenPotion", ModelDb.Potion<RegenPotion>().Title.GetFormattedText()),
        new StringVar("StrengthPotion", ModelDb.Potion<StrengthPotion>().Title.GetFormattedText()),
        new StringVar("FlexPotion", ModelDb.Potion<FlexPotion>().Title.GetFormattedText()),
        new StringVar("FlammablePotion", ModelDb.Potion<FlammablePotion>().Title.GetFormattedText()),
        new StringVar("HypnoticPotion", ModelDb.Potion<HypnoticPotion>().Title.GetFormattedText())
    ];
    
    /// <summary>
    /// 叙事以不挠为主角；联机时各玩家独立选择，因此要求存在任意玩家是不挠（兼容所有皮肤）。
    /// </summary>
    public override bool IsAllowed(IRunState runState) => 
        runState.Players.Any(player => player.Character is Indomitable);
    
    /// <summary>
    /// 初始页面仅提供三位姐姐的邀请，悬停时显示将加入牌组的卡牌及其关键词。
    /// </summary>
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, AcceptIllustrious, $"{Id.Entry}.pages.INITIAL.options.ILLUSTRIOUS",
            HoverTipFactory.FromCardWithCardHoverTips<IllustriousAegis>()),
        new(this, AcceptVictorious, $"{Id.Entry}.pages.INITIAL.options.VICTORIOUS",
            HoverTipFactory.FromCardWithCardHoverTips<VictoriousSong>()),
        new(this, AcceptFormidable, $"{Id.Entry}.pages.INITIAL.options.FORMIDABLE",
            HoverTipFactory.FromCardWithCardHoverTips<FormidablePressure>())
    ];
    
    /// <summary>
    /// 光辉的邀请：获得光辉的庇护，随后选择护盾药水或再生药水。
    /// </summary>
    private async Task AcceptIllustrious()
    {
        await AddInvitationCard<IllustriousAegis>();
        SetEventState(PageDescription(IllustriousPage),
        [
            CreatePotionOption<ShieldPotion>(IllustriousPage, "SHIELD_POTION"),
            CreatePotionOption<RegenPotion>(IllustriousPage, "REGEN_POTION")
        ]);
    }
    
    /// <summary>
    /// 胜利的邀请：获得胜利的高歌，随后选择力量药水或肌肉药水。
    /// </summary>
    private async Task AcceptVictorious()
    {
        await AddInvitationCard<VictoriousSong>();
        SetEventState(PageDescription(VictoriousPage),
        [
            CreatePotionOption<StrengthPotion>(VictoriousPage, "STRENGTH_POTION"),
            CreatePotionOption<FlexPotion>(VictoriousPage, "FLEX_POTION")
        ]);
    }
    
    /// <summary>
    /// 可畏的邀请：获得可畏的威压，随后选择易燃药水或催眠药水。
    /// </summary>
    private async Task AcceptFormidable()
    {
        await AddInvitationCard<FormidablePressure>();
        SetEventState(PageDescription(FormidablePage),
        [
            CreatePotionOption<FlammablePotion>(FormidablePage, "FLAMMABLE_POTION"),
            CreatePotionOption<HypnoticPotion>(FormidablePage, "HYPNOTIC_POTION")
        ]);
    }
    
    /// <summary>
    /// 通过原版入组命令永久添加一张卡牌，保留遗物钩子、历史记录和卡牌获得动画。
    /// </summary>
    private async Task AddInvitationCard<T>() where T : CardModel
    {
        ArgumentNullException.ThrowIfNull(Owner);
        var card = Owner.RunState.CreateCard<T>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        // 与原版事件保持一致，给卡牌入组预览留出时间后再切换分支页面。
        await Cmd.CustomScaledWait(0.5f, 1.2f);
    }
    
    /// <summary>
    /// 将药水模型、实际领取回调和悬停说明绑定到同一选项，避免展示与发奖不一致。
    /// </summary>
    private EventOption CreatePotionOption<T>(string pageKey, string optionKey) where T : PotionModel => 
        new(this, ReceivePotion<T>, 
            $"{Id.Entry}.pages.{pageKey}.options.{optionKey}", 
            HoverTipFactory.FromPotion<T>());
    
    /// <summary>
    /// 六个药水选项共用原版奖励流程，领取结束后统一显示茶会结尾。
    /// </summary>
    private async Task ReceivePotion<T>() where T : PotionModel
    {
        ArgumentNullException.ThrowIfNull(Owner);
        // 奖励界面允许药水槽已满的玩家先丢弃旧药水，也沿用原版的跳过和联机同步规则。
        await RewardsCmd.OfferCustom(Owner, [new PotionReward(ModelDb.Potion<T>().ToMutable(), Owner)]);
        // 原版在完成事件后生成离开按钮，保留完成状态与地图通行的标准处理。
        SetEventFinished(PageDescription("END"));
    }
}