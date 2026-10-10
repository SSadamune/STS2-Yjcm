const state = { data: null };
const $ = (selector) => document.querySelector(selector);
const percent = (value) => value == null ? "—" : `${(value * 100).toFixed(1)}%`;
const number = (value, digits = 0) => value == null ? "—" : Number(value).toLocaleString("zh-CN", { maximumFractionDigits: digits });
const duration = (seconds) => {
  if (seconds == null) return "—";
  const total = Math.round(seconds);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  return hours ? `${hours}时 ${minutes}分` : `${minutes}分`;
};
const escapeIdLabel = (id) => id.split(".").at(-1).replaceAll("_", " ");

function metric(label, value, detail) {
  const node = document.createElement("div");
  node.className = "metric";
  const labelNode = document.createElement("span"); labelNode.className = "label"; labelNode.textContent = label;
  const valueNode = document.createElement("strong"); valueNode.className = "value"; valueNode.textContent = value;
  const detailNode = document.createElement("span"); detailNode.className = "detail"; detailNode.textContent = detail;
  node.append(labelNode, valueNode, detailNode);
  return node;
}

function renderSummary(data) {
  const summary = $("#summary");
  summary.replaceChildren(
    metric(data.characterFilter === "all" ? "记录跑局" : `${data.characterLabel}跑局`, number(data.runs.total), `总体胜率 ${percent(data.runs.winRate)}`),
    metric("平均跑局时长", duration(data.runs.averageDurationSeconds), `中位数 ${duration(data.runs.medianDurationSeconds)}`),
    metric("谋司马懿击杀率", percent(data.simaYi.killRate), `${number(data.simaYi.kills)} 次击杀`),
    metric("谋司马懿被击杀率", percent(data.simaYi.deathRate), `${number(data.simaYi.deaths)} 次阵亡`),
    metric("匿名安装数", number(data.usage.uniqueInstalls), `${number(data.usage.sessions)} 次会话`),
    metric("异常 / 会话", percent(data.usage.exceptionsPerSession), `${number(data.usage.exceptions)} 条异常事件`),
  );
}

function renderSima(data) {
  $("#sima-sample").textContent = `样本 ${number(data.simaYi.encounters)} 场`;
  const bars = $("#sima-bars");
  bars.replaceChildren();
  for (const [label, value, count, kind] of [
    ["玩家击杀", data.simaYi.killRate, data.simaYi.kills, "win"],
    ["玩家阵亡", data.simaYi.deathRate, data.simaYi.deaths, "loss"],
  ]) {
    const row = document.createElement("div"); row.className = "outcome-row";
    const name = document.createElement("span"); name.textContent = label;
    const track = document.createElement("div"); track.className = "outcome-track";
    const fill = document.createElement("span"); fill.className = `outcome-fill ${kind}`; fill.style.width = `${Math.max(0, (value ?? 0) * 100)}%`;
    track.append(fill);
    const result = document.createElement("span"); result.className = "outcome-value"; result.textContent = `${percent(value)} · ${count}`;
    row.append(name, track, result); bars.append(row);
  }
}

function linePath(values, width, height, max) {
  if (!values.length) return "";
  return values.map((value, index) => {
    const x = values.length === 1 ? width / 2 : (index / (values.length - 1)) * width;
    const y = height - (value / Math.max(max, 1)) * height;
    return `${index ? "L" : "M"}${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
}

function renderTrend(data) {
  const svg = $("#trend");
  svg.replaceChildren();
  const width = 620, height = 125, top = 8;
  svg.setAttribute("viewBox", `0 0 ${width} 160`);
  for (let i = 0; i <= 4; i += 1) {
    const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
    line.setAttribute("x1", 0); line.setAttribute("x2", width); line.setAttribute("y1", top + i * height / 4); line.setAttribute("y2", top + i * height / 4); line.setAttribute("class", "gridline"); svg.append(line);
  }
  const max = Math.max(1, ...data.trend.flatMap((day) => [day.runs, day.simaEncounters]));
  for (const [key, className] of [["runs", "run-line"], ["simaEncounters", "boss-line"]]) {
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("d", linePath(data.trend.map((day) => day[key]), width, height, max));
    path.setAttribute("transform", `translate(0 ${top})`); path.setAttribute("class", className); svg.append(path);
  }
  if (data.trend.length) {
    for (const [x, text] of [[0, data.trend[0].day], [width, data.trend.at(-1).day]]) {
      const label = document.createElementNS("http://www.w3.org/2000/svg", "text");
      label.setAttribute("x", x); label.setAttribute("y", 154); label.setAttribute("text-anchor", x ? "end" : "start"); label.textContent = text; svg.append(label);
    }
  }
}

function renderCards() {
  if (!state.data) return;
  const query = $("#card-search").value.trim().toLowerCase();
  const modOnly = $("#mod-only").checked;
  const min = Number($("#min-offered").value);
  const sort = $("#card-sort").value;
  const cards = state.data.cards.filter((card) =>
    card.offered >= min &&
    (!modOnly || card.id.startsWith("CARD.YJCM_") || card.id.startsWith("CARD.WU_ZHUGELIANG_")) &&
    (!query || `${card.name ?? ""} ${card.id}`.toLowerCase().includes(query)),
  ).sort((a, b) => {
    if (sort === "pick_rate_desc") return (b.pickRate ?? -1) - (a.pickRate ?? -1) || b.offered - a.offered || a.id.localeCompare(b.id);
    if (sort === "picked_desc") return b.picked - a.picked || b.offered - a.offered || a.id.localeCompare(b.id);
    if (sort === "name_asc") return (a.name ?? escapeIdLabel(a.id)).localeCompare(b.name ?? escapeIdLabel(b.id), "zh-CN") || a.id.localeCompare(b.id);
    return b.offered - a.offered || b.picked - a.picked || a.id.localeCompare(b.id);
  });
  const body = $("#card-rows"); body.replaceChildren();
  if (!cards.length) {
    const row = document.createElement("tr"); const cell = document.createElement("td"); cell.colSpan = 5; cell.className = "empty"; cell.textContent = "当前筛选条件下没有卡牌样本"; row.append(cell); body.append(row); return;
  }
  for (const card of cards) {
    const row = document.createElement("tr");
    const cardCell = document.createElement("td");
    const name = document.createElement("div"); name.textContent = card.name ?? escapeIdLabel(card.id);
    const id = document.createElement("div"); id.className = "card-id"; id.textContent = card.id;
    cardCell.append(name, id);
    const offered = document.createElement("td"); offered.textContent = number(card.offered);
    const picked = document.createElement("td"); picked.textContent = number(card.picked);
    const rate = document.createElement("td"); rate.textContent = percent(card.pickRate);
    const barCell = document.createElement("td"); const track = document.createElement("div"); track.className = "mini-track"; const fill = document.createElement("span"); fill.className = "mini-fill"; fill.style.width = `${(card.pickRate ?? 0) * 100}%`; track.append(fill); barCell.append(track);
    row.append(cardCell, offered, picked, rate, barCell); body.append(row);
  }
}

function renderRankList(selector, entries) {
  const root = $(selector); root.replaceChildren();
  if (!entries.length) { const empty = document.createElement("div"); empty.className = "empty"; empty.textContent = "暂无数据"; root.append(empty); return; }
  for (const entry of entries.slice(0, 8)) {
    const row = document.createElement("div"); row.className = "rank-item";
    const name = document.createElement("span"); name.className = "rank-name"; name.title = entry.name; name.textContent = entry.name;
    const count = document.createElement("span"); count.className = "rank-count"; count.textContent = number(entry.count);
    row.append(name, count); root.append(row);
  }
}

function render(data) {
  state.data = data;
  renderSummary(data); renderSima(data); renderTrend(data); renderCards();
  renderRankList("#versions", data.breakdowns.versions);
  renderRankList("#characters", data.breakdowns.characters);
  renderRankList("#deaths", data.breakdowns.deathEncounters);
}

let loadSequence = 0;
const rangeLabel = (days) => days ? `最近 ${days} 天` : "全部数据";
const characterLabel = (data) => data?.characterLabel ?? "全部角色";
async function load() {
  const sequence = ++loadSequence;
  const button = $("#refresh"); const status = $("#status"); button.disabled = true; status.className = "status"; status.textContent = "正在从 PostHog 读取事件…";
  try {
    const character = encodeURIComponent($("#character").value);
    const response = await fetch(`/api/dashboard?days=${$("#range").value}&character=${character}`, { cache: "no-store" });
    const body = await response.json();
    if (sequence !== loadSequence) return;
    if (!response.ok) throw new Error(body.error || `HTTP ${response.status}`);
    if (body.verification?.complete !== true) throw new Error("统计尚未通过完整性校验，请重启遥测仪表盘服务后刷新");
    render(body);
    const warning = body.warnings?.length ? `；${body.warnings.join("；")}` : "";
    status.className = body.stale ? "status error" : "status";
    status.textContent = `${body.stale ? "当前显示上次完整数据" : "已连接 · 数据完整性校验通过"} · ${characterLabel(body)} · ${rangeLabel(body.rangeDays)} · 更新于 ${new Date(body.generatedAt).toLocaleString("zh-CN")}${warning}`;
  } catch (error) {
    if (sequence !== loadSequence) return;
    status.className = "status error";
    const retained = state.data ? `；仍显示上次“${characterLabel(state.data)} · ${rangeLabel(state.data.rangeDays)}”的完整数据（${new Date(state.data.generatedAt).toLocaleString("zh-CN")}）` : "；尚无完整数据可显示";
    status.textContent = `刷新失败：${error.message}${retained}`;
  } finally { if (sequence === loadSequence) button.disabled = false; }
}

$("#refresh").addEventListener("click", load);
$("#range").addEventListener("change", load);
$("#character").addEventListener("change", load);
$("#card-search").addEventListener("input", renderCards);
$("#mod-only").addEventListener("change", renderCards);
$("#min-offered").addEventListener("change", renderCards);
$("#card-sort").addEventListener("change", renderCards);
load();
