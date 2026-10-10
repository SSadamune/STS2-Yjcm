import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { selectVariant } from './variant-source.mjs';
const root=path.resolve(import.meta.dirname, '..');
function walk(dir) { return fs.readdirSync(dir, {withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]); }
const sources=walk(path.join(root,'mod/src')).filter(f=>f.endsWith('.cs'));
for(const file of sources) {
  const source=fs.readFileSync(file,'utf8');
  for(const v of ['stable','beta']) selectVariant(source,v);
  assert(!/E:\\sts2mod|C:\\Users\\CDphantom/.test(source), `Personal path: ${file}`);
}
for(const file of walk(path.join(root,'mod/Yjcm/localization')).filter(f=>f.endsWith('.json'))) JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
assert.equal(JSON.parse(fs.readFileSync(path.join(root,'mod/mod_manifest.json'),'utf8')).id,'Yjcm');
for(const item of ['正式版','wuzhugeliang','威董卓开发','feature_beta']) assert(!fs.existsSync(path.join(root,item)),`Duplicate legacy tree ${item}`);
console.log(`REPOSITORY_CHECK_OK unifiedSources=${sources.length} variants=stable,beta`);
