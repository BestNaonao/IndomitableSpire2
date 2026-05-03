using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards;

[Pool(typeof(IndomitableCardPool))]
public abstract class IndomitableCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    // Image size:
    // Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    // Full art: 606x852
    // public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    private string? _cachedPortraitPath;
    
    public override string CustomPortraitPath
    {
        get
        {
            if (_cachedPortraitPath != null) return _cachedPortraitPath;
            
            var normalPath = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
            _cachedPortraitPath = ResourceLoader.Exists(normalPath)
                ? normalPath : Rarity != CardRarity.Ancient ? 
                    "beta/indomitable_beta_card.png".CardImagePath() : 
                    "beta/indomitable_beta_ancient_card.png".CardImagePath();
            
            return _cachedPortraitPath;
        }
    }
}