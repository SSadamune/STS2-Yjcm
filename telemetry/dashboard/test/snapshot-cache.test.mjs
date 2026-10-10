import test from "node:test";
import assert from "node:assert/strict";
import { createSnapshotCache } from "../snapshot-cache.mjs";
const complete = (total = 132) => ({ runs: { total }, verification: { complete: true }, warnings: [], generatedAt: "2026-09-02T12:00:00Z" });

test("failed refresh preserves last complete snapshot without changing its timestamp", async () => {
  let time = 0, fail = false;
  const get = createSnapshotCache(async () => { if (fail) throw Error("timeout"); return complete(); }, { now: () => time });
  const first = await get(30);
  fail = true; time = 31000;
  const stale = await get(30);
  assert.equal(stale.runs.total, 132);
  assert.equal(stale.generatedAt, first.generatedAt);
  assert.equal(stale.stale, true);
  assert.match(stale.warnings[0], /保留上次完整统计/);
  fail = false; time = 62000;
  assert.equal((await get(30)).stale, false);
});

test("unverified smaller results never replace complete counts", async () => {
  let time = 0;
  const get = createSnapshotCache(async () => time ? { ...complete(5), verification: { complete: false } } : complete(), { now: () => time });
  await get(30); time = 31000;
  assert.equal((await get(30)).runs.total, 132);
});

test("different ranges never borrow cached data on failure", async () => {
  const get = createSnapshotCache(async days => { if (days === 7) throw Error("failed"); return complete(); });
  await get(30);
  await assert.rejects(get(7), /failed/);
});

test("concurrent loads are coalesced and completed results are briefly cached", async () => {
  let calls = 0;
  const get = createSnapshotCache(async () => { calls++; return complete(); });
  const [a, b] = await Promise.all([get(30), get(30)]);
  assert.equal(calls, 1);
  assert.equal(a, b);
  await get(30);
  assert.equal(calls, 1);
});

test("first failure produces an error, not a misleading zero; later retry can succeed", async () => {
  let calls = 0;
  const get = createSnapshotCache(() => { if (++calls === 1) throw Error("failed"); return complete(); });
  await assert.rejects(get(30), /failed/);
  assert.equal((await get(30)).runs.total, 132);
});
