using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

// 保留原 F.20 的模型 ID；两种挂载都是同一模型的临时形态。
public sealed class SeaHornetFighter() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IVersatileCard
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new SeaHornetRockets(), new SeaHornetBombs()]);
    
    public CardModel CardModel => this;
    // 不保存到牌组：实牌离开打出区后卸载，选项/百科预览则保留其挂载。
    public string? SelectedOptionId { get; private set; }
    string? IVersatileCard.SelectedOptionId
    {
        get => SelectedOptionId;
        set => SelectedOptionId = value;
    }
    
    private IEnumerable<IEnumerable<DamageResult>> _loadoutDamageResults = [];
    
    public IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    // 选项将攻击结果交回母卡，由舰载机基类统一结算防空造成的耐久损失。
    internal void RecordLoadoutDamageResults(IEnumerable<IEnumerable<DamageResult>> results)
        => _loadoutDamageResults = results;
    
    protected override int MaxDurability { get; set; } = 15;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    // 关键字：战斗攻击机 + 水平轰炸机 + 编队
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.StrikeFighter, IndomitableKeywords.LevelBomber, IndomitableKeywords.Formation];
    
    protected override IEnumerable<CardTag> SubclassTags => SelectedOptionId switch
    {
        SeaHornetRockets.OptionId => [IndomitableTags.Versatile, IndomitableTags.StrikeFighter],
        SeaHornetBombs.OptionId => [IndomitableTags.Versatile, IndomitableTags.LevelBomber],
        _ => [IndomitableTags.Versatile, IndomitableTags.StrikeFighter, IndomitableTags.LevelBomber]
    };
    
    // CardModel.Tags 会缓存 CanonicalTags，且克隆时不复制该集合；直接按挂载生成，避免修改共享缓存。
    public override IEnumerable<CardTag> Tags => CanonicalTags;
    
    // DynamicVars 在克隆时已经初始化。始终注册两套命名变量，切换挂载只选择使用哪一套。
    protected override IEnumerable<DynamicVar> AdditionalVars => this.CreateVersatileVars();
    
    public override string Title => this.SelectedOption()?.GetTitle(this) ?? base.Title;
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddVersatileDescriptionArgs(description);
    }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => this.GetVersatileHoverTips();
    
    void IVersatileCard.OnVersatileOptionSelected()
    {
        // CanonicalKeywords 只负责首次初始化；克隆后必须操作实例的关键词集合。
        if (SelectedOptionId == SeaHornetBombs.OptionId) RemoveKeyword(IndomitableKeywords.StrikeFighter);
        else AddKeyword(IndomitableKeywords.StrikeFighter);
        if (SelectedOptionId == SeaHornetRockets.OptionId) RemoveKeyword(IndomitableKeywords.LevelBomber);
        else AddKeyword(IndomitableKeywords.LevelBomber);
    }
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return [];
        _loadoutDamageResults = [];
        await VersatileCardCmd.ChooseAndExecute(choiceContext, cardPlay);
        return _loadoutDamageResults;
    }
    
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
    
    protected override void OnUpgrade()
    {
        VersatileCardCmd.UpgradeVersatileOptions(this);
        UpgradeDurability();
    }
}