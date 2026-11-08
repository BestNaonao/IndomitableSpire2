// Resource/localization contract checks; native animation QA is documented separately.
// Usage: node tools/check_mu_skin.mjs (from the repository root)
import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const read = file => fs.readFileSync(file,'utf8').replace(/^\uFEFF/,'');
let checks=0;
const check=(condition,message)=>{assert.ok(condition,message);checks++;};
const root='TashkentSpire2/';
const mu='TASHKENTSPIRE2-TASHKENT_MU';
const old='TASHKENTSPIRE2-TASHKENT_VARIANT_FOUR';
for(const language of ['zhs','eng','jpn','kor']){
  let count=0;
  for(const table of ['characters','ancients','relics']){
    const data=JSON.parse(read(`${root}localization/${language}/${table}.json`));
    for(const [key,value] of Object.entries(data).filter(([key])=>key.includes(old))){
      const alias=key.replace(old,mu);
      check(typeof data[alias]==='string'&&data[alias].length>0,`${language}: missing ${alias}`);
      if(!key.endsWith('.skinName'))check(data[alias]===value,`${language}: gameplay/dialogue differs for ${alias}`);
      count++;
    }
  }
  check(count===55,`${language}: incomplete skin aliases`);
  const characters=JSON.parse(read(`${root}localization/${language}/characters.json`));
  const ui=JSON.parse(read(`${root}localization/${language}/gameplay_ui.json`));
  check(ui['TASHKENTSPIRE2-BATTLE_REWARDS.previewSkin4']===characters[mu+'.skinName'],`${language}: reward preview mismatch`);
}
const manager=read('TashkentSpire2Code/Character/TashkentSkinManager.cs');
check(/Default,\s+VariantTwo,\s+VariantThree,\s+VariantFour,\s+Mu\s*\}/.test(manager),'Existing saved skin ordinals must not change');
check((manager.match(/\[TashkentSkin\./g)??[]).length===5,'Five skin definitions required');
for(const [,resource] of manager.matchAll(/"res:\/\/([^"\n]+\.tscn)"/g))check(fs.existsSync(resource),`Missing skin scene ${resource}`);
for(const scene of ['Tashkent_mu','tashkent_rest_site_mu','tashkent_merchant_mu']){
  const data=read(`${root}scenes/characters/${scene}.tscn`);
  check(data.includes('tashkent_spine_mu.tres'),`${scene}: wrong skeleton`);
  for(const [,resource] of data.matchAll(/path="res:\/\/([^"\n]+)"/g))check(fs.existsSync(resource),`${scene}: missing dependency ${resource}`);
}
const variants=read('TashkentSpire2Code/Character/TashkentSkinVariants.cs');
check(/class TashkentMu : TashkentCharacter/.test(variants),'Mu must inherit gameplay, animations, VFX and config paths');
const panel=read('TashkentSpire2Code/Nodes/TashkentSkinSelectPanel.cs');
check(panel.includes('ModelDb.Character<TashkentMu>()'),'Skin selector must expose Mu');
check(panel.includes('% Skins.Length'),'Selector must wrap through all skins');
check((panel.match(/Lobby.SetLocalCharacter/g)??[]).length===1,'Do not duplicate lobby character-change messages');
const progression=read('TashkentSpire2Code/Patches/TashkentSharedProgressionPatch.cs');
check((progression.match(/ModelDb.GetId<TashkentMu>\(\)/g)??[]).length===2,'Mu stats lookup AND migration must use shared progression');
const rewards=read('TashkentSpire2Code/Nodes/UI/TashkentBattleRewardsAssets.cs');
const paintings=[...rewards.matchAll(/"res:\/\/([^"\n]+_painting\.png)"/g)].map(match=>match[1]);
check(paintings.length===5&&paintings[4].endsWith('tashigan_idol_painting.png'),'Reward painting order must match skin ordinals');
for(const painting of paintings)check(fs.existsSync(painting),`Missing painting ${painting}`);
const view=read(`${root}scenes/ui/tashkent_battle_rewards.gd`);
check(view.includes('"tashigan_idol"')&&view.includes('@export_range(0, 4, 1)'),'Reward preview must expose skin 4');
check(!/clampi\(skin,\s*0,\s*3\)/.test(view),'Reward skin must not clamp Mu to old skin 3');
const skeleton=fs.readFileSync(`${root}animations/characters/Tashkent/tashigan_idol.skel`);
const original=Buffer.from(skeleton.subarray(0,400973));
check(original[10675]===23,'Mu must contain its 22 originals plus the crouch overlay');
original[10675]=22;
check(crypto.createHash('sha256').update(original).digest('hex')==='e62b0cab05b830cc379373debbd219bc9cc2610b43958cb4ccc698027ee924fc','Do not quantize/re-export or modify the original 22 animations');
check(skeleton.length===400973+3071&&skeleton.subarray(400973).includes(Buffer.from('tashkent_charge_crouch')),'Missing dedicated fixed-foot crouch');
for(const file of [`${root}animations/characters/Tashkent/tashigan_idol.atlas`,`${root}animations/characters/Tashkent/tashigan_idol.skel`,`${root}animations/characters/Tashkent/tashigan_idol.png`,paintings[4]]){
  const metadata=read(file+'.import');
  check(metadata.includes(`source_file="res://${file}"`),`Wrong import source: ${file}`);
  const imported=metadata.match(/\npath="res:\/\/([^"\n]+)"/);
  check(imported&&fs.existsSync(imported[1]),`Missing current local import cache: ${file}`);
}
console.log(`PASS ${checks} Mu skin resource, localization, progression and binary-preservation contract checks`);
