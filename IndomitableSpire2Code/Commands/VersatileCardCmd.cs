using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class VersatileCardCmd
{
    public static async Task<VersatileCardOption?> ChooseAndExecute(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not IVersatileCard card)
            throw new ArgumentException("The played card must implement IVersatileCard.", nameof(cardPlay));
        var options = card.CreateVersatileOptions();
        var option = options.FirstOrDefault(candidate => candidate.Id == card.SelectedOptionId);
        if (option == null)
        {
            if (options.Count == 0) return null;
            var previews = options.Select(candidate => candidate.CreatePreview(cardPlay.Card)).ToArray();
            var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, previews, cardPlay.Player);
            // 从原本的 options 中找到其 Id 和选择的 Id 相同的选项
            if (selected is not IVersatileCard { SelectedOptionId: { } selectedId }) return null;
            option = options.FirstOrDefault(candidate => candidate.Id == selectedId);
            if (option == null) return null;
            card.SelectVersatileOption(option.Id);
            NCard.FindOnTable(cardPlay.Card)?.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        }
        // 按照选择的选项执行效果
        await option.Execute(cardPlay.Card, choiceContext, cardPlay);
        return option;
    }
}