import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.dirname(fileURLToPath(import.meta.url));
const file=process.argv[2];
const fd=fs.openSync(file,'r');
function read(offset,size) {
  const b=Buffer.alloc(size);
  assert.equal(fs.readSync(fd,b,0,size,offset),size);
  return b;
}
try {
  const header=read(0,40);
  assert.equal(header.readUInt32LE(0),0x43504447);
  assert.equal(header.readUInt32LE(4),3);
  const base=Number(header.readBigUInt64LE(24));
  const indexOffset=Number(header.readBigUInt64LE(32));
  const index=read(indexOffset,fs.fstatSync(fd).size-indexOffset);
  let p=4;
  const names=[];
  for(let i=0;i<index.readUInt32LE(0);i++) {
    const len=index.readUInt32LE(p);p+=4;
    const name=index.subarray(p,p+len).toString('utf8').replaceAll('\0','');p+=len;
    const start=Number(index.readBigUInt64LE(p))+base;
    const size=Number(index.readBigUInt64LE(p+8));
    const hash=index.subarray(p+16,p+32);p+=36;
    if(/localization\/(?:zhs|eng)\/(?:powers|relics|cards|potions)\.json$/.test(name)) {
      const data=read(start,size);
      assert(crypto.createHash('md5').update(data).digest().equals(hash),name);
      const target=path.join(root,'原版分析',name);
      fs.mkdirSync(path.dirname(target),{recursive:true});
      fs.writeFileSync(target,data);
      names.push(name);
    }
  }
  console.log(JSON.stringify({count:index.readUInt32LE(0),extracted:names},null,2));
} finally {fs.closeSync(fd);}
