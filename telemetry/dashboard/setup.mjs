import { mkdir, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createInterface, emitKeypressEvents } from "node:readline";

const dashboardDir = dirname(fileURLToPath(import.meta.url));
const configPath = join(dashboardDir, "config.local.json");
const rl = createInterface({ input: process.stdin, output: process.stdout });

function question(prompt) {
  return new Promise((resolve) => rl.question(prompt, (answer) => resolve(answer.trim())));
}

async function secretQuestion(prompt) {
  if (!process.stdin.isTTY || typeof process.stdin.setRawMode !== "function") {
    return question(prompt);
  }
  rl.pause();
  process.stdout.write(prompt);
  emitKeypressEvents(process.stdin);
  process.stdin.setRawMode(true);
  process.stdin.resume();
  process.stdin.setEncoding("utf8");
  let value = "";
  return new Promise((resolve, reject) => {
    const cleanup = () => {
      process.stdin.off("data", onData);
      process.stdin.setRawMode(false);
      process.stdin.pause();
      rl.resume();
    };
    const onData = (chunk) => {
      if (chunk === "\u0003") {
        cleanup();
        reject(new Error("已取消配置"));
        return;
      }
      if (chunk === "\r" || chunk === "\n") {
        process.stdout.write("\n");
        cleanup();
        resolve(value.trim());
        return;
      }
      if (chunk === "\u0008" || chunk === "\u007f") {
        if (value.length) {
          value = value.slice(0, -1);
          process.stdout.write("\b \b");
        }
        return;
      }
      const printable = [...chunk].filter((char) => char >= " ").join("");
      value += printable;
      process.stdout.write("*".repeat([...printable].length));
    };
    process.stdin.on("data", onData);
  });
}

try {
  console.log("\n武诸葛亮遥测仪表盘首次配置\n");
  console.log("这里需要 PostHog 的查询凭据，不是游戏上传使用的 phc_ Project token。");
  console.log("请在 PostHog：头像 → Settings → Personal API keys 创建只读密钥。");
  console.log("密钥只写入本机已忽略的 dashboard/config.local.json，不会发送给网页前端。\n");

  const hostInput = await question("PostHog 区域地址 [https://us.posthog.com]：");
  const host = (hostInput || "https://us.posthog.com").replace(/\/+$/, "");
  const projectId = await question("PostHog Project ID（纯数字）：");
  const personalApiKey = await secretQuestion("Personal API key（输入时显示 *）：");

  const url = new URL(host);
  if (url.protocol !== "https:") throw new Error("PostHog 地址必须使用 https://");
  if (!/^\d+$/.test(projectId)) throw new Error("Project ID 必须是纯数字");
  if (!personalApiKey) throw new Error("Personal API key 不能为空");

  await mkdir(dashboardDir, { recursive: true });
  await writeFile(
    configPath,
    `${JSON.stringify({ host, projectId, personalApiKey }, null, 2)}\n`,
    { encoding: "utf8", flag: "w" },
  );
  console.log(`\n配置已保存：${configPath}`);
  console.log("现在运行：npm.cmd run dashboard");
} catch (error) {
  console.error(`\n配置失败：${error.message}`);
  process.exitCode = 1;
} finally {
  rl.close();
}
