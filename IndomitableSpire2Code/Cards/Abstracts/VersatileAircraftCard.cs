using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

/// <summary>沿用火把的挂载流程：重放沿用挂载，离开打出区后卸载，预览保留母卡身份。</summary>
public abstract class VersatileAircraftCard(int cost, CardRarity rarity)
    : CarrierAircraftCard(cost, CardType.Attack, rarity, TargetType.AnyEnemy), IVersatileCard
{
    public CardModel CardModel => this;
    // 不保存到牌组：实牌离开打出区后卸载，选项/百科预览则保留其挂载。
    public string? SelectedOptionId { get; private set; }
    string? IVersatileCard.SelectedOptionId
    {
        get => SelectedOptionId;
        set => SelectedOptionId = value;
    }
    
    // 选项将攻击结果交回母卡，由舰载机基类统一结算防空造成的耐久损失。
    private IEnumerable<IEnumerable<DamageResult>> _loadoutDamageResults = [];
    internal void RecordLoadoutDamageResults(IEnumerable<IEnumerable<DamageResult>> results)
        => _loadoutDamageResults = results;
    
    // 选项列表
    public abstract IReadOnlyList<VersatileCardOption> CreateVersatileOptions();
    
    // 选项 id 向舰载机 Tag 和 Keyword 的映射
    protected abstract (CardTag Tag, CardKeyword Keyword) TypeForOption(string id);
    
    // 自动根据映射生成关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        CreateVersatileOptions().Select(option => TypeForOption(option.Id).Keyword);
    
    // 根据选项 id 自动生成标签，没有选择时提供所有选项标签
    protected override IEnumerable<CardTag> SubclassTags =>
        new[] { IndomitableTags.Versatile }.Concat(CreateVersatileOptions()
            .Where(option => SelectedOptionId == null || option.Id == SelectedOptionId)
            .Select(option => TypeForOption(option.Id).Tag));
    
    // CardModel.Tags 会缓存 CanonicalTags，且克隆时不复制该集合；直接按挂载生成，避免修改共享缓存。
    public override IEnumerable<CardTag> Tags => CanonicalTags;
    
    // 根据选项替换卡牌的标题名称
    public override string Title => this.SelectedOption()?.GetTitle(this) ?? base.Title;
    
    // 将多选项的本地化文本添加到本地化文本参数中
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddVersatileDescriptionArgs(description);
    }
    
    // 根据选项提供额外提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => this.GetVersatileHoverTips();
    
    // CanonicalKeywords 只负责首次初始化；克隆后必须操作实例的关键词集合。
    void IVersatileCard.OnVersatileOptionSelected()
    {
        foreach (var option in CreateVersatileOptions())
        {
            var keyword = TypeForOption(option.Id).Keyword;
            if (SelectedOptionId == null || option.Id == SelectedOptionId) AddKeyword(keyword);
            else RemoveKeyword(keyword);
        }
    }
    
    protected virtual Task BeforeLoadout(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
    
    protected virtual Task AfterLoadout(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _loadoutDamageResults = [];
        if (CombatState == null) return [];
        await BeforeLoadout(choiceContext, cardPlay);
        if (await VersatileCardCmd.ChooseAndExecute(choiceContext, cardPlay) != null)
            await AfterLoadout(choiceContext, cardPlay);
        return _loadoutDamageResults;
    }
    
    // 离开打出区后需要重置到无挂载的状态
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && Pile?.Type != PileType.Play) VersatileCardCmd.ResetAfterPileChange(this);
        return base.AfterCardChangedPiles(card, oldPileType, clonedBy);
    }
    
    // 降级会把关键词重建为标准形态，需要重新同步预览的挂载。
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        this.SelectVersatileOption(SelectedOptionId);
    }
    
    protected override void AfterCloned()
    {
        base.AfterCloned();
        _loadoutDamageResults = [];
        this.SelectVersatileOption(null);
    }
    
    protected override void OnUpgrade()
    {
        VersatileCardCmd.UpgradeVersatileOptions(this);
        UpgradeDurability();
    }
}