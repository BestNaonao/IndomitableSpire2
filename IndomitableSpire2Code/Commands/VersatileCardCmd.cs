using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class VersatileCardCmd
{
    public static async Task<VersatileCardOption?> ChooseAndExecute(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not IVersatileCard card)
            throw new ArgumentException("The played card must implement IVersatileCard.", nameof(cardPlay));
        var options = card.CreateVersatileOptions();
        if (card.SelectedOption() is not {} option)
        {
            if (options.Count == 0) return null;
            var previews = options.Select(candidate => candidate.CreatePreview(cardPlay.Card)).ToArray();
            var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, previews, cardPlay.Player);
            // 从原本的 options 中找到其 Id 和选择的 Id 相同的选项
            if (selected is not IVersatileCard { SelectedOptionId: { } selectedId }) return null;
            option = card.FindVersatileOption(selectedId);
            if (option == null) return null;
            card.SelectVersatileOption(option.Id);
            NCard.FindOnTable(cardPlay.Card)?.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        }
        // 按照选择的选项执行效果
        await option.Execute(cardPlay.Card, choiceContext, cardPlay);
        return option;
    }
    
    /// <summary>清除选项并刷新卡面。调用方须先确认：变化的是母卡自身，且当前不在打出区。</summary>
    public static void ResetAfterPileChange(CardModel source)
    {
        if (source is IVersatileCard { SelectedOptionId: not null } versatile)
        {
            versatile.SelectVersatileOption(null);
            NCard.FindOnTable(source)?.UpdateVisuals(source.Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
        }
    }
    
    /// <summary>在母卡升级时升级全部选项的变量；不增加母卡升级等级，也不处理母卡自身的耐久升级。</summary>
    public static void UpgradeVersatileOptions(CardModel source)
    {
        source.AssertMutable();
        if (source is IVersatileCard versatile)
            foreach (var option in versatile.CreateVersatileOptions()) option.Upgrade(source);
    }
}