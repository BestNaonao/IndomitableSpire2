using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Basics;
using IndomitableSpire2.IndomitableSpire2Code.Relics;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public enum IndomitableSkin
{
    Default,
    Maid
}

// 【核心】：必须是 abstract，该类只是玩法数据的容器，不是一个具体角色
public abstract class Indomitable : CustomCharacterModel
{
    public const string CharacterId = "Indomitable";
    public override CharacterGender Gender => CharacterGender.Feminine;
    internal static readonly Color TopicColor = new("FFFFFF");
    
    // 角色名称的颜色，STS2 使用 Godot 的 Color 结构体
    public override Color NameColor => TopicColor; // 替换为不挠的主题色，比如白色或淡蓝

    public override int StartingHp => 69; // 初始血量
    public override int StartingGold => 99; // BaseLib 默认是 99，你可以重写修改

    // 小地图、对话、能量球相关的颜色设置
    public override Color EnergyLabelOutlineColor => new Color("1E283CFF");
    public override Color DialogueColor => new("AAAAAA");
    public override Color MapDrawingColor => new("AAAAAA");
    public override Color RemoteTargetingLineColor => new("AAAAAA");
    public override Color RemoteTargetingLineOutline => Colors.Black;
    
    // 在这里添加角色的卡池、药水池和遗物池
    public override CardPoolModel CardPool => ModelDb.CardPool<IndomitableCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IndomitablePotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<IndomitableRelicPool>();

    // 设置初始卡组
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIndomitable>(),
        ModelDb.Card<StrikeIndomitable>(),
        ModelDb.Card<StrikeIndomitable>(),
        ModelDb.Card<StrikeIndomitable>(),
        ModelDb.Card<DefendIndomitable>(),
        ModelDb.Card<DefendIndomitable>(),
        ModelDb.Card<DefendIndomitable>(),
        ModelDb.Card<DefendIndomitable>(),
        ModelDb.Card<TakeABreak>(),
        ModelDb.Card<Ignite>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<ShikikanDakimakura>()
    ];
    
    // ... 同样的方式替换选人界面的立绘、头像等 ...
    public override string CustomCharacterSelectBg =>               // 选择界面背景
        $"res://IndomitableSpire2/scenes/characters/char_select_bg_indomitable_{new Random().Next(1, 5)}.tscn";
    public override string CustomCharacterSelectIconPath =>         // 选择界面图标
        "res://IndomitableSpire2/images/charui/char_select_indomitable.png";
    public override string CustomCharacterSelectLockedIconPath =>   // 选择锁定图标
        "res://IndomitableSpire2/images/charui/char_select_indomitable_locked.png";
    public override string CustomCharacterSelectTransitionPath =>   // 选择专场素材
        "res://IndomitableSpire2/materials/indomitable_transition_mat.tres";
    public override string CustomIconTexturePath =>                 // 小图标
        "res://IndomitableSpire2/images/charui/character_icon_indomitable.png";
    public override string CustomIconPath =>                        // 角色图标场景
        "res://IndomitableSpire2/scenes/characters/indomitable_icon.tscn";
    public override string CustomMapMarkerPath =>                   // 地图标记
        "res://IndomitableSpire2/images/charui/map_marker_indomitable.png";
    public override string CustomEnergyCounterPath =>               // 能量计数器
        "res://IndomitableSpire2/scenes/combat/energy_counters/indomitable_energy_counter.tscn";
    public override string CustomTrailPath =>                       // 卡牌轨迹特效
        "res://IndomitableSpire2/scenes/vfx/card_trail_indomitable.tscn";

    // 原版逻辑构建动作映射，传入 Spine 文件中实际命名的动作字符串
    public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) => SetupAnimationState(
        controller: controller, 
        idleName: "normal",     // 站立动画
        deadName: "dead",       // 死亡动画
        hitName: "touch",       // 受击动画
        attackName: "attack",       // 攻击动画
        castName: "attack_left",    // 释放技能动画
        relaxedName: "sleep"    // 休息动画
        );
    
    public override List<string> GetArchitectAttackVfx()
    {
        const int num = 5;
        var list = new List<string>(num);
        CollectionsMarshal.SetCount(list, num);
        var span = CollectionsMarshal.AsSpan(list);
        span[0] = "vfx/vfx_attack_slash";
        span[1] = "vfx/vfx_fire_burst";
        span[2] = "vfx/vfx_fire_burning";
        span[3] = "vfx/vfx_rock_shatter";
        span[4] = "vfx/vfx_fire_smoke_puff";
        return list;
    }
}