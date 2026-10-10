import {readPack} from './pck-reader.mjs';
const previous=readPack(process.argv[2]), current=readPack(process.argv[3]);
const missing=[], changed=[];
for(const [name,bytes] of previous) {
  if(!current.has(name)) missing.push(name);
  else if(!bytes.equals(current.get(name))) changed.push(name);
}
console.log(JSON.stringify({previous:previous.size,current:current.size,missing,changed},null,2));
