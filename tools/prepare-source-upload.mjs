// Export the audited Git index for connector-based initial uploads (no credentials).
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const git = args => execFileSync('git', args, {cwd:root, encoding:'utf8'});
execFileSync(process.execPath, [path.join(root,'tools/check-public-source.mjs')], {cwd:root, stdio:'inherit'});
const files = git(['ls-files','--cached','-z']).split('\0').filter(Boolean).sort();
const blobs = new Map(git(['ls-files','--stage','-z']).split('\0').filter(Boolean).map(row => {
  const match = row.match(/^(\d+) ([0-9a-f]+) 0\t(.+)$/s);
  assert(match, 'Unmerged index');
  assert.equal(match[1], '100644', 'Only ordinary source files may be uploaded');
  return [match[3], match[2]];
}));
assert(files.includes('LICENSE') && files.includes('README.md'), 'Stage the source-only selection before export');
const output = path.join(root,'artifacts/public-upload');
const snapshot = path.join(output,'snapshot');
fs.mkdirSync(snapshot,{recursive:true});
assert(fs.readdirSync(snapshot).length === 0, 'Snapshot directory must be empty; choose a fresh export location');
git(['checkout-index','--all','--prefix='+snapshot.replaceAll('\\','/')+'/']);
const treeElements = files.map(relative => {
  const data = fs.readFileSync(path.join(snapshot,relative));
  const sha = crypto.createHash('sha1').update(`blob ${data.length}\0`).update(data).digest('hex');
  assert.equal(sha, blobs.get(relative), `Export differs from Git index: ${relative}`);
  return {path:relative, mode:'100644', type:'blob', content:new TextDecoder('utf-8',{fatal:true,ignoreBOM:true}).decode(data)};
});
const payload = {treeSha:git(['write-tree']).trim(), files:files.length, treeElements};
const target = path.join(output,'payload.json');
fs.writeFileSync(target,JSON.stringify(payload));
console.log(`SOURCE_UPLOAD_PREPARED files=${files.length} tree=${payload.treeSha} snapshot=${snapshot}`);
