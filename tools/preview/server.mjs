import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(here,'../..');
const modules=process.env.WDZ_NODE_MODULES||path.resolve(here,'../../node_modules');
const runtime=path.join(modules,'@esotericsoftware/spine-webgl/dist/iife/spine-webgl.js');
export function startServer(port=8793) {
  const server=http.createServer((request,response)=>{
    try {
      const url=decodeURIComponent(new URL(request.url,'http://localhost').pathname);
      const relative=url==='/'?'tools/preview/README.md':url.slice(1);
      const file=url==='/vendor/spine-webgl.js'?runtime:path.resolve(root,relative);
      if(file!==runtime&&!file.startsWith(root+path.sep)) { response.writeHead(403).end(); return; }
      const type={'.js':'text/javascript','.mjs':'text/javascript','.html':'text/html; charset=utf-8',
        '.json':'application/json','.skel':'application/octet-stream','.atlas':'text/plain','.png':'image/png','.md':'text/plain; charset=utf-8'}[path.extname(file)];
      if(!type||!fs.existsSync(file)||!fs.statSync(file).isFile()){response.writeHead(404).end();return;}
      response.writeHead(200,{'Content-Type':type,'Cache-Control':'no-store'});
      fs.createReadStream(file).pipe(response);
    } catch(error) {response.writeHead(400).end(String(error));}
  });
  return new Promise(resolve=>server.listen(port,'127.0.0.1',()=>resolve(server)));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  const server=await startServer(Number(process.argv[2]||8793));
  console.log(`WeiDongZhuo Spine preview: http://127.0.0.1:${server.address().port}/`);
}
