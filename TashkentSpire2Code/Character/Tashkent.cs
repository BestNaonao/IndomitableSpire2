using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;
using TashkentSpire2.TashkentSpire2Code.Cards.Basics;
using TashkentSpire2.TashkentSpire2Code.Relics;
using TashkentSpire2.TashkentSpire2Code.Config;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public class TashkentCharacter : CustomCharacterModel
{
	public const string CharacterId = "Tashkent";
	internal const string AdvanceAnimationName = "move";
	internal const string RetreatAnimationName = "move_left";
	internal const string AdvanceAnimationTrigger = "TashkentAdvance";
	internal const string RetreatAnimationTrigger = "TashkentRetreat";
	public virtual TashkentSkin CurrentSkin => TashkentSkin.Default;
	protected TashkentSkinDefinition CurrentSkinDefinition => TashkentSkinManager.GetDefinition(CurrentSkin);

	public override CharacterGender Gender => CharacterGender.Feminine;
	internal static readonly Color TopicColor = new("#9A72A1");
	public override Color NameColor => TopicColor;
	protected override CharacterModel? UnlocksAfterRunAs => null;
	public override int StartingHp => 70; // 初始血量
	public override int StartingGold => 99; // 初始金币
	
	// 小地图、对话、能量球相关的颜色设置
	public override Color EnergyLabelOutlineColor => new Color("1E283CFF");
	public override Color DialogueColor => new("#9A72A1");
	public override Color MapDrawingColor => new("#9A72A1");
	public override Color RemoteTargetingLineColor => new("#AAAAAA");
	public override Color RemoteTargetingLineOutline => Colors.Black;
	
	// 在这里添加角色的卡池、药水池和遗物池
	public override CardPoolModel CardPool => ModelDb.CardPool<TashkentCardPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<TashkentPotionPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<TashkentRelicPool>();
	
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
		ModelDb.Card<LoadShot>(),
		ModelDb.Card<RetreatTorpedo>()
	];

	public override IReadOnlyList<RelicModel> StartingRelics =>
	[
		ModelDb.Relic<EjectionStart>()
	];
	
	public override string CustomAttackSfx => 
		"res://TashkentSpire2/sfx/tashkent_attacksfx.mp3";
	public override string CustomCastSfx => 
		"res://TashkentSpire2/sfx/tashkent_castsfx.mp3";
	public override string CustomDeathSfx => 
		"res://TashkentSpire2/sfx/tashkent_deathsfx.mp3";
	public override string CharacterSelectSfx => 
		"res://TashkentSpire2/sfx/tashkent_character_select.mp3";
	public override string CharacterTransitionSfx =>
		"res://TashkentSpire2/sfx/tashkent_character_transition.mp3";
	
	public override string CustomIconTexturePath =>                 //选择时
		"res://TashkentSpire2/images/Tashkent/character_icon_tashkent.png";  
	public override string CustomCharacterSelectIconPath =>         //选择时
		"res://TashkentSpire2/images/Tashkent/char_select_tashkent.png"; 
	public override string CustomIconOutlineTexturePath =>			//带边框
		"res://TashkentSpire2/images/Tashkent/character_icon_tashkent_outline.png"; 
	public override string CustomCharacterSelectLockedIconPath =>   //未解锁
		"res://TashkentSpire2/images/Tashkent/char_select_tashkent_locked.png"; 
	public override string CustomMapMarkerPath =>                   // 地图标记
		"res://TashkentSpire2/images/Tashkent/map_marker_tashkent.png";
	
	public override string CustomVisualPath =>                      //人物模型
		CurrentSkinDefinition.VisualPath;
	public override string CustomTrailPath =>                       // 卡牌轨迹特效
		"res://TashkentSpire2/scenes/vfx/card_trail_tashkent.tscn";
	public override string CustomRestSiteAnimPath =>                // 篝火休息
		CurrentSkinDefinition.RestSiteAnimPath;
	public override string CustomMerchantAnimPath =>                // 商店场景
		CurrentSkinDefinition.MerchantAnimPath;
	public override string CustomCharacterSelectBg =>               // 选择界面背景
		"res://TashkentSpire2/scenes/characters/char_select_bg_Tashkent.tscn";
	public override string CustomCharacterSelectTransitionPath =>   // 选择专场素材
		"res://TashkentSpire2/materials/tashkent_transition_mat.tres";
	public override string CustomIconPath =>                        // 角色图标场景
		"res://TashkentSpire2/scenes/characters/Tashkent_icon.tscn";
	public override string CustomEnergyCounterPath =>               // 能量计数器
		"res://TashkentSpire2/scenes/vfx/tashkent_energy_counter.tscn";

	// Keep persistent combat visuals warm. These effects reuse a small, fixed number
	// of Sprite2Ds and never consume gameplay RNG.
	protected override IEnumerable<string> ExtraAssetPaths =>
		NTorpedoVfx.AssetPaths
			.Concat(NPoseidonFormVfx.AssetPaths)
			.Concat(NWarpDriveVfx.AssetPaths);
	
	// public override string CustomArmPointingTexturePath =>
	//     "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_point.png";
	// public override string CustomArmRockTexturePath =>
	//     "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_rock.png";
	// public override string CustomArmPaperTexturePath =>
	//     "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_paper.png";
	// public override string CustomArmScissorsTexturePath =>
	//     "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_scissors.png";
	
	public override string CustomArmPointingTexturePath =>
		TashkentConfig.ReplaceHandWithLegInMultiplayer
			? "res://TashkentSpire2/images/Tashkent/feet/multiplayer_foot_tashkent_point.png"
			: "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_point.png";

	public override string CustomArmRockTexturePath =>
		TashkentConfig.ReplaceHandWithLegInMultiplayer
			? "res://TashkentSpire2/images/Tashkent/feet/multiplayer_foot_tashkent_rock.png"
			: "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_rock.png";

	public override string CustomArmPaperTexturePath =>
		TashkentConfig.ReplaceHandWithLegInMultiplayer
			? "res://TashkentSpire2/images/Tashkent/feet/multiplayer_foot_tashkent_paper.png"
			: "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_paper.png";

	public override string CustomArmScissorsTexturePath =>
		TashkentConfig.ReplaceHandWithLegInMultiplayer
			? "res://TashkentSpire2/images/Tashkent/feet/multiplayer_foot_tashkent_scissors.png"
			: "res://TashkentSpire2/images/Tashkent/hands/multiplayer_hand_tashkent_scissors.png";

	public override RelicIconData? CustomYummyCookie =>
		new RelicIconData(
			BigIconPath: "res://TashkentSpire2/images/relics/big/YummyCookie_tashkent.png",
			PackedIconPath: "res://TashkentSpire2/images/relics/packed/YummyCookie_tashkent.png",
			PackedIconOutlinePath: "res://TashkentSpire2/images/relics/outline/YummyCookie_tashkent.png"
		);
	
	// 将游戏的通用角色动作映射到这套 Spine 文件中的实际动画名。
	public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller)
	{
		CreatureAnimator animator = SetupAnimationState(
			controller: controller,
			idleName: "normal",
			deadName: "dead",
			hitName: "touch",
			attackName: "attack",
			castName: "attack_left",
			relaxedName: "sleep"
		);

		// DistancePower explicitly returns these looping movement poses to Idle when
		// its position tween finishes. Keeping them in CreatureAnimator means attack,
		// hit, and death triggers can still interrupt them through the normal pipeline.
		animator.AddAnyState(AdvanceAnimationTrigger, new AnimState(AdvanceAnimationName, true));
		animator.AddAnyState(RetreatAnimationTrigger, new AnimState(RetreatAnimationName, true));
		return animator;
	}

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
