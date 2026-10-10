using STS2RitsuLib.Telemetry;

namespace Yjcm;

/// <summary>
/// 一将成名遥测注册入口，覆盖武诸葛亮与威董卓。只有玩家在 RitsuLib 授权界面明确同意后才会发送对应类别。
/// </summary>
public static class WuZhugeliangTelemetry
{
    // 只使用当前模组 ID；旧申请方数据不再兼容或读取。
    private const string TelemetryApplicantId = "Yjcm";
    // Cloudflare Worker 部署完成后填写根地址，例如 https://wuzhugeliang-telemetry.example.workers.dev。
    // 留空时不会注册遥测申请方，也不会向玩家弹出授权提示。
    private const string WorkerHost = "https://wuzhugeliang-telemetry.cdphantom-mod.workers.dev";

    private static int _registrationState;

    /// <summary>注册遥测申请方；可由正常模组入口或双版本 Loader 安全重复调用。</summary>
    public static void Register()
    {
        if (string.IsNullOrWhiteSpace(WorkerHost))
        {
            MainFile.Logger.Warn("遥测后端尚未配置，本次不注册遥测申请方");
            return;
        }

        if (Interlocked.CompareExchange(ref _registrationState, 1, 0) != 0)
            return;

        try
        {
            TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
            {
                ApplicantId = TelemetryApplicantId,
                OwnerModId = MainFile.ModId,
                DisplayName = "三国杀：一将成名mod",
                Adapter = new PostHogTelemetryAdapter(
                    host: WorkerHost,
                    projectApiKey: "proxy"),
                Requests =
                [
                    TelemetryRequest.BasicUsage(
                        "发送游戏、模组和 RitsuLib 版本、操作系统、游戏语言及匿名安装 ID，用于统计版本兼容性和活跃安装量。"),
                    TelemetryRequest.ModInventory(
                        "发送当前已登记模组的 ID、版本、加载状态和玩法影响标记，用于排查模组冲突；不发送本地文件路径。"),
                    TelemetryRequest.Diagnostics(
                        "发送游戏进程捕获的 .NET 异常类型、脱敏后的消息与堆栈，以及 RitsuLib 运行状态，用于定位崩溃和兼容问题。"),
                    TelemetryRequest.RunHistory(
                        "发送非放弃跑局结束后的原版 run-history JSON（角色、卡组、遗物、路线、战斗和结算信息），用于分别分析武诸葛亮与威董卓的平衡性。",
                        captureFilter: evt => !evt.IsAbandoned),
                ],
            });

            MainFile.Logger.Info($"遥测申请方已注册，发送端点: {WorkerHost}");
        }
        catch
        {
            Interlocked.Exchange(ref _registrationState, 0);
            throw;
        }
    }
}
