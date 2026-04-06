using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using Godot;
using TashkentSpire2.TashkentSpire2Code.Cards.Basics;
using TashkentSpire2.TashkentSpire2Code.Relics;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentCharacter : CustomCharacterModel
{
    public const string CharacterId = "Tashkent";
    public override CharacterGender Gender => CharacterGender.Feminine;
    internal static readonly Color TopicColor = new("9A72A1");
    public override Color NameColor => TopicColor;
    protected override CharacterModel? UnlocksAfterRunAs => null;
    public override int StartingHp => 70; // 初始血量
    public override int StartingGold => 99; // 初始金币
    
    // 小地图、对话、能量球相关的颜色设置
    public override Color EnergyLabelOutlineColor => new Color("1E283CF");
    public override Color DialogueColor => new("AAAAAA");
    public override Color MapDrawingColor => new("9A72A1");
    public override Color RemoteTargetingLineColor => new("AAAAAA");
    public override Color RemoteTargetingLineOutline => Colors.Black;
    
    // 在这里添加角色的卡池、药水池和遗物池
    public override CardPoolModel CardPool => ModelDb.CardPool<TashkentCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TashkentPotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TashkentRelicPool>();

    // 设置初始卡组
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        // ModelDb.Card<LoadShot>(),
        ModelDb.Card<RetreatTorpedo>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<EjectionStart>()
    ];
    
    // 获取休息点的视觉场景
    public override string CustomRestSiteAnimPath => CustomVisualPath;
    // 获取商店的视觉场景
    public override string CustomMerchantAnimPath => CustomVisualPath;
    
    public override string CustomIconTexturePath => 
        "res://TashkentSpire2/images/Tashkent/character_icon_tashkent.png"; //小头像
    public override string CustomCharacterSelectIconPath => 
        "res://TashkentSpire2/images/Tashkent/char_select_tashkent.png"; //选择时
    public override string CustomCharacterSelectLockedIconPath => 
        "res://TashkentSpire2/images/Tashkent/char_select_tashkent_locked.png"; //未解锁
    public override string CustomMapMarkerPath =>                   // 地图标记
        "res://TashkentSpire2/images/charui/map_marker_Tashkent.png";
    
    public override string CustomCharacterSelectBg =>               // 选择界面背景
        "res://TashkentSpire2/scenes/characters/char_select_bg_Tashkent.tscn";
    public override string CustomCharacterSelectTransitionPath =>   // 选择专场素材
        "res://TashkentSpire2/materials/Tashkent_transition_mat.tres";
    public override string CustomIconPath =>                        // 角色图标场景
        "res://TashkentSpire2/scenes/characters/Tashkent_icon.tscn";
    public override string CustomEnergyCounterPath =>               // 能量计数器
        "res://TashkentSpire2/scenes/combat/energy_counters/Tashkent_energy_counter.tscn";
    public override string CustomTrailPath =>                       // 卡牌轨迹特效
        "res://TashkentSpire2/scenes/vfx/card_trail_Tashkent.tscn";
    
    public override string CustomArmPointingTexturePath =>
        "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_point.jpg";
    public override string CustomArmRockTexturePath =>
        "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_rock.png";
    public override string CustomArmPaperTexturePath =>
        "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_paper.png";
    public override string CustomArmScissorsTexturePath =>
        "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_scissors.png";

    // // 原版逻辑构建动作映射，传入 Spine 文件中实际命名的动作字符串
    // public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) => SetupAnimationState(
    //     controller: controller, 
    //     idleName: "normal",     // 站立动画
    //     deadName: "dead",       // 死亡动画
    //     hitName: "touch",       // 受击动画
    //     attackName: "attack",       // 攻击动画
    //     castName: "attack_left",    // 释放技能动画
    //     relaxedName: "sleep"    // 休息动画
    //     );
    //
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