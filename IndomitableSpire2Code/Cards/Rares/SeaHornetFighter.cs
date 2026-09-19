using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

// 保留原 F.20 的模型 ID；两种挂载都是同一模型的临时形态。
public sealed class SeaHornetFighter() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public enum LoadoutType
    {
        Unmounted,
        Rockets,
        Bombs
    }
    
    // 不保存到牌组：实牌离开打出区后卸载，选项/百科预览则保留其挂载。
    public LoadoutType Loadout { get; private set; }
    
    protected override int MaxDurability { get; set; } = 15;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    // 关键字：战斗攻击机 + 水平轰炸机 + 编队
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.StrikeFighter, IndomitableKeywords.LevelBomber, IndomitableKeywords.Formation];
    
    protected override IEnumerable<CardTag> SubclassTags => Loadout switch
    {
        LoadoutType.Rockets => [IndomitableTags.Versatile, IndomitableTags.StrikeFighter],
        LoadoutType.Bombs => [IndomitableTags.Versatile, IndomitableTags.LevelBomber],
        _ => [IndomitableTags.Versatile, IndomitableTags.StrikeFighter, IndomitableTags.LevelBomber]
    };
    
    // CardModel.Tags 会缓存 CanonicalTags，且克隆时不复制该集合；直接按挂载生成，避免修改共享缓存。
    public override IEnumerable<CardTag> Tags => CanonicalTags;
    
    // DynamicVars 在克隆时已经初始化。始终注册两套命名变量，切换挂载只选择使用哪一套。
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar("RocketDamage", 2M, ValueProp.Move),
        new RepeatVar("RocketRepeat", 8),
        new CustomPowerVar<OnFirePower>("RocketOnFire", 4M),
        new DamageVar("BombDamage", 8M, ValueProp.Move),
        new RepeatVar("BombRepeat", 2),
        new CustomPowerVar<OnFirePower>("BombOnFire", 3M)
    ];
    
    public override string Title => Loadout switch
    {
        LoadoutType.Rockets => new LocString("cards", $"{Id.Entry}.rockets.title").GetFormattedText(),
        LoadoutType.Bombs => new LocString("cards", $"{Id.Entry}.bombs.title").GetFormattedText(),
        _ => base.Title
    } + (IsUpgraded && Loadout != LoadoutType.Unmounted ? "+" : "");
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HasRockets", Loadout == LoadoutType.Rockets);
        description.Add("HasBombs", Loadout == LoadoutType.Bombs);
    }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(IndomitableKeywords.Versatile);
            if (Loadout != LoadoutType.Unmounted) yield break;
            yield return new LoadoutHoverTip(CreateLoadoutPreview(LoadoutType.Rockets));
            yield return new LoadoutHoverTip(CreateLoadoutPreview(LoadoutType.Bombs));
        }
    }
    
    private SeaHornetFighter CreateLoadoutPreview(LoadoutType loadout)
    {
        // 克隆实际数值、升级、耐久和附魔，保证选项与实际效果一致；预览不注册进战斗卡牌集合。
        var preview = (SeaHornetFighter)MutableClone();
        preview.SetLoadout(loadout);
        return preview;
    }
    
    private void SetLoadout(LoadoutType loadout)
    {
        AssertMutable();
        Loadout = loadout;
        // CanonicalKeywords 只负责首次初始化；克隆后必须操作实例的关键词集合。
        if (loadout == LoadoutType.Bombs) RemoveKeyword(IndomitableKeywords.StrikeFighter);
        else AddKeyword(IndomitableKeywords.StrikeFighter);
        if (loadout == LoadoutType.Rockets) RemoveKeyword(IndomitableKeywords.LevelBomber);
        else AddKeyword(IndomitableKeywords.LevelBomber);
    }
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return [];
        // 卡牌初始为未挂载时，提供玩家选择挂载的选项。
        if (Loadout == LoadoutType.Unmounted)
        {
            CardModel[] options =
            [
                CreateLoadoutPreview(LoadoutType.Rockets),
                CreateLoadoutPreview(LoadoutType.Bombs)
            ];
            var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
            if (selected is not SeaHornetFighter selectedHornet) return [];
            SetLoadout(selectedHornet.Loadout);
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        }
        // 如果玩家选择的挂载类型造成不同的伤害和效果：
        switch (Loadout)
        {
            case LoadoutType.Unmounted: goto default;
            case LoadoutType.Rockets:
            {
                ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
                var attack = await DamageCmd.Attack(DynamicVars["RocketDamage"].BaseValue)
                    .WithHitCount(DynamicVars["RocketRepeat"].IntValue)
                    .FromCard(this, cardPlay)
                    .OnlyPlayAnimOnce()
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_fire_burst")
                    .Execute(choiceContext);
                
                if (cardPlay.Target.IsAlive)
                    await PowerCmd.Apply<OnFirePower>(choiceContext, cardPlay.Target,
                        DynamicVars["RocketOnFire"].BaseValue, Owner.Creature, this);
                return attack.Results;
            }
            case LoadoutType.Bombs:
            {
                var bombing = await DamageCmd.Attack(DynamicVars["BombDamage"].BaseValue)
                    .WithHitCount(DynamicVars["BombRepeat"].IntValue)
                    .FromCard(this, cardPlay)
                    .TargetingAllOpponents(CombatState)
                    .WithHitFx("vfx/vfx_fire_burst")
                    .Execute(choiceContext);
                
                await PowerCmd.Apply<OnFirePower>(choiceContext, CombatState.HittableEnemies,
                    DynamicVars["BombOnFire"].BaseValue, Owner.Creature, this);
                return bombing.Results;
            }
            default:
                return [];
        }
    }
    
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        // 返回手牌时立即刷新；弃牌/消耗堆中的预览会从已卸载的模型重新生成。
        if (card == this && Pile?.Type != PileType.Play && Loadout != LoadoutType.Unmounted)
        {
            SetLoadout(LoadoutType.Unmounted);
            NCard.FindOnTable(this)?.UpdateVisuals(Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
        }
        return base.AfterCardChangedPiles(card, oldPileType, clonedBy);
    }
    
    // 降级会把关键词重建为标准形态，需要重新同步预览的挂载。
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        SetLoadout(Loadout);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["RocketDamage"].UpgradeValueBy(1M);
        DynamicVars["RocketOnFire"].UpgradeValueBy(1M);
        DynamicVars["BombDamage"].UpgradeValueBy(3M);
        DynamicVars["BombOnFire"].UpgradeValueBy(1M);
        UpgradeDurability();
    }
    
    // 原生 CardHoverTip 按模型 ID 去重；同一模型的两种挂载必须有不同的提示 ID。
    private sealed class LoadoutHoverTip(SeaHornetFighter card) : CardHoverTip(card), IHoverTip
    {
        string IHoverTip.Id => $"{base.Id}-{card.Loadout}";
    }
}