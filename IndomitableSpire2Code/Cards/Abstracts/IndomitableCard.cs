using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

[Pool(typeof(IndomitableCardPool))]
public abstract class IndomitableCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
) : IndomitableSpire2Card(baseCost, type, rarity, target, showInCardLibrary, autoAdd);