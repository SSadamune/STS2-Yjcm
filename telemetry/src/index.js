const MAX_BODY_BYTES = 5 * 1024 * 1024;
const MAX_BATCH_SIZE = 100;
const REQUEST_TIMEOUT_MS = 9000;
const APPLICANT_ID = 'Yjcm';
const TELEMETRY_SCHEMA = 'ritsulib.telemetry.v1';

const EVENT_RULES = new Map([
	['session_start', { requestId: 'basic_usage', category: 'BasicUsage' }],
	['mod_inventory', { requestId: 'mod_inventory', category: 'ModInventory' }],
	['run_history.completed', { requestId: 'run_history', category: 'RunHistory' }],
	['exception', { requestId: 'diagnostics', category: 'Diagnostics' }],
]);

const JSON_HEADERS = {
	'Content-Type': 'application/json; charset=utf-8',
	'Cache-Control': 'no-store',
	'X-Content-Type-Options': 'nosniff',
};

function jsonResponse(status, body, extraHeaders = {}) {
	return new Response(JSON.stringify(body), {
		status,
		headers: { ...JSON_HEADERS, ...extraHeaders },
	});
}

function validateEvent(event, index) {
	if (!event || typeof event !== 'object' || Array.isArray(event)) {
		return `batch[${index}] 必须是对象`;
	}

	const rule = EVENT_RULES.get(event.event);
	if (!rule) return `batch[${index}].event 不受支持`;

	if (typeof event.distinct_id !== 'string' || event.distinct_id.length === 0 || event.distinct_id.length > 128) {
		return `batch[${index}].distinct_id 必须是 1 至 128 字符的字符串`;
	}

	if (!event.properties || typeof event.properties !== 'object' || Array.isArray(event.properties)) {
		return `batch[${index}].properties 必须是对象`;
	}

	const properties = event.properties;
	if (properties.schema !== TELEMETRY_SCHEMA) return `batch[${index}].properties.schema 不匹配`;
	if (properties.applicant_id !== APPLICANT_ID) return `batch[${index}].properties.applicant_id 不匹配`;
	if (properties.request_id !== rule.requestId) return `batch[${index}].properties.request_id 不匹配`;
	if (properties.category !== rule.category) return `batch[${index}].properties.category 不匹配`;

	if (event.timestamp !== undefined && event.timestamp !== null) {
		if (typeof event.timestamp !== 'string' || Number.isNaN(Date.parse(event.timestamp))) {
			return `batch[${index}].timestamp 必须是有效时间字符串`;
		}
	}

	return null;
}

async function readJsonBody(request) {
	const declaredLength = Number.parseInt(request.headers.get('content-length') ?? '0', 10);
	if (Number.isFinite(declaredLength) && declaredLength > MAX_BODY_BYTES) {
		return { error: jsonResponse(413, { error: 'payload_too_large', max_bytes: MAX_BODY_BYTES }) };
	}

	const bytes = await request.arrayBuffer();
	if (bytes.byteLength > MAX_BODY_BYTES) {
		return { error: jsonResponse(413, { error: 'payload_too_large', max_bytes: MAX_BODY_BYTES }) };
	}

	try {
		return { body: JSON.parse(new TextDecoder().decode(bytes)) };
	} catch {
		return { error: jsonResponse(400, { error: 'invalid_json' }) };
	}
}

function normalizePostHogHost(value) {
	const fallback = 'https://us.i.posthog.com';
	let url;
	try {
		url = new URL(value || fallback);
	} catch {
		return null;
	}

	if (url.protocol !== 'https:') return null;
	return url.origin;
}

function sanitizeBatch(batch) {
	return batch.map((event) => {
		const properties = { ...event.properties };
		delete properties.$ip;
		return {
			event: event.event,
			distinct_id: event.distinct_id,
			properties,
			timestamp: event.timestamp,
		};
	});
}

async function forwardBatch(body, env) {
	if (!env.POSTHOG_API_KEY) {
		return jsonResponse(503, { error: 'backend_not_configured' });
	}

	const postHogHost = normalizePostHogHost(env.POSTHOG_HOST);
	if (!postHogHost) {
		return jsonResponse(500, { error: 'invalid_upstream_configuration' });
	}

	const controller = new AbortController();
	const timeout = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
	let upstream;
	try {
		upstream = await fetch(`${postHogHost}/batch/`, {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ api_key: env.POSTHOG_API_KEY, batch: sanitizeBatch(body.batch) }),
			signal: controller.signal,
		});
	} catch (error) {
		console.error(`[遥测] PostHog 请求失败: ${error instanceof Error ? error.message : String(error)}`);
		return jsonResponse(502, { error: 'upstream_unreachable' });
	} finally {
		clearTimeout(timeout);
	}

	if (!upstream.ok) {
		console.error(`[遥测] PostHog 返回 ${upstream.status}`);
		return jsonResponse(502, { error: 'upstream_failed', upstream_status: upstream.status });
	}

	return jsonResponse(200, { ok: true, accepted: body.batch.length });
}

export default {
	async fetch(request, env) {
		const url = new URL(request.url);
		if (request.method === 'GET' && url.pathname === '/health') {
			return jsonResponse(200, { ok: true, service: 'wuzhugeliang-telemetry' });
		}

		if (url.pathname !== '/batch' && url.pathname !== '/batch/') {
			return jsonResponse(404, { error: 'not_found' });
		}

		if (request.method !== 'POST') {
			return jsonResponse(405, { error: 'method_not_allowed' }, { Allow: 'POST' });
		}

		const contentType = request.headers.get('content-type') ?? '';
		if (!contentType.toLowerCase().includes('application/json')) {
			return jsonResponse(415, { error: 'unsupported_media_type' });
		}

		const parsed = await readJsonBody(request);
		if (parsed.error) return parsed.error;

		const body = parsed.body;
		if (!body || typeof body !== 'object' || Array.isArray(body)) {
			return jsonResponse(400, { error: 'invalid_format', message: '请求体必须是对象' });
		}
		if (!Array.isArray(body.batch) || body.batch.length === 0) {
			return jsonResponse(400, { error: 'invalid_format', message: 'batch 必须是非空数组' });
		}
		if (body.batch.length > MAX_BATCH_SIZE) {
			return jsonResponse(400, { error: 'batch_too_large', max_events: MAX_BATCH_SIZE });
		}

		for (let index = 0; index < body.batch.length; index += 1) {
			const validationError = validateEvent(body.batch[index], index);
			if (validationError) {
				return jsonResponse(400, { error: 'invalid_event', message: validationError });
			}
		}

		return forwardBatch(body, env);
	},
};
