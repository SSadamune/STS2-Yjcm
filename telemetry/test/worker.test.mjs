import assert from 'node:assert/strict';
import test from 'node:test';

import worker from '../src/index.js';

function validEvent() {
	return {
		event: 'session_start',
		distinct_id: 'anonymous-test-install',
		properties: {
			schema: 'ritsulib.telemetry.v1',
			applicant_id: 'Yjcm',
			request_id: 'basic_usage',
			category: 'BasicUsage',
			$ip: '192.0.2.1',
		},
		timestamp: '2026-08-22T00:00:00.000Z',
	};
}

function post(body) {
	return new Request('https://telemetry.example/batch/', {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(body),
	});
}

test('健康检查不会访问上游', async () => {
	const response = await worker.fetch(new Request('https://telemetry.example/health'), {});
	assert.equal(response.status, 200);
	assert.deepEqual(await response.json(), { ok: true, service: 'wuzhugeliang-telemetry' });
});

test('拒绝错误的申请方', async () => {
	const event = validEvent();
	event.properties.applicant_id = 'OtherMod';
	const response = await worker.fetch(post({ batch: [event] }), { POSTHOG_API_KEY: 'phc_test' });
	assert.equal(response.status, 400);
	assert.equal((await response.json()).error, 'invalid_event');
});

test('未配置服务端密钥时不转发', async () => {
	const response = await worker.fetch(post({ batch: [validEvent()] }), {});
	assert.equal(response.status, 503);
	assert.equal((await response.json()).error, 'backend_not_configured');
});

test('只把校验后的批次和服务端密钥发给 PostHog', async () => {
	const originalFetch = globalThis.fetch;
	let forwarded;
	globalThis.fetch = async (url, options) => {
		forwarded = { url, options };
		return new Response('{}', { status: 200 });
	};

	try {
		const response = await worker.fetch(
			post({ api_key: '来自客户端的伪密钥', batch: [validEvent()] }),
			{ POSTHOG_API_KEY: '服务端真实密钥', POSTHOG_HOST: 'https://us.i.posthog.com' },
		);
		assert.equal(response.status, 200);
		assert.equal(forwarded.url, 'https://us.i.posthog.com/batch/');
		const forwardedBody = JSON.parse(forwarded.options.body);
		assert.equal(forwardedBody.api_key, '服务端真实密钥');
		assert.equal(forwardedBody.batch.length, 1);
		assert.equal(forwardedBody.batch[0].properties.$ip, undefined);
	} finally {
		globalThis.fetch = originalFetch;
	}
});
