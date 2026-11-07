# 炮弹选择七档滑条

直接修改原有 `TashkentSpire2/scenes/ui/tashkent_selection.tscn`，未新建另一套选择弹窗。卡牌、按钮及选择区采用当前场景中用户调整后的布局；保留左右箭头、确认、查看战场。

- 固定显示 0～6 七个点，原生 HSlider 步长为 1，可直接点击或拖动吸附。方向键/手柄沿用游戏 MegaInput 左右操作。
- 青色表示现有弹药可覆盖的数量；灰色叉号表示当前不可选；拥有弹幕时，超出现有弹药但允许选择的档位用橙色显示。
- 现有弹药数量在对应点上方标注；大于 6 的实际弹药数量完整显示，标注停靠在最右端。
- 集火门槛读取正在打出的卡牌 `TashkentSpire2-Shot.BaseValue`，向上取整，与 `shellsLoaded >= BaseValue` 的结算条件一致。11 个调用点均传入自身卡牌，仅保留门槛点的金色圆环及短标记线，不显示集火说明文字或悬停提示。
- 选择区不绘制背景或边框，仅保留七档轨道、数字、头像手柄、现有弹药标注和集火位置标记。不显示发射数量、选择数量、剩余/超额或集火达成说明；不显示滑条悬停提示。
- 滑条只改变预览，不发射、不写入模型弹药量、不发送网络消息。确认后仍由 GetShellCountcmd 原有 PlayerChoiceSynchronizer 同步唯一整数；远端和回放不会打开本地选择界面。查看战场时整个选择区一并隐藏，关闭界面时维持原有返回 0 的行为。
- 中英日韩共用相同节点和数值规则，字体使用游戏本地化替代字体。

## 原版按钮及输入提示

查看战场沿用用户当前布局，动作绑定仍由原版 NPeekButton / NConfirmButton 处理。确认按钮将用户设置的根节点 2 倍缩放烘焙为实际 400×220 的控件尺寸，根节点 scale 为 1，内部小人图标框为 160×160，仍用 KeepAspectCentered。枢轴及子节点尺寸同步翻倍；根据原版 OnWindowChange 定位公式补偿实例偏移，保持修改前的屏幕可见位置和悬停中心不变。原版 1.05 悬停、0.95 按下及回到 1 的缩放代码不作替换。装饰图不截断按钮的鼠标事件。

旧版游戏 NButton 管理 `%ControllerIcon`，新版改为管理 `%HotkeyIcon`。TashkentButtonHotkeyIcon 只在运行游戏存在原版 `scenes/ui/hotkey_icon.tscn` 时安装该节点，并隐藏失效的静态 A 占位图；旧版继续使用原生 ControllerIcon 流程。鼠标、手柄及纯键盘提示均由各自原版按钮管理，不调用 ForceMouseMode、不更改全局输入模式、不重绑动作。旧占位图在场景默认隐藏，避免初始化闪现。

确认小人下方增加 220×64 的碧蓝航线金黄色确认条，与小人同属一个原版按钮，共用悬停、按下、停用及确认动作。仅扩展确认条所在区域的命中测试，不改变根节点 400×220 的几何、枢轴或原版定位公式。文字分别为“确认 / Confirm / 確認 / 확인”，使用游戏对应语言字体；切换语言返回时刷新。底图 `images/ui/shell_selection/confirm_button.png` 复制自碧蓝航线解包 `data/ui/commonui_atlas/btn_big_yellow.png`，未修改源文件，无额外每帧更新。

新增确认条通过 28 项 Godot 检查，包括旧节点屏幕矩形不变、四语言文字及字形、实际鼠标点击超出原根矩形的下半条区域、禁用/隐藏不可点击，以及原版悬停恢复；原有 648 项选择流程及协议分支回归仍通过。测试入口为隔离目录 `tashkent-button-glyph-qa/modern/confirm_caption.tscn`。

## 手柄素材

`TashkentSpire2/images/ui/shell_selection/tashkent_handle.png` 直接复制自碧蓝航线解包 `D:/GitClone/AzurLane/data/qicon/tashigan/tashigan.png`，为塔什干透明 Q 版头像，未修改源文件。控件显示为 64×64，不逐帧创建纹理或加载图片。

## 验证及发布

完整模组编译输出到隔离测试目录，未部署游戏。Godot 原生交互测试脚本位于 `D:/GitClone/.codex-build/tashkent-ammo-slider-qa/selector_qa.gd`，覆盖七档点击、拖动、边界限制、弹幕超额、隐藏、四语言文本布局及尺寸变化，另检查背景、额外文字节点和悬停提示确实不存在。截图使用修改后场景中提取的原始选择区节点及原始脚本，不是另画的示意图。

`D:/GitClone/.codex-build/tashkent-button-glyph-qa` 使用新旧两套原版 NButton、NHotkeyIcon 源码及框架单例测试替身，检查鼠标/手柄/纯键盘显隐、唯一节点归属、绑定不重复、停用启用和退出解绑。尚未在实际游戏对局内操作验证。

其中 modern/ConfirmScaleQA.cs 使用原版 NConfirmButton 源码和真实 Godot 动画帧，先复现旧根节点 scale=2 时悬停缩小，再对比烘焙前后控件、图标、输入提示的屏幕矩形及枢轴，检查原版悬停/按下/移出恢复和不同分辨率下的屏幕相对定位。

本功能同时修改 C#、场景、GDScript、本地化和新增图片，发布时需重新构建 DLL 并重新导出 PCK，二者一同更新；不能仅替换 DLL。

2026-11-07 的报文修复及额外流程检查见 [弹药选择回归报告](ammo-selector-regression-20261107.md)。该轮仅修改 C#，没有再调整用户的场景布局、按钮大小或滑条内容。
