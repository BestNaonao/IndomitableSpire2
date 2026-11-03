# 战后奖励界面样式图生成记录

模式：内置 imagegen，非 CLI。仅预览，不作为运行时整张背景贴图；正式界面应使用真实 UI 切图与还原立绘。

参考图依次为：

1. `D:/GitClone/TashkentSpire2/TashkentSpire2/images/Tashkent/Tashkent_character.png`
2. `D:/GitClone/Slay the Spire 2/images/ui/reward_screen/reward_panel.png`
3. `D:/GitClone/Slay the Spire 2/images/ui/reward_screen/reward_banner.png`
4. `D:/GitClone/AzurLane/data/ui/battleresult_atlas/sactx-0-2048x2048-ASTC 6x6-BattleResult-58820ffd.png`
5. `D:/GitClone/AzurLane/data/battlescore/battle_score_s/letter_s/letter_S.png`

## 最终提交提示词

```text
Use case: ui-mockup. Create ONE polished high-resolution 16:9 landscape PC game battle-reward screen style proposal, preferably 1920x1080. Full-screen in-game interface, no device/browser frame, no annotations or watermark.
This is the Tashkent mod for Slay the Spire 2. The surrounding UI must closely resemble Azur Lane's ORIGINAL classic battle-result screen, not a generic futuristic dashboard. Hybrid rule: ORIGINAL Spire reward panel on LEFT, actual Tashkent full illustration on RIGHT; all other chrome Azur Lane battle result.
Reference roles:
Image 1: actual Tashkent original skin illustration, supporting compositing insert. Preserve her exact face, long purple hair, pale blue eyes, blue head accessories, fully covering thick white winter coat, white boots and ice-covered grey/cyan naval rigging. Preserve identity/proportions/art style. Remove only the white surrounding background. No chibi and no costume change.
Image 2: ACTUAL Slay the Spire 2 dark blue-grey rough painterly stone reward panel. Keep this panel's textures and shape recognizable, not a redesigned sci-fi box.
Image 3: ACTUAL Spire tan parchment reward banner. Use with left panel.
Image 4: an authentic Azur Lane result-screen texture ATLAS, many disconnected sprites. Use it ONLY as style/component reference for layered off-white, navy and slate strips, clipped corners, slanted white ship-frame bands, yellow-orange accents and footer. NEVER display the whole atlas or a collage of unrelated fragments.
Image 5: actual orange-gold S victory emblem.
Layout:
TOP left: prominent gold S emblem with large gold Chinese “大获全胜” beside it, small “BATTLE RESULT” and “战斗结束”. Classic Azur Lane clean off-white/grey header band, restrained technical lines. Background layered dark blue-grey and off-white diagonal panels with subtle industrial print. Crisp functional game UI, not an illustration poster; no arbitrary decorative snowflakes or ornate gold borders.
LEFT 40%: approximately x=100..700,y=300..910 on a 1920x1080 canvas. Intact original Spire reward panel with original tan parchment banner, exact header “战利品”. Three generous turquoise painterly rounded reward rows with appropriate small game icons and exact Simplified Chinese labels: “25 金币”, “选择一张卡牌”, “获得药水”. Original Spire reward-box look rather than Azur Lane drop-item tiles. Text clear, high contrast. Character and stripes NEVER cover this panel or its interaction area.
RIGHT 55%: original Tashkent artwork from head to boots, prominent at x=900..1850,y=125..1020, face upper-right. Fit full head, naval rigging and silhouette. Behind character, classic Azur Lane slanted white/slate frame geometry, no busy decoration. Bottom identity plate dark and slanted, exact “TASHKENT” and “塔什干”.
BOTTOM right: one unmistakable Azur Lane-like yellow-orange slanted button “继续”. No duplicate continue buttons. Footer slim, not a heavy control dashboard.
No invented EXP, affection, damage statistics, score numbers, fleet roster, stars, stage labels or combat-condition panels. No cursor. Exact Chinese typography, subtle English secondary labels. Sample rewards are only layout placeholders. This is a faithful high-fidelity style proposal; preserve reference artwork and Spire panel as closely as possible, do not invent alternate character art or modern restyling.
```
