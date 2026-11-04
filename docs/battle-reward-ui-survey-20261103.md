# 碧蓝航线风格战后奖励界面：素材与样式调研

2026-11-04 更新：场景已实装，第二套的原 Sprite 几何／独立表情层已恢复，第四套已确认使用 `_rw_tex` 人物层。下文是先前调研记录；当前资源、预览方式与验证边界见 `docs/battle-rewards-scene.md`。

本轮范围：仅本地素材核实与样式预览，不修改奖励 UI、战斗逻辑或运行素材。预览按“左侧保留杀戮尖塔 2 原版奖励框，右侧当前塔什干皮肤立绘，其余装饰采用碧蓝航线结算风格”理解。

## 已核实可用素材

资源根目录：`D:/GitClone/AzurLane/data`。

| 部件 | 已定位资源 | 说明 |
| --- | --- | --- |
| 结算通用 UI | `ui/battleresult_atlas/` | 87 张 PNG，含独立切图与整张打包图集；不应把整张图集直接显示成背景。 |
| 结算页布局线索 | `ui/battleresultui/` | 已导出 UI 相关 MonoBehaviour JSON、局部 FBX；Unity 对象不能直接作为 Godot 界面使用。 |
| 新版结算页 | `ui/newbattleresultgradepage/`、`ui/newbattleresultdisplaypaintingspages/`、`ui/newbattleresultstatisticspage/` | 分别有评级、立绘展示、统计的相关资源；本次预览选择经典结算视觉语汇，不混加无关统计。 |
| S 评级 | `battlescore/battle_score_s/letter_s/letter_S.png` | 已查看原图，橙金色 S。 |
| 大获全胜 | `battlescore/battle_score_s/label_s/label_S.png` | 已查看原图，是中文“大获全胜”金色字样。 |
| 斜切立绘框 | `ui/battleresult_atlas/ship_frame_top.png`、`ship_frame_down_w.png` | 可按屏幕尺寸重新摆放，保证不覆盖奖励交互。 |
| 顶部与评级条 | `ui/battleresult_atlas/bg_top.png`、`rankbg_top.png`、`rankbg_yello.png` | 灰白／深色底条及黄色强调。 |
| 名称／MVP 装饰 | `ui/battleresult_atlas/name_bg.png`、`mvp.png`、`bg_mvp_top.png` | 有原素材。单人界面是否显示 MVP 待设计确认，不据此新增游戏统计。 |

上表 PNG 数量以目录实际枚举为准，Unity 界面层级与最终屏幕布局尚未在原客户端复现；预览是素材约束下的合成设计，不宣称像素级复刻截图。

## 皮肤立绘对应关系

当前项目的 `TashkentCharacter.CurrentSkin` 和 `TashkentSkinManager` 已区分四套皮肤；未来可读取奖励玩家的角色模型来选取立绘，不要读取其他玩家或统一硬编码原皮。

| 项目皮肤 | 碧蓝航线立绘贴图 | 当前导出状态 |
| --- | --- | --- |
| Default | `painting/tashigan_tex/tashigan.png` | 分块贴图；同目录有 `tashigan-mesh.obj`，需要按 mesh/UV 还原。项目已有完整 `TashkentSpire2/images/Tashkent/Tashkent_character.png`，本次作为原皮外观参考。 |
| VariantTwo | `painting/tashigan_2_tex/tashigan_2.png` | 分块贴图；当前目录没有导出的 mesh。`reports/failures.csv` 明确记录该纹理包有一个对象被跳过，原包保留在 `unprocessed/AssetBundles/painting/tashigan_2_tex`；后续需定向补取 mesh，不能当成完整可用立绘。 |
| VariantThree | `painting/tashigan_3_tex/tashigan_3.png` | 同目录有 `tashigan_3-mesh.obj`，需还原后检查边缘、脸部与装备。 |
| VariantFour | `painting/tashigan_4_tex/tashigan_4.png` | 同目录有 `tashigan_4-mesh.obj`，需还原后检查。 |

`painting/tashigan*` 非 `_tex` 目录另有 `MeshImage.json` 等关联资料。不要把角色 Spine 图集、破碎纹理或 AI 重绘当作正式皮肤立绘替代品。原皮本轮只使用已有完整图进行样式参考；其他皮肤未宣称完成还原。

## 保留尖塔奖励逻辑的可行方案

已检查官方源码 `D:/GitClone/Slay the Spire 2/src/Core/Nodes/Screens/NRewardsScreen.cs` 和 `scenes/screens/rewards_screen.tscn`：奖励窗在 `Rewards` 节点下，滚动、领取、选择卡牌、继续按钮和多人等待已有完整逻辑。

未来实现时优先移动／缩放现有 `Rewards` 窗到左侧，保留 `NRewardButton`、滚动遮罩、手柄焦点、跳过与领取逻辑。独立新增背板、右侧立绘及名称条；装饰控件不得截获鼠标。继续按钮只换样式与位置，继续使用原来的状态控制和多人等待行为。

原版奖励素材位于 `D:/GitClone/Slay the Spire 2/images/ui/reward_screen/`：

- `reward_panel.png`：深蓝灰手绘石板底。
- `reward_banner.png`：棕色羊皮横幅。
- `reward_item_button.png`：青绿色领取条。

不要用碧蓝航线掉落格子取代尖塔的卡牌选择／药水／遗物交互。也不要给这个纯外观改动增加 EXP、好感度、舰队面板或新评分机制。S 与“大获全胜”若采用，先视为装饰标题，不做未授权的评价系统。

## 第一版样式预览约束

- 16:9 全屏，左侧约 40% 为原版奖励窗，右侧约 55% 为当前皮肤完整立绘。
- 顶部采用原结算的 S／大获全胜标识；灰白与深蓝灰斜切构图，橙黄色用于继续按钮。
- 原皮展示仅用于确定布局，不代表正式界面只支持原皮。
- 样例金币、卡牌、药水只是排版占位，不变更实际奖励。
- 本轮样式图通过内置 imagegen，输入实际查看过的立绘、原版奖励框／横幅、碧蓝航线图集和 S 标识，奖励行按已核实外观进行文字约束；不是局内运行截图。正式制作仍应直接使用真实还原素材，避免生成图中文字或纹理的细节偏差。

## 交付文件

- 第一版样式图：`D:/GitClone/TashkentSpire2/docs/reward-ui-preview/tashkent-battle-rewards-v1.png`。
- 内置 imagegen 的最终提示词和五张参考图清单：`D:/GitClone/TashkentSpire2/docs/battle-reward-ui-mockup-prompt-20261103.md`。生成时引用原皮完整立绘、原版奖励框、羊皮横幅、碧蓝航线结算图集与 S 标识；奖励行外观通过提示词描述，没有将第六张图作为工具输入。
