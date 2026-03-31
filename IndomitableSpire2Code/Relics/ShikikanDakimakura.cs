using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class ShikikanDakimakura : IndomitableRelic
{
    // 设为初始遗物
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 大图标（通常用于查看遗物大图和通关结算）
    protected override string BigIconPath => 
        "res://IndomitableSpire2/images/relics/big/ShikikanDakimakura.png";
    // 小图标和轮廓图的路径
    public override string PackedIconPath => 
        "res://IndomitableSpire2/images/relics/packed/ShikikanDakimakura_packed.tres";
    protected override string PackedIconOutlinePath => 
        "res://IndomitableSpire2/images/relics/outline/ShikikanDakimakura_outline.tres";

    // 定义动态变量：恢复 1 点生命值。参考了原版燃烧之血的写法。
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new HealVar(1M), new PowerVar<MotivationPower>(10M)];

    // 在回合结束时恢复 1 点生命值。
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 如果角色已经死亡，直接跳过（参考了燃烧之血的防崩溃处理）
        if (side != Owner.Creature.Side || Owner.Creature.IsDead)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        await PowerCmd.Apply<MotivationPower>(Owner.Creature, DynamicVars["MotivationPower"].BaseValue, Owner.Creature, null);
    }
}