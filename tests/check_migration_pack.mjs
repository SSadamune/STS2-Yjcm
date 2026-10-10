// One-time comparison; archival PCK is ignored in Git and not required by normal builds.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {readPack} from '../tools/pck-reader.mjs';
const root=path.resolve(import.meta.dirname,'..');
const reference=process.argv[2]||path.join(root,'artifacts/migration/reference.pck');
assert(fs.existsSync(reference),'Provide the pre-migration full PCK as the first argument (one-time check only).');
const old=readPack(reference);
const current=readPack(path.join(root,'artifacts/dist/yjcm/Yjcm.pck'));
const loose=new Set();
for(const [name,data] of old) if(name.startsWith('Spine/')||name.startsWith('Voice/')) {
  loose.add(name);
  if(name.endsWith('.import')) for(const m of data.toString('utf8').matchAll(/res:\/\/([^"\r\n]+\.ctex)/g)) loose.add(m[1]);
}
let checked=0;
for(const [name,data] of old) {
  if(loose.has(name)||/\.(cs|csproj|sln|pdb)$/.test(name)||['.godot/uid_cache.bin','project.binary','WuZhugeliang.json'].includes(name)) continue;
  assert(current.has(name),`Missing runtime resource: ${name}`);
  const next=current.get(name);
  if(name.startsWith('Yjcm/localization/')&&name.endsWith('.json')) assert.deepEqual(JSON.parse(data.toString().replace(/^\uFEFF/,'')),JSON.parse(next.toString().replace(/^\uFEFF/,'')),`Localization changed: ${name}`);
  else if(name.endsWith('.import')) {
    const getTarget=b=>b.toString().match(/path="res:\/\/([^"\r\n]+)"/)?.[1];
    assert.equal(getTarget(next),getTarget(data),`Import target changed: ${name}`);
    assert(current.has(getTarget(next)),`Imported asset missing: ${name}`);
  } else assert(data.equals(next),`Runtime asset bytes changed: ${name}`);
  checked++;
}
console.log(`MIGRATION_PACK_EQUIVALENCE_OK checked=${checked} looseDuplicatesExcluded=${loose.size}`);
