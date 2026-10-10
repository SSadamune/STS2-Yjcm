// The legacy /events endpoint can end pagination before all historical rows
// have been returned. Query explicitly bounded data and verify every count.
export const eventNames = ["run_history.completed", "session_start", "exception"];
const pageSizes = { "run_history.completed": 10, session_start: 500, exception: 500 };
const quote = (value) => `'${String(value).replaceAll("\\", "\\\\").replaceAll("'", "\\'")}'`;

export function createQueryClient(config, { fetchImpl = fetch, attempts = 2, timeoutMs = 45_000, retryDelayMs = 500 } = {}) {
  const base = new URL(config.host);
  if (base.protocol !== "https:" || !/^\d+$/.test(String(config.projectId)) || !config.personalApiKey) {
    throw new Error("PostHog 查询配置无效");
  }
  const url = new URL(`/api/projects/${config.projectId}/query/`, base);
  return async (sql) => {
    for (let attempt = 1; attempt <= attempts; attempt += 1) {
      try {
        const response = await fetchImpl(url, {
          method: "POST",
          redirect: "error", // Never forward a credential through a redirect.
          headers: { Accept: "application/json", "Content-Type": "application/json", Authorization: `Bearer ${config.personalApiKey}` },
          body: JSON.stringify({ query: { kind: "HogQLQuery", query: sql }, refresh: "force_blocking", name: "wuzhugeliang_dashboard_verified_read" }),
          signal: AbortSignal.timeout(timeoutMs),
        });
        if (!response.ok) {
          await response.body?.cancel();
          // Do not echo response bodies, which may contain sensitive data.
          throw new Error(`PostHog 查询失败（HTTP ${response.status}）`);
        }
        const body = await response.json();
        if (body.error || body.query_status?.error || body.query_status?.complete === false || !Array.isArray(body.results)) {
          throw new Error("PostHog 未返回已完成的查询结果");
        }
        return body.results;
      } catch (error) {
        if (attempt === attempts) throw error;
        await new Promise((resolve) => setTimeout(resolve, retryDelayMs));
      }
    }
  };
}

export function dateBounds(days, now = new Date()) {
  if (![0, 7, 30, 90, 365].includes(days)) throw new Error("不支持的时间范围");
  // Keep the exact same UTC upper bound for all pages and count checks.
  const before = new Date(Math.floor(now.getTime() / 1000) * 1000);
  const sqlDate = (date) => quote(date.toISOString().slice(0, 19).replace("T", " "));
  const after = days > 0 ? new Date(before.getTime() - days * 86_400_000) : null;
  const where = ["properties.applicant_id = 'Yjcm'", `timestamp < toDateTime(${sqlDate(before)}, 'UTC')`];
  if (after) where.push(`timestamp >= toDateTime(${sqlDate(after)}, 'UTC')`);
  return { where: where.join(" AND "), before: before.toISOString(), after: after?.toISOString() ?? null };
}

async function readCounts(query, where) {
  const rows = await query(`SELECT event, count() FROM events WHERE ${where} AND event IN (${eventNames.map(quote).join(", ")}) GROUP BY event LIMIT 10`);
  const counts = Object.fromEntries(eventNames.map((name) => [name, 0]));
  const seen = new Set();
  for (const row of rows) {
    if (!Array.isArray(row) || row.length !== 2 || !eventNames.includes(row[0]) || seen.has(row[0]) || !/^[0-9]+$/.test(String(row[1])) || !Number.isSafeInteger(Number(row[1]))) {
      throw new Error("PostHog 总数校验返回格式异常");
    }
    seen.add(row[0]);
    counts[row[0]] = Number(row[1]);
  }
  return counts;
}

function propertiesObject(value) {
  const parsed = typeof value === "string" ? JSON.parse(value) : value;
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error("跑局属性格式异常");
  if (parsed.applicant_id !== "Yjcm") throw new Error("跑局申请方不匹配");
  return parsed;
}

export async function readVerifiedEvents(query, { days = 30, now = new Date(), maxEvents = 20_000 } = {}) {
  const bounds = dateBounds(days, now);
  const expected = await readCounts(query, bounds.where);
  for (const name of eventNames) {
    if (expected[name] > maxEvents) throw new Error(`${name} 共 ${expected[name]} 条，超过安全读取上限 ${maxEvents}；请缩小时间范围，未显示截断数据`);
  }
  const result = {};
  // Read categories sequentially to avoid multiplying large response bodies.
  for (const name of eventNames) {
    const events = [];
    const ids = new Set();
    let cursor = null;
    while (events.length < expected[name]) {
      const cursorWhere = cursor
        ? ` AND (timestamp > ${quote(cursor.timestamp)} OR (timestamp = ${quote(cursor.timestamp)} AND toString(uuid) > ${quote(cursor.id)}))`
        : "";
      const properties = name === "run_history.completed" ? ", properties" : "";
      const rows = await query(`SELECT toString(timestamp), toString(uuid), distinct_id${properties} FROM events WHERE ${bounds.where} AND event = ${quote(name)}${cursorWhere} ORDER BY timestamp ASC, toString(uuid) ASC LIMIT ${pageSizes[name]}`);
      if (!rows.length) throw new Error(`${name} 读取不完整：应有 ${expected[name]} 条，只读到 ${events.length} 条；未覆盖完整统计`);
      for (const row of rows) {
        if (!Array.isArray(row) || row.length !== (properties ? 4 : 3)) throw new Error(`${name} 事件格式异常`);
        const [timestamp, id, distinctId, rawProperties] = row;
        if (typeof timestamp !== "string" || !/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(\.\d{1,6})?$/.test(timestamp) || typeof id !== "string" || !/^[0-9a-f-]{36}$/i.test(id)) {
          throw new Error(`${name} 分页游标格式异常`);
        }
        if (ids.has(id) || (cursor && (timestamp < cursor.timestamp || (timestamp === cursor.timestamp && id <= cursor.id)))) {
          throw new Error(`${name} 分页重复或顺序异常；未覆盖完整统计`);
        }
        ids.add(id);
        cursor = { timestamp, id };
        events.push({ event: name, id, timestamp: `${timestamp.replace(" ", "T")}Z`, distinct_id: distinctId, properties: properties ? propertiesObject(rawProperties) : {} });
      }
      if (events.length > expected[name]) throw new Error(`${name} 查询期间数据发生变化，请刷新重试`);
    }
    result[name] = events;
  }
  // Late-arriving events may fall before the fixed upper timestamp. Reject
  // inconsistent snapshots rather than silently publishing misleading totals.
  const checked = await readCounts(query, bounds.where);
  for (const name of eventNames) {
    if (checked[name] !== result[name].length) throw new Error(`${name} 总数校验不一致，请刷新重试；未覆盖完整统计`);
  }
  return { events: result, verification: { source: "PostHog Query API", complete: true, expected, received: checked, after: bounds.after, before: bounds.before } };
}
