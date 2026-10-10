// Read-only audit of tracked files and non-ignored candidates. Never prints secrets.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const files = [...new Set(execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z'], {cwd: root, encoding:'utf8'}).split('\0').filter(Boolean))].sort();
const extensions = new Set(['.cs','.csproj','.sln','.props','.godot','.gd','.gdshader','.tscn','.tres','.uid','.mjs','.js','.ps1','.py','.cmd','.html','.css','.json','.jsonc','.yml','.yaml','.md','.txt','.cfg']);
const exact = new Set(['LICENSE','.gitignore','.gitattributes','.editorconfig']);
const privatePath = /(?:^|\/)(?:\.git|\.godot|\.vs|node_modules|bin|obj|artifacts|assets)(?:\/|$)|(?:^|\/)(?:build\.local\.json|config\.local\.json|snapshot\.json|\.env(?:\..*)?|\.dev\.vars)$|^mod\/(?:images\/|Yjcm\/(?:Images|Audio|audios|Voice)\/)|^tests\/sunqian-public-api-.*\.cs\.txt$|^docs\/design\/初始遗物\/原版增益清单\./;
const secretPatterns = [
  /\bgh[pousr]_[A-Za-z0-9]{30,}\b/,
  /\bgithub_pat_[A-Za-z0-9_]{30,}\b/,
  /\bAKIA[0-9A-Z]{16}\b/,
  /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/,
  /\bsk-(?:proj-)?[A-Za-z0-9_-]{30,}\b/
];
const digest = crypto.createHash('sha256');
let bytes = 0;
for (const relative of files) {
  assert(!privatePath.test(relative), `Private/excluded path in public selection: ${relative}`);
  assert(exact.has(path.basename(relative)) || extensions.has(path.extname(relative)), `Not an allowed source file: ${relative}`);
  const data = fs.readFileSync(path.join(root, relative));
  assert(!data.includes(0), `Binary data in public selection: ${relative}`);
  const text = new TextDecoder('utf-8', {fatal:true}).decode(data);
  for (const pattern of secretPatterns) assert(!pattern.test(text), `Potential credential detected; inspect locally: ${relative}`);
  bytes += data.length;
  digest.update(relative).update('\0').update(data).update('\0');
}
assert(files.includes('LICENSE'), 'Missing license');
assert(files.includes('docs/资源准备.md'), 'Missing local resource preparation instructions');
console.log(`PUBLIC_SOURCE_AUDIT_OK files=${files.length} bytes=${bytes} fingerprint=${digest.digest('hex')}`);
