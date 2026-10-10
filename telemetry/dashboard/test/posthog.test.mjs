import test from "node:test";
import assert from "node:assert/strict";
import { createQueryClient, dateBounds, readVerifiedEvents } from "../posthog.mjs";
import { buildDashboardData } from "../analytics.mjs";

const now = new Date("2026-09-02T12:00:00Z");
const config = { host: "https://us.posthog.com", projectId: 123, personalApiKey: "test-only" };
const id = (n) => `00000000-0000-0000-0000-${String(n).padStart(12, "0")}`;
const row = (n, timestamp = "2026-08-24 12:00:00.123456") => [timestamp, id(n), "anonymous-test", {
  applicant_id: "Yjcm", payload: { applicant_payload: { run_history: { card_choices: [{ card: { id: "CARD.TEST" }, was_picked: true }] } } },
}];
const counts = (n) => [["run_history.completed", n]];
function queueQuery(responses, sqls = []) {
  return async (sql) => {
    sqls.push(sql);
    assert.ok(responses.length, "unexpected extra query");
    const response = responses.shift();
    if (response instanceof Error) throw response;
    return response;
  };
}

test("continues short pages and preserves same-timestamp events using UUID tie-breaker", async () => {
  const sqls = [];
  const responses = [counts(3), [row(1)], [row(2), row(3)], counts(3)];
  const result = await readVerifiedEvents(queueQuery(responses, sqls), { now });
  assert.equal(result.verification.complete, true);
  assert.equal(result.events["run_history.completed"].length, 3);
  assert.match(sqls[2], /timestamp = '2026-08-24 12:00:00.123456'/);
  assert.ok(sqls[2].includes(`toString(uuid) > '${id(1)}'`));
  assert.ok(sqls.every(sql => sql.includes("properties.applicant_id = 'Yjcm'")));
  assert.equal(buildDashboardData({ runEvents: result.events["run_history.completed"] }).cards[0].offered, 3);
  assert.equal(responses.length, 0);
});

test("rejects silently shortened results (132 expected, only 5 returned)", async () => {
  await assert.rejects(readVerifiedEvents(queueQuery([counts(132), [1, 2, 3, 4, 5].map(n => row(n)), []]), { now }), /应有 132 条，只读到 5 条/);
});

test("rejects a failure midway through pagination", async () => {
  await assert.rejects(readVerifiedEvents(queueQuery([counts(2), [row(1)], new Error("timeout")]), { now }), /timeout/);
});

test("rejects duplicate pagination rather than counting the same event twice", async () => {
  await assert.rejects(readVerifiedEvents(queueQuery([counts(2), [row(1)], [row(1)]]), { now }), /分页重复/);
});

test("rejects data changed while querying", async () => {
  await assert.rejects(readVerifiedEvents(queueQuery([counts(1), [row(1)], counts(2)]), { now }), /总数校验不一致/);
});

test("zero events is valid only when both count checks agree", async () => {
  const result = await readVerifiedEvents(queueQuery([[], []]), { now });
  assert.equal(result.verification.complete, true);
  assert.deepEqual(result.events["run_history.completed"], []);
});

test("all time has no implicit 24-hour lower bound; ranges share fixed UTC bounds", () => {
  const all = dateBounds(0, now);
  assert.equal(all.after, null);
  assert.doesNotMatch(all.where, /timestamp >=/);
  const month = dateBounds(30, now);
  assert.equal(month.after, "2026-08-03T12:00:00.000Z");
  assert.equal(all.before, month.before);
  assert.throws(() => dateBounds(1, now), /不支持/);
});

test("never presents data truncated by the safety cap", async () => {
  await assert.rejects(readVerifiedEvents(queueQuery([counts(20001)]), { now }), /超过安全读取上限/);
});

test("rejects invalid row properties and applicant mismatch", async () => {
  const invalid = row(1); invalid[3] = { applicant_id: "OtherMod" };
  await assert.rejects(readVerifiedEvents(queueQuery([counts(1), [invalid]]), { now }), /申请方不匹配/);
});

test("uses Query API, fresh blocking results and redirect protection", async () => {
  const query = createQueryClient(config, { fetchImpl: async (url, options) => {
    assert.equal(url.href, "https://us.posthog.com/api/projects/123/query/");
    assert.equal(options.method, "POST");
    assert.equal(options.redirect, "error");
    const body = JSON.parse(options.body);
    assert.equal(body.refresh, "force_blocking");
    assert.equal(body.query.kind, "HogQLQuery");
    return new Response(JSON.stringify({ results: [[132]] }));
  } });
  assert.deepEqual(await query("SELECT count() FROM events"), [[132]]);
});

test("retries connection failures but does not return partial results", async () => {
  let calls = 0;
  const query = createQueryClient(config, { retryDelayMs: 0, fetchImpl: async () => {
    if (++calls === 1) throw new Error("connection reset");
    return new Response(JSON.stringify({ results: [] }));
  } });
  assert.deepEqual(await query("SELECT 1"), []);
  assert.equal(calls, 2);
});

test("fails clearly for authorization errors without echoing response bodies", async () => {
  const query = createQueryClient(config, { attempts: 1, fetchImpl: async () => new Response("secret-response", { status: 403 }) });
  await assert.rejects(query("SELECT 1"), error => /403/.test(error.message) && !error.message.includes("secret-response"));
});

test("rejects unfinished query responses and insecure configs", async () => {
  const query = createQueryClient(config, { attempts: 1, fetchImpl: async () => new Response(JSON.stringify({ results: [], query_status: { complete: false } })) });
  await assert.rejects(query("SELECT 1"), /未返回已完成/);
  assert.throws(() => createQueryClient({ ...config, host: "http://example.com" }), /配置无效/);
});
