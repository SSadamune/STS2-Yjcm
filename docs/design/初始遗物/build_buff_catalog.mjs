import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.dirname(fileURLToPath(import.meta.url));
const analysis=path.join(root,'原版分析');
const codeRoot=path.join(analysis,'beta-0.111.0');
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const inGameBuff=r=>r.Type==='Buff' && /^MegaCrit\.Sts2\.Core\.Models\.Powers\.\w+$/.test(r.fullName);
const beta=json(path.join(analysis,'powers-beta-0.111.0.json')).filter(inGameBuff);
const stable=json(path.join(analysis,'powers-stable-0.107.1.json')).filter(inGameBuff);
const locales=Object.fromEntries(['powers','cards','relics'].map(k=>[k,json(path.join(analysis,'localization/zhs',k+'.json'))]));
const caps=s=>s.replace(/([A-Z])([A-Z][a-z])/g,'$1_$2').replace(/([a-z0-9])([A-Z])/g,'$1_$2').toUpperCase();
const read=p=>fs.readFileSync(p,'utf8');
const clean=s=>(s??'').replace(/\[\/?[^\]]+\]/g,'').replaceAll('\n',' ').replaceAll('|','｜');
const sources=new Map();
for(const folder of ['MegaCrit.Sts2.Core.Models.Monsters','MegaCrit.Sts2.Core.Models.Cards','MegaCrit.Sts2.Core.Models.Relics']) {
  const dir=path.join(codeRoot,folder);
  for(const name of fs.readdirSync(dir).filter(n=>n.endsWith('.cs'))) sources.set(folder+'/'+name,read(path.join(dir,name)));
}
const generalNames=new Set(('Strength Dexterity Focus Vigor Artifact BlockNextTurn Buffer Intangible Barricade Metallicize Plating Thorns Ritual Regeneration Regen RetainBlock DodgeAndRoll Evasive Enrage CurlUp Hardened Shell Flight Shroud Invincible Slow Malleable SharpHide BeatOfDeath ModeShift Thievery Minion Explosive').split(' ').map(n=>n+'Power'));
for(const name of ['MinionPower','ThieveryPower','ShroudPower']) generalNames.delete(name);
for(const name of ['BlurPower','DoubleDamagePower','DrawCardsNextTurnPower','EnergyNextTurnPower','FlameBarrierPower']) generalNames.add(name);
const rows=beta.map(r=>{
  const file=path.join(codeRoot,'MegaCrit.Sts2.Core.Models.Powers',r.class+'.cs');
  assert(fs.existsSync(file),'Missing code: '+r.class);
  const source=read(file), id=caps(r.class);
  const origin=/OriginModel\s*=>\s*ModelDb\.(Card|Potion|Relic)<([^>]+)>/.exec(source);
  let title=locales.powers[id+'.title'];
  let description=locales.powers[id+'.description'];
  if(!title&&origin) {
    const table=origin[1]==='Relic'?'relics':origin[1]==='Card'?'cards':'potions';
    if(table==='potions'&&!locales.potions) locales.potions=json(path.join(analysis,'localization/zhs/potions.json'));
    title=locales[table][caps(origin[2])+'.title'];
    description=locales[table][caps(origin[2])+'.description'];
  }
  if(r.base.startsWith('Temporary')) {
    const attribute=r.base.includes('Strength')?'力量':r.base.includes('Dexterity')?'敏捷':'集中';
    description='临时'+attribute+'的配套状态，参与施加与到期回退；不是一份独立的永久'+attribute+'。';
  }
  const usages=[...sources].filter(([name,code])=>new RegExp('\\b'+r.class+'\\b').test(code)).map(([name])=>name);
  const monsterRelated=usages.some(n=>n.includes('.Monsters/'));
  const warnings=[];
  if(r.base.startsWith('Temporary')) warnings.push('临时属性的配套状态，不能与力量/敏捷/集中重复偷取；需专门处理到期回退');
  if(/Owner\.Monster|\.Monster is|Owner\.PetOwner|\bOsty\b|\bCompanion\b/.test(source)) warnings.push('包含怪物/召唤物相关分支，转给玩家前需核对');
  if(/Owner\.Player/.test(source)) warnings.push('依赖玩家状态');
  if(r.InstanceType==='Instanced'||r.InstanceType==='Multiple') warnings.push('绑定来源/目标实例，不能只复制层数');
  if(r.StackType==='Single') warnings.push('单一开关效果，不是普通数值层数');
  if(/IsVisibleInternal\s*=>\s*false/.test(source)) warnings.push('原版隐藏状态');
  if(r.AllowNegative==='True') warnings.push('原版允许负值；本遗物仍只允许偷正数');
  if(/GetInternalData</.test(source)) warnings.push('带内部状态，需处理转移/移除时的状态一致性');
  if(description==='TODO') warnings.push('原版说明为 TODO，不作为默认可偷效果');
  const section=r.base.startsWith('Temporary')?'临时属性配套状态':generalNames.has(r.class)?'常规属性与常见战斗状态':monsterRelated?'其他怪物相关增益':'其他玩家或系统增益';
  return {...r,id,title:title??'[需要核对名称] '+r.class,description:clean(description??'[此状态没有独立中文说明]'),section,monsterRelated,warnings,source:path.relative(root,file).replaceAll('\\','/'),usages,inStable:stable.some(s=>s.class===r.class)};
});
fs.writeFileSync(path.join(root,'原版增益清单.json'),JSON.stringify({version:'0.111.0',classification:'PowerType.Buff',count:rows.length,rows,stableOnly:stable.filter(s=>!rows.some(r=>r.class===s.class))},null,2));
const sections=['常规属性与常见战斗状态','其他怪物相关增益','其他玩家或系统增益','临时属性配套状态'];
let md='# 原版增益选择清单（测试版 0.111.0）\n\n';
md+='本清单从本机原版 sts2.dll 的 PowerType.Buff 分类和原版中文文案提取，不含任何 mod。共 '+rows.length+' 个具体状态类，已排除 12 个原版内部 Mock 测试类。隐藏/未完成机制仍列出并提示；有“增益”分类不代表它对玩家有利，也不代表可以直接安全转移。\n\n';
md+='用途：请你选择允许威董卓初始遗物偷取的名称或编号。在你确认前，没有设置默认白名单，也没有实现偷取、抽牌或最大生命变化。\n\n';
md+='格挡数值、能量、金币、辉星、充能球、遗物本体和最大生命不是这里的 PowerType.Buff，不会自动纳入；弱化、易伤等负面效果也不在本表。力量、敏捷、集中虽支持负数，只有敌人实际正数层数才可能入选。\n\n';
md+='说明文字取原版普通说明；其中固定数字是展示基准，不一定等于实际层数。标为“可叠加计数”的数值可能代表回合、触发次数或伤害上限，不一定是越高越强。临时属性配套状态不可与其主属性重复扣取。分类依据是常见用途与源码引用，只用于导航，不是兼容性承诺。\n\n';
md+='## 如何回复\n\n你可以回复“允许：力量、人工制品、覆甲……”或直接给编号。怪物专用机制如果也想允许，请单独注明；我会先说明需要怎样适配，不擅自扩大白名单。\n\n';
let number=0;
for(const section of sections) {
  const group=rows.filter(r=>r.section===section);
  md+='## '+section+'（'+group.length+' 项）\n\n';
  md+='| 编号 | 名称 | 原版效果 / 含义 | 叠加形式 | 转移注意事项 | 程序类型 |\n| --- | --- | --- | --- | --- | --- |\n';
  for(const row of group) {
    row.number=++number;
    md+='| '+[number,clean(row.title),row.description,row.StackType==='Single'?'单一效果':'计数（'+row.StackType+'）',row.warnings.join('；')||'尚未验证跨持有者转移','['+row.class+']('+row.source+')'].join(' | ')+' |\n';
  }
  md+='\n';
}
md+='## 正式版兼容提醒\n\n';
md+='下列效果在本次保存的测试版有、正式版 0.107.1 没有：'+rows.filter(r=>!r.inStable).map(r=>r.title+'（'+r.class+'）').join('、')+'。\n\n';
md+='正式版中另外存在、当前测试版已不在增益类中的类型：'+stable.filter(s=>!rows.some(r=>r.class===s.class)).map(s=>s.class).join('、')+'。这两项不列入当前测试版可选表。\n\n';
md+='## 核对来源\n\n- 编译依赖/beta-0.111.0/sts2.dll（逐类型读取分类）\n- 编译依赖/stable-0.107.1/sts2.dll（分支差异核对）\n- 当前测试版 SlayTheSpire2.pck 中 localization/zhs/powers.json 以及对应卡牌/遗物/药水标题\n- 原版分析/beta-0.111.0 中对应 Power 类实现\n\n源码风险标记为静态检查提示，最终能否偷取须在你选定后逐项验证。\n';
fs.writeFileSync(path.join(root,'原版增益清单.md'),md);
fs.writeFileSync(path.join(root,'原版增益清单.json'),JSON.stringify({version:'0.111.0',classification:'PowerType.Buff',count:rows.length,rows,stableOnly:stable.filter(s=>!rows.some(r=>r.class===s.class))},null,2));
console.log(JSON.stringify({count:rows.length,sections:Object.fromEntries([...new Set(rows.map(r=>r.section))].map(s=>[s,rows.filter(r=>r.section===s).length])),missingTitles:rows.filter(r=>r.title.startsWith('[需要')).map(r=>r.class),betaOnly:rows.filter(r=>!r.inStable).map(r=>r.class),stableOnly:stable.filter(s=>!rows.some(r=>r.class===s.class)).map(r=>r.class),common:rows.filter(r=>r.section==='常规属性与常见战斗状态').map(r=>({class:r.class,title:r.title,description:r.description})),monsterRelated:rows.filter(r=>r.section==='其他怪物相关增益').map(r=>({class:r.class,title:r.title,description:r.description}))},null,2));
