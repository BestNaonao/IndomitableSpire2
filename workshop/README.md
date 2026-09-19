# Tashkent Steam Workshop Artwork

## Output Rules / 成图规则

- 主宣传图固定为 `1200 × 450`，分区横幅固定为 `1200 × 320`。
- 画布外围必须保留真实透明通道；Steam 页面自带的深蓝色不属于图片内容。
- 主标题以英文为主、中文为辅，文字统一由本地脚本绘制，避免生成模型造成拼写错误。
- 角色必须使用项目内真实 Spine 模型离屏渲染，不使用 AI 重绘角色，也不从卡图中裁切替代。
- 底色花纹保持低对比度，边框轮廓清晰，不与标题抢夺视觉层级。
- 配色以冰白、浅紫、藏青和青色为主，仅使用少量银金色勾边。
- 主图展示四套等大装束，左右各两个并保持镜像对称；三个分区横幅分别使用不同的左上角静态 Spine 小人。

## Files / 文件

- `images/tashkent_workshop_hero.png`
- `images/tashkent_workshop_content.png`
- `images/tashkent_workshop_mechanics.png`
- `images/tashkent_workshop_extras.png`
- `steam_workshop_description_zh.md`：中文版创意工坊文案。
- `steam_workshop_description_en.md`：英文版创意工坊文案。
- `steam_workshop_description_ja.md`：日文版创意工坊文案。
- `steam_workshop_description_ko.md`：韩文版创意工坊文案。
- `.build/*_base.png`：内置 ImageGen 生成的透明框体底图。
- `renders/tashkent_spine_1.png` ～ `renders/tashkent_spine_4.png`
- `build_workshop_images.ps1`：将透明框体、精确文字和 Spine 小人合成为最终图片。

## Final ImageGen Prompt Set / 最终生成提示词

四张图片均使用内置 ImageGen 生成透明框体底图，参考 Janus 对应图片的构图与完成度；所有提示词均要求“不生成文字与人物”，最终文字与 Spine 角色由本地脚本合成。

### Hero Base

> Use case: ads-marketing. Asset type: Steam Workshop hero banner base, 1200 x 450 ultra-wide transparent PNG. Create a new original banner frame for the Azur Lane Tashkent character mod for Slay the Spire 2, using the provided Janus banner only as a layout and finish reference. Generate the frame and backdrop only. Inside the frame, use a restrained pale arctic sea with distant icy mountains and a faint fantasy tower silhouette, with subtle Soviet destroyer/navigation motifs. Use icy white, cool lavender, navy, cyan accents and restrained silver-gold trim. Leave a clear title area at the top and open space for four chibi cutouts at the bottom. Genuine transparent alpha outside the frame; no text, letters, numbers, characters, people, logos or watermark; no dark-blue full-canvas background.

### Mod Content Base

> Use case: ads-marketing. Asset type: Steam Workshop section header banner base, 1200 x 320 ultra-wide transparent PNG. Create a new original Mod Content frame using the Janus section header only as a layout and finish reference. Generate frame and decoration only. Use a clean frosted-white inner panel with subtle blueprint silhouettes of naval cards, shells and a destroyer, plus a simple navy/cyan lower accent stripe. Reserve the left end for one chibi cutout and the center for two title lines. Genuine transparent alpha outside the banner; no text, letters, numbers, characters, people, logos or watermark.

### Core Mechanics Base

> Use case: ads-marketing. Asset type: Steam Workshop section header banner base, 1200 x 320 ultra-wide transparent PNG. Create a new original Core Mechanics frame using the Janus section header only as a layout and finish reference. Generate frame and decoration only. Use a pale lavender-white panel with faint technical line art of a horizontal torpedo, distance gauge ticks, ammunition shell and targeting reticle. Reserve the left end for one chibi cutout and the center for two title lines. Genuine transparent alpha outside the banner; no text, letters, numbers, characters, people, logos or watermark.

### Extra Features Base

> Use case: ads-marketing. Asset type: Steam Workshop section header banner base, 1200 x 320 ultra-wide transparent PNG. Create a new original Extra Features frame using the Janus section header only as a layout and finish reference. Generate frame and decoration only. Use a warm ivory-to-pale-lavender panel with faint motifs suggesting four outfits, multiplayer hand/leg selection, a sandworm silhouette and an emperor-crab compass emblem. Reserve the left end for one chibi cutout and the center for two title lines. Genuine transparent alpha outside the banner; no text, letters, numbers, characters, people, logos or watermark.

## Rebuild / 重建

在项目根目录运行：

```powershell
& 'workshop/build_workshop_images.ps1'
```

中英文文案使用以下 GitHub Raw 图片根地址：

`https://raw.githubusercontent.com/BestNaonao/IndomitableSpire2/branch_tashkent/workshop/images/`

在仓库切换为 Public 且 `branch_tashkent` 分支上传图片后，Steam 创意工坊即可直接加载这些地址。
