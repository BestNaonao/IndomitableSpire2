using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class EjectionStart : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 大图标（通常用于查看遗物大图和通关结算）
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/ShikikanDakimakura.png";
    // 小图标和轮廓图的路径
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/ShikikanDakimakura_packed.tres";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/ShikikanDakimakura_outline.tres";
    
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 如果角色已经死亡，直接跳过（参考了燃烧之血的防崩溃处理）
        if (side != Owner.Creature.Side || Owner.Creature.IsDead)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        // await PowerCmd.Apply<MotivationPower>(Owner.Creature, DynamicVars["MotivationPower"].BaseValue, Owner.Creature, null);
    }
}