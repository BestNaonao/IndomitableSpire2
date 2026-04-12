// using System;
// using HarmonyLib;
// using MegaCrit.Sts2.Core.Animation;
// using MegaCrit.Sts2.Core.Bindings.MegaSpine;
// using MegaCrit.Sts2.Core.Modding;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.Characters;
// using MegaCrit.Sts2.Core.Nodes.Combat;
//
// namespace IndomitableSpire2.IndomitableSpire2Code.Patches;
//
// [ModInitializer("Init")]
// public static class Entry
// {
//     // 替换哪个角色
//     private const string TargetCharacterId = "IRONCLAD";
//     
//     // Harmony的ID，确保唯一性
//     private const string HarmonyId = "sts2.bestnaonao.spineskinreplacement";
//     
//
//     public static void Init()
//     {
//         new Harmony(HarmonyId).PatchAll();
//     }
//
//     [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
//     private static class NCreature_Ready_Patch
//     {
//         private static void Postfix(NCreature __instance)
//         {
//             var player = __instance?.Entity?.Player;
//             if (player == null) return;
//
//             if (!string.Equals(player.Character.Id.Entry, TargetCharacterId, StringComparison.OrdinalIgnoreCase))
//                 return;
//
//             var visuals = __instance.Visuals;
//             float desiredScale = 1.0F;
//             visuals.Body.Scale = new Godot.Vector2(desiredScale, desiredScale);
//
//         }
//     }
// }
//
// [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.GenerateAnimator))]
// public class ReplaceIroncladAnimatorPatch
// {
//     // Prefix 返回 bool 类型。返回 false 意味着阻断原方法的执行。
//     [HarmonyPrefix]
//     static bool Prefix(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
//     {   
//         // 只拦截 Ironclad 实例
//         if (__instance is not Ironclad)
//         {
//             return true;
//         }
//         // 1. 初始化不挠（Indomitable）的专属动画状态
//         // true 表示这是一个循环动画
//         AnimState idleState = new AnimState("normal", true);     // 站立
//         AnimState deadState = new AnimState("dead");             // 死亡
//         AnimState hurtState = new AnimState("touch");            // 受击
//         AnimState attackState = new AnimState("attack");         // 攻击
//         AnimState castState = new AnimState("attack_left");      // 释放技能
//         AnimState relaxedState = new AnimState("sleep", true);   // 休息
//
//         // 2. 设置动画播放完毕后的自动连段（回到站立状态）
//         castState.NextState = idleState;
//         attackState.NextState = idleState;
//         hurtState.NextState = idleState;
//         // 休息状态被打断或结束时分支回到站立
//         relaxedState.AddBranch("Idle", idleState);
//
//         // 3. 构建新的 Animator，并绑定状态机字典
//         CreatureAnimator animator = new CreatureAnimator(idleState, controller);
//             
//         // 将游戏底层的状态键值（"Idle", "Dead" 等）强行映射到不挠的状态上
//         animator.AddAnyState("Idle", idleState);
//         animator.AddAnyState("Dead", deadState);
//         animator.AddAnyState("Hit", hurtState);
//         animator.AddAnyState("Attack", attackState);
//         animator.AddAnyState("Cast", castState);
//         animator.AddAnyState("Relaxed", relaxedState);
//
//         // 4. 将我们做好的 animator 赋值给原方法的返回值 __result
//         __result = animator;
//
//         // 5. 返回 false！这是最关键的一步，告诉引擎：不要去执行原本铁甲战士的代码了
//         return false;
//     }
// }