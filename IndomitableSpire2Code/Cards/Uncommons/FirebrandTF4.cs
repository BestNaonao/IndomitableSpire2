using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

// 两种挂载沿用同一母卡模型；重放保持挂载，离开打出区后卸载。
public sealed class FirebrandTF4() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IVersatileCard
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new FirebrandTorpedoes(), new FirebrandBombs()]);
    
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
    
    protected override int MaxDurability { get; set; } = 11;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    // 挂载同步机种关键词、标签及 AircraftIcons 卡框图标。
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.TorpedoBomber, IndomitableKeywords.DiveBomber];
    
    protected override IEnumerable<CardTag> SubclassTags => SelectedOptionId switch
    {
        FirebrandTorpedoes.OptionId => [IndomitableTags.Versatile, IndomitableTags.TorpedoBomber],
        FirebrandBombs.OptionId => [IndomitableTags.Versatile, IndomitableTags.DiveBomber],
        _ => [IndomitableTags.Versatile, IndomitableTags.TorpedoBomber, IndomitableTags.DiveBomber]
    };
    
    // CardModel.Tags 会缓存 CanonicalTags，且克隆时不复制该集合；直接按挂载生成，避免修改共享缓存。
    public override IEnumerable<CardTag> Tags => CanonicalTags;
    
    // DynamicVars 在克隆时已经初始化。始终注册两套命名变量，切换挂载只选择使用哪一套。
    protected override IEnumerable<DynamicVar> AdditionalVars => [..this.CreateVersatileVars(), new ReconVar(3M)];
    
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
        if (SelectedOptionId == FirebrandBombs.OptionId) RemoveKeyword(IndomitableKeywords.TorpedoBomber);
        else AddKeyword(IndomitableKeywords.TorpedoBomber);
        if (SelectedOptionId == FirebrandTorpedoes.OptionId) RemoveKeyword(IndomitableKeywords.DiveBomber);
        else AddKeyword(IndomitableKeywords.DiveBomber);
    }
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return [];
        _loadoutDamageResults = [];
        if (await VersatileCardCmd.ChooseAndExecute(choiceContext, cardPlay) != null)
            await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
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