import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
export function readPack(file) {
  const bytes=fs.readFileSync(file);
  assert.equal(bytes.readUInt32LE(0),0x43504447);
  assert.equal(bytes.readUInt32LE(4),3);
  const base=Number(bytes.readBigUInt64LE(24));
  let cursor=Number(bytes.readBigUInt64LE(32));
  const count=bytes.readUInt32LE(cursor); cursor+=4;
  const entries=new Map();
  for(let i=0;i<count;i++) {
    const length=bytes.readUInt32LE(cursor); cursor+=4;
    const name=bytes.subarray(cursor,cursor+length).toString('utf8').replaceAll('\0',''); cursor+=length;
    const start=Number(bytes.readBigUInt64LE(cursor))+base;
    const size=Number(bytes.readBigUInt64LE(cursor+8));
    const body=bytes.subarray(start,start+size);
    assert(crypto.createHash('md5').update(body).digest().equals(bytes.subarray(cursor+16,cursor+32)),`Corrupt asset: ${name}`);
    assert.equal(bytes.readUInt32LE(cursor+32),0,'Encrypted or deleted pack entry');
    cursor+=36;
    assert(!entries.has(name),`Duplicate resource: ${name}`); entries.set(name,body);
  }
  return entries;
}
