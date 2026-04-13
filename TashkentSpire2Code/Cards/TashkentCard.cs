using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Extensions;

namespace TashkentSpire2.TashkentSpire2Code.Cards;

[Pool(typeof(TashkentCardPool))]
public abstract class TashkentCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    public sealed override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, CombatState state)
    {
        await base.BeforeHandDraw(player, ctx, state);

        if (Enchantment != null)
        {
            await Enchantment.BeforeHandDraw(player, ctx, state);
        }
    }
}