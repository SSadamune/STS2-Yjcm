// Only fully verified snapshots enter the cache. A failed refresh never
// replaces the previous data, and ranges cannot borrow each other's data.
export function createSnapshotCache(load, { now = Date.now, ttlMs = 30_000 } = {}) {
  const entries = new Map();
  const pending = new Map();
  return (days) => {
    if (pending.has(days)) return pending.get(days);
    const previous = entries.get(days);
    if (previous && now() - previous.checkedAt < ttlMs) return Promise.resolve(previous.value);
    const task = (async () => {
      try {
        const value = await Promise.resolve().then(() => load(days));
        if (value.verification?.complete !== true) throw new Error("统计完整性未通过校验");
        const complete = { ...value, stale: false };
        entries.set(days, { complete, value: complete, checkedAt: now() });
        return complete;
      } catch (error) {
        if (!previous) throw error;
        const value = { ...previous.complete, stale: true, warnings: [`刷新失败，保留上次完整统计：${error.message}`] };
        entries.set(days, { ...previous, value, checkedAt: now() });
        return value;
      } finally {
        pending.delete(days);
      }
    })();
    pending.set(days, task);
    return task;
  };
}
