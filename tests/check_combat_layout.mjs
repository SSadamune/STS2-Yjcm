// Native five-slot arc, including orb_manager.tscn's +195 Y container offset.
// Bounding-box gaps are diagnostic only: side slots wrap around the silhouette.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const scene = fs.readFileSync(new URL('../mod/WeiDongZhuo/scenes/creature.tscn', import.meta.url), 'utf8');
const body = fs.readFileSync(new URL('../mod/WeiDongZhuo/scripts/combat_body.gd', import.meta.url), 'utf8');
const layout = JSON.parse(fs.readFileSync(new URL('fixtures/combat-layout.json', import.meta.url)));
const section = scene.split('[node name="OrbPos"')[1].split('[node ')[0];
const [, x, y] = section.match(/position = Vector2\(([-\d.]+), ([-\d.]+)\)/);
const scale = Number(body.match(/character.scale = Vector2.ONE \* ([\d.]+)/)[1]);
const [, bx, by] = body.match(/character.position = Vector2\(([-\d.]+), ([-\d.]+)\)/);
assert.equal(scale, 0.42, 'Keep the existing character size');
assert.equal(Number(bx), 95, 'Keep the adjusted character placement');
assert.equal(Number(by), -174);
const bodyTop = Number(by) - layout.bounds.combat.maxY * scale;
const checks = [];
// Multiplayer room zoom scales both the creature and its orb manager together.
// Test normal size and modest enlargement; temporary shrink effects are not covered.
for (const visualScale of [1, 1.25]) {
  for (const local of [true, false]) {
    const orbScale = visualScale > 1 ? 1 : (visualScale + 1) / 2;
    const radius = (225 + (300 - 225) * (5 - 3) / 7) * (local ? 1 : 0.75);
    let lowest = -Infinity;
    for (let i = 0; i < 5; i++) {
      const angle = (-150 + i * 125 / 4) * Math.PI / 180;
      const centerY = Number(y) * Math.min(visualScale, 1.25) + (local ? 0 : -50)
        + (195 + Math.sin(angle) * radius) * orbScale;
      lowest = Math.max(lowest, centerY + 45 * orbScale);
    }
    const clearance = bodyTop * visualScale - lowest;
    assert(Number.isFinite(lowest), 'Equipment position must be finite');
    checks.push({visualScale, local, lowest: Math.round(lowest), boundingBoxGap: Math.round(clearance)});
  }
}
assert.equal(Number(x), 0);
// At the supplied screenshot's approximately 760-high in-game foot anchor,
// the top slot must stay below the 120-high HUD, including the 45-unit icon radius.
const highest = 760 + Number(y) + 195 - (225 + 75 * 2 / 7) - 45;
assert(highest >= 120, 'Equipment arc moved into the top HUD');
console.log('COMBAT_LAYOUT_OK', JSON.stringify(checks));
