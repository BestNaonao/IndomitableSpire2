using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class VersatileExtensions
{
    public static VersatileCardOption? FindVersatileOption(this IVersatileCard versatileCard, string? optionId) => 
        optionId == null ? null : versatileCard.CreateVersatileOptions().FirstOrDefault(option => option.Id == optionId);
    
    public static VersatileCardOption? SelectedOption(this IVersatileCard versatileCard) => 
        versatileCard.FindVersatileOption(versatileCard.SelectedOptionId);
    
    public static void SelectVersatileOption(this IVersatileCard versatileCard, string? optionId)
    {
        versatileCard.CardModel.AssertMutable();
        if (optionId != null && versatileCard.FindVersatileOption(optionId) == null)
            throw new ArgumentException("Unknown versatile option.", nameof(optionId));
        versatileCard.SelectedOptionId = optionId;
        // 相同 ID 也要重新应用：降级会重建关键词，预览需要恢复对应形态。
        versatileCard.OnVersatileOptionSelected();
    }
    
    public static void AddVersatileDescriptionArgs(this IVersatileCard versatileCard, LocString description)
    {
        var option = versatileCard.SelectedOption();
        description.Add("HasOption", option != null);
        option?.AddDescriptionArgs(versatileCard.CardModel, description);
    }
    
    public static IEnumerable<IHoverTip> GetVersatileHoverTips(this IVersatileCard versatile, params IHoverTip[] additionalTips)
    {
        yield return HoverTipFactory.FromKeyword(IndomitableKeywords.Versatile);
        foreach (var tip in additionalTips) yield return tip;
        if (versatile.SelectedOption() != null) yield break;
        foreach (var option in versatile.CreateVersatileOptions()) yield return option.CreateHoverTip(versatile.CardModel);
    }
    
    // 汇总全部选项新建的变量，交由母卡注册；不受当前选择影响。实际变量归母卡所有，共享选项对象不得缓存这些变量。
    public static IEnumerable<DynamicVar> CreateVersatileVars(this IVersatileCard versatileCard) =>
        versatileCard.CreateVersatileOptions().SelectMany(option => option.CreateVars());
}