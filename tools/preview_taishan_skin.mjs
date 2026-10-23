// Development-only real Spine WebGL render; not a live-game screenshot.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {startServer} from './preview/server.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const require=createRequire(path.join(process.env.WDZ_NODE_MODULES||path.join(root,'node_modules'),'_yjcm_preview.cjs'));
const {chromium}=require('playwright-core');
const server=await startServer(0),origin=`http://127.0.0.1:${server.address().port}`;
const browser=await chromium.launch({channel:'chrome',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
const reports=path.join(root,'artifacts/reports/taishan-skin');
const layout=fs.readFileSync(path.join(root,'mod/WeiDongZhuo/scripts/taishan_select_bg.gd'),'utf8');
const characterBottom=Number(layout.match(/const CHARACTER_BOTTOM = ([\d.]+)/)?.[1]);
const characterCenterX=Number(layout.match(/const CHARACTER_CENTER_X = ([\d.]+)/)?.[1]);
assert(Number.isFinite(characterBottom),'Missing shared select portrait bottom anchor');
assert(Number.isFinite(characterCenterX),'Missing shared select portrait horizontal anchor');
fs.mkdirSync(reports,{recursive:true});
try {
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',error=>errors.push(String(error)));
  await page.goto(origin+'/tools/preview/README.md');
  await page.setContent(`<style>body{margin:0;background:#111}canvas{display:block}</style><canvas width="1280" height="720"></canvas><script src="${origin}/vendor/spine-webgl.js"></script>`);
  const results=await page.evaluate(async ({origin,characterBottom,characterCenterX})=>{
    const sp=window.spine,canvas=document.querySelector('canvas');
    const context=new sp.ManagedWebGLRenderingContext(canvas,{alpha:true,preserveDrawingBuffer:true,antialias:true});
    const renderer=new sp.SceneRenderer(canvas,context),manager=new sp.AssetManager(context),gl=context.gl;
    const manifest=await(await fetch(origin+'/assets/loose/Spine/WeiDongZhuoTaiShan/skin.json')).json();
    const models={};
    for(const asset of manifest.assets){
      const base=origin+`/assets/loose/Spine/WeiDongZhuoTaiShan/${asset.folder}/${asset.base}`;
      manager.loadBinary(base+'.skel');manager.loadTextureAtlas(base+'.atlas');await manager.loadAll();
      const data=new sp.SkeletonBinary(new sp.AtlasAttachmentLoader(manager.require(base+'.atlas'))).readSkeletonData(manager.require(base+'.skel'));
      models[asset.folder]={data,skeleton:new sp.Skeleton(data),asset};
    }
    function draw(folder,time,scale,x,y){
      const {data,skeleton}=models[folder];skeleton.setToSetupPose();
      data.findAnimation('play').apply(skeleton,-1,time,true,[],1,sp.MixBlend.replace,sp.MixDirection.mixIn);
      skeleton.scaleX=scale;skeleton.scaleY=scale;skeleton.x=x;skeleton.y=720-y;
      skeleton.updateWorldTransform(sp.Physics.none);renderer.drawSkeleton(skeleton,false);
    }
    renderer.camera.position.set(640,360,0);renderer.camera.viewportWidth=1280;renderer.camera.viewportHeight=720;renderer.camera.update();
    const captures=[];
    for(const time of [0,1.5,3.3,6.5]){
      gl.viewport(0,0,1280,720);gl.clearColor(.04,.045,.055,1);gl.clear(gl.COLOR_BUFFER_BIT);
      renderer.begin();
      const b=models.Background.asset.focus,c=models.Character.asset.focus;
      const bgScale=Math.max(1280/b.width,720/b.height)*1.025;
      draw('Background',time,bgScale,640-b.centerX*bgScale,360+b.centerY*bgScale);
      const charScale=Math.min(720*.86/c.height,1280*.66/c.width);
      draw('Character',time,charScale,1280*characterCenterX-c.centerX*charScale,720*characterBottom+c.minY*charScale);
      renderer.end();
      captures.push({name:`select-${time}.png`,url:canvas.toDataURL('image/png'),glError:gl.getError()});
    }
    gl.clearColor(.08,.12,.10,1);gl.clear(gl.COLOR_BUFFER_BIT);renderer.begin();
    draw('Combat',2,.42,440+95,570-106);renderer.end();
    captures.push({name:'combat.png',url:canvas.toDataURL('image/png'),glError:gl.getError()});
    return captures;
  },{origin,characterBottom,characterCenterX});
  for(const result of results)fs.writeFileSync(path.join(reports,result.name),Buffer.from(result.url.split(',')[1],'base64'));
  assert.equal(errors.length,0,JSON.stringify(errors));assert(results.every(r=>r.glError===0),'WebGL errors');
  console.log(`TAISHAN_SPINE_RENDER_OK frames=${results.length} glErrors=0`);
} finally {await browser.close();await new Promise(resolve=>server.close(resolve));}
