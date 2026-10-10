// Shared deterministic Spine geometry inspection; no raster replacement assets.
export function merge(a, b) {
  if (!b) return a;
  if (!a) return { ...b };
  return { minX: Math.min(a.minX, b.minX), minY: Math.min(a.minY, b.minY),
    maxX: Math.max(a.maxX, b.maxX), maxY: Math.max(a.maxY, b.maxY) };
}
export function rect(b) {
  if (!b) return null;
  const round = n => Math.round(n * 1000) / 1000;
  return { ...Object.fromEntries(Object.entries(b).map(([k,v]) => [k,round(v)])),
    width: round(b.maxX-b.minX), height: round(b.maxY-b.minY),
    centerX: round((b.minX+b.maxX)/2), centerY: round((b.minY+b.maxY)/2) };
}
export function pose(spine, skeleton, animation, time) {
  skeleton.setToSetupPose();
  if (animation) animation.apply(skeleton, -1, time, false, [], 1, spine.MixBlend.replace, spine.MixDirection.mixIn);
  skeleton.updateWorldTransform(spine.Physics.none);
}
export function slotGeometry(spine, slot) {
  const a = slot.getAttachment();
  if (!a || !slot.bone.active) return null;
  let v;
  if (a instanceof spine.RegionAttachment) {
    v = new Float32Array(8);
    a.computeWorldVertices(slot, v, 0, 2);
  } else if (a instanceof spine.MeshAttachment) {
    v = new Float32Array(a.worldVerticesLength);
    a.computeWorldVertices(slot, 0, v.length, v, 0, 2);
  } else return null;
  let b = null;
  for (let i=0;i<v.length;i+=2) {
    if (!Number.isFinite(v[i]) || !Number.isFinite(v[i+1])) throw new Error(`Nonfinite vertices: ${slot.data.name}`);
    b = merge(b, {minX:v[i], maxX:v[i], minY:v[i+1], maxY:v[i+1]});
  }
  return { bounds:b, alpha:slot.color.a*a.color.a, attachment:a.name, region:a.path, vertices:v };
}
export function keyTimes(object, out = new Set()) {
  if (!object || typeof object !== 'object') return out;
  if (typeof object.time === 'number') out.add(object.time);
  for (const value of Object.values(object)) if (value && typeof value === 'object') keyTimes(value,out);
  return out;
}
export function inspect(spine, data, json, atlas, fps=60) {
  const s = new spine.Skeleton(data);
  const slots = data.slots.map(slot => ({index:slot.index,name:slot.name,bone:slot.boneData.name,
    setupAttachment:slot.attachmentName,blendMode:slot.blendMode,setupBounds:null,animations:{}}));
  pose(spine,s,null,0);
  let setup = null;
  for (const slot of s.slots) {
    const g = slotGeometry(spine,slot);
    if (g) { slots[slot.data.index].setupBounds=rect(g.bounds); setup=merge(setup,g.bounds); }
  }
  const animations=[];
  for (const animation of data.animations) {
    const times=keyTimes(json.animations[animation.name]);
    times.add(0); times.add(animation.duration);
    for(let i=0;i<=Math.ceil(animation.duration*fps);i++) times.add(Math.min(i/fps,animation.duration));
    let raw=null,visible=null;
    const perSlot = slots.map(() => ({raw:null,visible:null,activeSamples:0}));
    for (const time of [...times].sort((a,b)=>a-b)) {
      pose(spine,s,animation,time);
      for (const slot of s.slots) {
        const g=slotGeometry(spine,slot); if(!g) continue;
        const p=perSlot[slot.data.index]; p.raw=merge(p.raw,g.bounds); raw=merge(raw,g.bounds);
        if(g.alpha>0.001) { p.visible=merge(p.visible,g.bounds); visible=merge(visible,g.bounds); p.activeSamples++; }
      }
    }
    perSlot.forEach((p,i)=>slots[i].animations[animation.name]={rawBounds:rect(p.raw),visibleBounds:rect(p.visible),activeSamples:p.activeSamples});
    animations.push({name:animation.name,duration:animation.duration,samples:times.size,rawBounds:rect(raw),visibleBounds:rect(visible)});
  }
  return {spine:json.skeleton.spine,bones:data.bones.length,slotCount:slots.length,
    ik:json.ik?.length||0,transform:json.transform?.length||0,skins:data.skins.map(s=>s.name),
    exportedHeader:json.skeleton,setupBounds:rect(setup),animations,slots,
    boneHierarchy:json.bones.map((b,index)=>({index,name:b.name,parent:b.parent||null})),
    regions:atlas.regions.map(r=>({name:r.name,page:r.page.name,x:r.x,y:r.y,width:r.width,height:r.height,
      originalWidth:r.originalWidth,originalHeight:r.originalHeight,offsetX:r.offsetX,offsetY:r.offsetY,rotate:r.degrees,
      packedWidth:r.degrees%180?r.height:r.width,packedHeight:r.degrees%180?r.width:r.height})),
    texturePages:atlas.pages.map(p=>({name:p.name,width:p.width,height:p.height,pma:p.pma})),
    attachmentMap:json.skins.flatMap(skin=>Object.entries(skin.attachments).flatMap(([slot,aa])=>Object.entries(aa).map(([name,a])=>
      ({skin:skin.name,slot,name,type:a.type||'region',region:a.path||a.name||name,width:a.width,height:a.height}))))};
}
