// Convert supplied Spine data only; never modify source art or invent attack clips.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {merge, rect, pose, slotGeometry} from './preview/geometry.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const workspace=process.argv[2]||path.dirname(root);
const modules=process.env.WDZ_NODE_MODULES||path.join(root,'node_modules');
const require=createRequire(path.join(modules,'_yjcm_skin.cjs'));
const spine=require('@esotericsoftware/spine-core');
const source=path.join(workspace,'素材/图片素材/威董卓皮肤');
const output=path.join(root,'assets/loose/Spine/WeiDongZhuoTaiShan');
const reports=path.join(root,'artifacts/reports/taishan-skin');
fs.mkdirSync(reports,{recursive:true});
const converter=path.join(workspace,'_spine_conversion_tools/SpineSkeletonDataConverter.exe');
function convert(input,output){
  const result=spawnSync(converter,[input,output,'-v','4.2.43'],{encoding:'utf8',windowsHide:true});
  assert.equal(result.status,0,result.stderr||result.stdout);
}
const json={};
for(const name of ['beijing','daiji']) {
  const target=path.join(reports,`${name}.json`);
  convert(path.join(source,`${name}.skel`),target);
  json[name]=JSON.parse(fs.readFileSync(target,'utf8'));
}
const combat=structuredClone(json.daiji);
const boneMap=new Map(combat.bones.map(b=>[b.name,b]));
function bodyBone(name){
  for(let bone=boneMap.get(name);bone;bone=boneMap.get(bone.parent)) if(bone.name==='ren') return true;
  return false;
}
const hidden=new Set(combat.slots.filter(s=>!bodyBone(s.bone)).map(s=>s.name));
for(const slot of combat.slots) if(hidden.has(slot.name)) delete slot.attachment;
for(const skin of combat.skins) for(const slot of hidden) delete skin.attachments[slot];
for(const animation of Object.values(combat.animations)) for(const slot of hidden) if(animation.slots) delete animation.slots[slot];
const combatJson=path.join(reports,'combat.json');
fs.writeFileSync(combatJson,JSON.stringify(combat));
const report={name:'泰山崩石',version:'4.2.43',hiddenCombatSlots:[...hidden],assets:[]};
for(const [folder,name,dataFile] of [
  ['Background','beijing',path.join(reports,'beijing.json')],
  ['Character','daiji',path.join(reports,'daiji.json')],
  ['Combat','daiji',combatJson]
]) {
  const target=path.join(output,folder);fs.mkdirSync(target,{recursive:true});
  const base=folder==='Background'?'background':'character';
  const atlasText=fs.readFileSync(path.join(source,`${name}.atlas`),'utf8');
  fs.writeFileSync(path.join(target,`${base}.atlas`),atlasText);
  const atlas=new spine.TextureAtlas(atlasText);
  for(const page of atlas.pages){
    assert(!page.name.includes('..')&&!path.isAbsolute(page.name),'Unexpected atlas page');
    fs.copyFileSync(path.join(source,page.name),path.join(target,page.name));
    page.setTexture(new spine.FakeTexture({width:page.width,height:page.height}));
  }
  convert(dataFile,path.join(target,`${base}.skel`));
  const data=new spine.SkeletonBinary(new spine.AtlasAttachmentLoader(atlas))
    .readSkeletonData(fs.readFileSync(path.join(target,`${base}.skel`)));
  assert.equal(data.version,'4.2.43');assert.deepEqual(data.animations.map(a=>a.name),['play']);
  const skeleton=new spine.Skeleton(data),animation=data.animations[0];
  let bounds=null,focus=null;
  for(let i=0;i<=60;i++){
    pose(spine,skeleton,animation,i*animation.duration/60);
    for(const slot of skeleton.slots){const g=slotGeometry(spine,slot);if(!g||g.alpha<=0.001)continue;
      bounds=merge(bounds,g.bounds);
      if(folder==='Background'?slot.data.name==='beijing':bodyBone(slot.bone.data.name))focus=merge(focus,g.bounds);
    }
  }
  assert(focus,'Missing body/backdrop geometry');
  report.assets.push({folder,base,pages:atlas.pages.map(p=>p.name),duration:animation.duration,
    bounds:rect(bounds),focus:rect(focus),slotCount:data.slots.length});
}
fs.writeFileSync(path.join(output,'skin.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
