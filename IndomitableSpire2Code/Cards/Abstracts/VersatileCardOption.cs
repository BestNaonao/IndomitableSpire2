using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

/// <summary>
/// 多用途效果的定义，不是独立卡牌模型，也不注册卡池。
/// Id 区分选项/提示；预览保留母卡模型 ID、卡池、费用、稀有度及全部实例状态。
/// </summary>
public abstract class VersatileCardOption(string id, int? durabilityCost = null, string? localizationKey = null)
{
    public string Id { get; } = id;
    public int? DurabilityCost { get; } = durabilityCost;
    
    public abstract Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay);
    
    /// <summary>每次调用创建新的变量；由母卡注册、克隆和保存，不能缓存在共享选项中。</summary>
    public virtual IEnumerable<DynamicVar> CreateVars() => [];
    
    /// <summary>仅升级本选项在母卡上的变量；无论当前选中哪个选项，母卡升级时都会调用。</summary>
    public virtual void Upgrade(CardModel source) { }
    
    // 默认按母卡和选项 ID 组织本地化；特殊选项仍可提供自己的条目路径。
    private string GetLocalizationKey(CardModel source) => localizationKey ?? $"{source.Id.Entry}.{Id}";
    
    public string GetTitle(CardModel source)
        => new LocString("cards", $"{GetLocalizationKey(source)}.title").GetFormattedText() + (source.IsUpgraded ? "+" : "");
    
    // 用于支持在母卡上显示选项的效果描述
    public void AddDescriptionArgs(CardModel source, LocString description)
        => description.AddObj("OptionDescription", new OptionDescription(description, GetLocalizationKey(source)));
    
    protected LocString GetSelectionPrompt(CardModel source)
    {
        var prompt = new LocString("cards", $"{GetLocalizationKey(source)}.selectionScreenPrompt");
        source.DynamicVars.AddTo(prompt);
        return prompt;
    }
    
    public CardModel CreatePreview(CardModel source)
    {
        var preview = (CardModel)source.MutableClone();
        ((IVersatileCard)preview).SelectVersatileOption(Id);
        return preview;
    }
    
    public IHoverTip CreateHoverTip(CardModel source) => new OptionHoverTip(CreatePreview(source), Id);
    
    // CardModel.Description 不是虚属性；通过母卡描述的占位符展示选项文本。
    // 原版在 AddExtraArgsToDescription 之后才加入 IfUpgraded 等变量，必须延迟到格式化时读取完整上下文。
    private sealed class OptionDescription(LocString context, string key)
    {
        public override string ToString()
        {
            var description = new LocString("cards", $"{key}.description");
            description.AddVariablesFrom(context);
            return description.GetFormattedText();
        }
    }
    
    // 重写提示的 Id 以避免不同选项因使用的母卡 Id 相同而顶替。
    private sealed class OptionHoverTip(CardModel card, string optionId) : CardHoverTip(card), IHoverTip
    {
        string IHoverTip.Id => $"{base.Id}-{optionId}";
    }
}