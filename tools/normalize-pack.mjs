// Normalize one Godot export from the unified project; never read an old project/PCK.
// Keep host-loaded text resources readable (custom GDScript depends on the host game's globals).
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readPack} from './pck-reader.mjs';
const root=path.resolve(import.meta.dirname,'..');
const file=process.argv[2]; assert(file,'PCK path required');
const original=fs.readFileSync(file), files=readPack(file);
function walk(dir) {
  return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=> e.name==='.godot'||e.name==='src'?[]:e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);
}
let preserved=0;
for(const source of walk(path.join(root,'mod'))) {
  const relative=path.relative(path.join(root,'mod'),source).replaceAll('\\','/');
  const body=fs.readFileSync(source);
  const textResource=/\.(gd|gdshader|tscn|tres)$/.test(source) && (source.endsWith('.gd')||source.endsWith('.gdshader')||body.toString('utf8').replace(/^\uFEFF/,'').trimStart().startsWith('['));
  const oldRawTexture=relative.startsWith('WeiDongZhuo/')&&source.endsWith('.png')||relative==='Yjcm/Images/events/qu_yuan_tou_jiang.png';
  if(!textResource&&!oldRawTexture) continue;
  files.set(relative,body); preserved++;
  // The former raw DongZhuo resources are resolved against the host. Keep existing
  // common-resource remaps (and their compiled assets) unchanged.
  if(textResource && /WeiDongZhuo|weidongzhuo|wei_dong_zhuo/.test(relative)) files.delete(relative+'.remap');
}
const base=Number(original.readBigUInt64LE(24));
const header=Buffer.from(original.subarray(0,base));
const chunks=[header], records=[]; let cursor=base;
for(const [name,body] of [...files].sort(([a],[b])=>a.localeCompare(b,'en'))) {
  const encoded=Buffer.from(name), padded=Math.ceil(encoded.length/4)*4;
  const record=Buffer.alloc(4+padded+36); record.writeUInt32LE(padded); encoded.copy(record,4);
  record.writeBigUInt64LE(BigInt(cursor-base),4+padded); record.writeBigUInt64LE(BigInt(body.length),12+padded);
  crypto.createHash('md5').update(body).digest().copy(record,20+padded); records.push(record);
  chunks.push(body); cursor+=body.length;
  const padding=(16-cursor%16)%16; if(padding){chunks.push(Buffer.alloc(padding));cursor+=padding;}
}
header.writeBigUInt64LE(BigInt(cursor),32);
const count=Buffer.alloc(4); count.writeUInt32LE(files.size); chunks.push(count,...records);
fs.writeFileSync(file,Buffer.concat(chunks));
console.log(`UNIFIED_PACK_NORMALIZED entries=${files.size} rawResources=${preserved}`);
