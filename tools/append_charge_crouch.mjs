// Append a small, event-free Spine 4.2 overlay. Do NOT re-export the skeleton:
// a JSON round trip would quantize every original animation/mesh/curve.
// Usage: node tools/append_charge_crouch.mjs INPUT_DIRECTORY OUTPUT_DIRECTORY [SKIN]
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

const clipName = 'tashkent_charge_crouch';
const baselines = [
  {skin:'tashigan', pelvis:'char', depth:17, feet:[[-11.357248,5.235985],[15.974894,5.8493137]],sha256:'75b19fc124a1a5105d7833511f61de504b8d7d345e6fa114e8e2a6ec50448c33'},
  {skin:'tashigan_2', pelvis:'char', depth:18, feet:[[-11.513166,.8456421],[13.333812,1.8686237]],sha256:'ed895878b677fdaaf0f00f400136d676ef2bd395430e2cf48051f5cf26dd65d8'},
  {skin:'tashigan_3', pelvis:'bone', depth:18, feet:[[-11.17189,2.1407928],[13.153049,2.3743439]],sha256:'1608cc3c93d69f27ef1dfc59f7b2b6d800bb14accd5eee0ca5fbbacb4552bf16'},
  {skin:'tashigan_4', pelvis:'char', depth:18, feet:[[9.645512,.5530052],[-13.704084,-.10656738]],sha256:'932a447f5e9ac3ef82932d5950048275481c59b556d02e5f460f7c651fd48de2'},
  // Mu's source has no IK constraints. Bake the same fixed-foot rotations
  // against its own thigh/shin chains, without changing the original rig.
  {skin:'tashigan_idol', pelvis:'bone', depth:18, animationCount:22,
    legs:[['leg_L1','leg_L3'],['leg_R1','leg_R3']],
    feet:[[-13.87812,5.055639],[11.22825,3.851126]],
    sha256:'e62b0cab05b830cc379373debbd219bc9cc2610b43958cb4ccc698027ee924fc'},
];

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const varint = n => {const out=[]; do {let b=n&127; n>>>=7;out.push(b|(n?128:0));}while(n);return Buffer.from(out);};
const string = text => Buffer.concat([varint(Buffer.byteLength(text)+1),Buffer.from(text)]);
const float = n => {const b=Buffer.alloc(4);b.writeFloatBE(n);return b;};

function header(bytes) {
  let p=8;
  const byte=()=>bytes[p++];
  const v=()=>{let n=0,s=0,b;do{b=byte();n|=(b&127)<<s;s+=7;}while(b&128);return n>>>0;};
  const str=()=>{const n=v();if(n<2)return '';const t=bytes.toString('utf8',p,p+n-1);p+=n-1;return t;};
  const f=()=>{const n=bytes.readFloatBE(p);p+=4;return n;};
  assert.match(str(),/^4\.2\./); p+=20; const extra=byte();
  if(extra){p+=4;str();str();}
  for(let n=v();n>0;n--)str();
  const bones=[];
  for(let i=0,n=v();i<n;i++){
    const name=str(),parent=i?v():-1;
    const [rotation,x,y,scaleX,scaleY,shearX,shearY,length]=Array.from({length:8},f);
    v();byte();if(extra){p+=4;str();byte();}
    bones.push({name,index:i,parent,rotation,x,y,scaleX,scaleY,shearX,shearY,length});
  }
  for(let n=v();n>0;n--){str();v();p+=8;v();v();if(extra)byte();}
  const ik=[];
  for(let i=0,n=v();i<n;i++){
    const name=str();v();const chain=Array.from({length:v()},()=>bones[v()]);const target=bones[v()];
    const flags=byte();if((flags&32)&&(flags&64))f();if(flags&128)f();
    ik.push({name,index:i,chain,target});
  }
  return {bones,ik};
}

function encodeClip(bones, ik, definition) {
  const timelines=new Map();
  const constant=(name,kind,...values)=>{
    const a=timelines.get(name)??[];a.push({kind,frames:[{time:0,values}]});timelines.set(name,a);
  };
  constant('root',0,0);constant('root',1,0,0);
  // The root stays still. This translation is on the pelvis/head-parent bone,
  // never on SpineSprite.Scale or any bone scale/shear timeline.
  const frames=Array.from({length:49},(_,i)=>{
    const t=i/48,s=t*t*(3-2*t);
    return {time:.2*t,values:[0,-definition.depth*s]};
  });
  // Never sample the actual end while holding: CreatureAnimator listens to ALL
  // track completions and must not interpret this overlay as an attack finish.
  frames.push({time:.21,values:[0,-definition.depth]});
  timelines.set(definition.pelvis,[{kind:1,frames},{kind:0,frames:frames.map(f=>({time:f.time,values:[-4*(f.values[1]/-definition.depth)]}))}]);
  constant('body',0,0);constant('body',1,0,0);
  const radians=Math.PI/180;
  const normalize=angle=>((angle+180)%360+360)%360-180;
  // Bake the two-link IK solution into ordinary rotation keys. In particular,
  // do not leave an IK mix=1 behind on skin 4: its attack has no IK timeline.
  // Every existing action stays byte-identical, including its constraint data.
  for(const constraint of ik){
    const [thigh,shin]=constraint.chain;
    const foot=definition.feet[constraint.name.endsWith('L')?0:1];
    const thighKeys=[],shinKeys=[];
    for(const frame of frames){
      const posed=new Map();
      for(const bone of bones){
        const parent=bone.parent<0?{x:0,y:0,angle:0}:posed.get(bone.parent);
        let x=bone.x,y=bone.y,angle=bone.rotation;
        if(bone.name===definition.pelvis){x+=frame.values[0];y+=frame.values[1];angle+=-4*(frame.values[1]/-definition.depth);}
        const c=Math.cos(parent.angle*radians),s=Math.sin(parent.angle*radians);
        posed.set(bone.index,{x:parent.x+c*x-s*y,y:parent.y+s*x+c*y,angle:parent.angle+angle});
      }
      for(let p=thigh.parent;p>=0;p=bones[p].parent){
        const b=bones[p];assert.ok(Math.abs(b.scaleX-1)<1e-6&&Math.abs(b.scaleY-1)<1e-6&&Math.abs(b.shearX)<1e-6&&Math.abs(b.shearY)<1e-6,'unsupported scaled/sheared leg ancestry');
      }
      const origin=posed.get(thigh.index),parent=posed.get(thigh.parent);
      const dx=foot[0]-origin.x,dy=foot[1]-origin.y;
      const firstLength=Math.hypot(shin.x,shin.y),secondLength=shin.length;
      const cosine=(dx*dx+dy*dy-firstLength*firstLength-secondLength*secondLength)/(2*firstLength*secondLength);
      assert.ok(cosine<=1.001&&cosine>=-1.001,'unreachable authored foot position');
      const knee=-Math.acos(Math.max(-1,Math.min(1,cosine)));
      const offset=Math.atan2(shin.y,shin.x);
      const hip=Math.atan2(dy,dx)-Math.atan2(secondLength*Math.sin(knee),firstLength+secondLength*Math.cos(knee))-offset;
      thighKeys.push({time:frame.time,values:[normalize(hip/radians-parent.angle-thigh.rotation)]});
      shinKeys.push({time:frame.time,values:[normalize((offset+knee)/radians-shin.rotation)]});
    }
    timelines.set(thigh.name,[{kind:0,frames:thighKeys}]);constant(thigh.name,1,0,0);
    timelines.set(shin.name,[{kind:0,frames:shinKeys}]);constant(shin.name,1,0,0);
  }
  const chunks=[string(clipName),varint([...timelines.values()].reduce((sum,t)=>sum+t.length,0)),varint(0),varint(timelines.size)];
  for(const [name,list] of timelines){
    const bone=bones.find(b=>b.name===name);assert.ok(bone,`missing bone ${name}`);
    chunks.push(varint(bone.index),varint(list.length));
    for(const t of list){
      chunks.push(Buffer.from([t.kind]),varint(t.frames.length),varint(0)); // all linear; no Bezier storage
      for(let i=0;i<t.frames.length;i++){
        chunks.push(float(t.frames[i].time),...t.frames[i].values.map(float));
        if(i>0)chunks.push(Buffer.from([0]));
      }
    }
  }
  chunks.push(varint(0)); // no runtime IK changes to leak into the next action
  for(let i=0;i<6;i++)chunks.push(varint(0)); // transform/path/physics/attachment/draw order/event timelines
  return Buffer.concat(chunks);
}

const [inputDir,outputDir,selectedSkin]=process.argv.slice(2);
assert.ok(inputDir&&outputDir,'Specify input and output directories. Originals are not overwritten by default.');
assert.ok(!selectedSkin||baselines.some(d=>d.skin===selectedSkin),'Unknown skin.');
fs.mkdirSync(outputDir,{recursive:true});
const manifest=[];
for(const definition of baselines){
  if(selectedSkin&&definition.skin!==selectedSkin)continue;
  // Preserve the existing four-skin directory workflow. Mu's independently
  // extracted source is authored explicitly with the optional skin argument.
  if(!selectedSkin&&definition.skin==='tashigan_idol')continue;
  const file=definition.skin+'.skel',input=fs.readFileSync(path.join(inputDir,file));
  const {bones,ik:sourceIk}=header(input);
  const ik=definition.legs
    ? definition.legs.map((names,index)=>({name:index===0?'footL':'footR',chain:names.map(name=>bones.find(b=>b.name===name))}))
    : sourceIk;
  assert.equal(sourceIk.length,definition.legs?0:2);
  assert.ok(ik.length===2&&ik.every(c=>c.chain.length===2&&c.chain.every(Boolean)),'Missing leg chains.');
  const clip=encodeClip(bones,ik,definition);
  // This uniquely identifies the original count + first animation header.
  // Refuse unknown layouts instead of guessing a byte offset in a binary file.
  const animationCount=definition.animationCount??20;
  const originalMarker=Buffer.concat([varint(animationCount),string('attack')]);
  let original=input,countAt=input.indexOf(originalMarker);
  if(countAt<0){
    const marker=Buffer.concat([varint(animationCount+1),string('attack')]);countAt=input.indexOf(marker);
    assert.ok(countAt>=0&&input.subarray(-clip.length).equals(clip),'Not an original or this tool\'s exact output.');
    original=Buffer.from(input.subarray(0,-clip.length));original[countAt]=animationCount;
  }
  assert.equal(original.indexOf(originalMarker,countAt+1),-1,'ambiguous animation-count offset');
  assert.ok(countAt>1000);
  assert.equal(hash(original),definition.sha256,'Skeleton changed: remeasure foot positions before authoring.');
  const output=Buffer.concat([original,clip]);output[countAt]=animationCount+1;
  // The complete existing binary is preserved, apart from this one count byte.
  const restored=Buffer.from(output.subarray(0,original.length));restored[countAt]=animationCount;
  assert.ok(restored.equals(original));
  fs.writeFileSync(path.join(outputDir,file),output);
  manifest.push({skin:definition.skin,originalSha256:hash(original),outputSha256:hash(output),countByte:countAt,originalBytes:original.length,appendedBytes:clip.length,depth:definition.depth,descentDuration:.2,clipDuration:.21});
}
console.log(JSON.stringify(manifest,null,2));
