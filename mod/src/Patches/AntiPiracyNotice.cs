using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace Yjcm;

/// <summary>
/// 主菜单公告入口。公告正文集中在 <see cref="AntiPiracyNoticeContent"/>，作者更新文案时无需改动状态逻辑。
/// </summary>
[HarmonyPatch(typeof(NGame), "LaunchMainMenu")]
internal static class AntiPiracyNoticeMainMenuPatch
{
    private static void Postfix(NGame __instance, ref Task __result) =>
        __result = ShowAfterMainMenu(__result, __instance);

    private static async Task ShowAfterMainMenu(Task startup, NGame game)
    {
        await startup;
        await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        await AntiPiracyNoticeController.PresentAtMainMenu(game);
    }
}

internal static class AntiPiracyNoticeContent
{
    public const string Title = "一将成名反盗版公告";

    // 弹窗、设置页和每日提示都会展示同一份正文。
    public const string Body = "三国杀：一将成名mod，作者CDphantom（爱发电同名），b站CD幻影，交流群1106930563。\n\n"
        + "mod素材均取自三国杀游戏素材与ai生成，本mod仅发布于杀戮尖塔2steam创意工坊，[color=#FF4D4D]【未发布于其他任何平台】[/color]，包括但不限于小黑盒mod平台，玩家动力，踩蘑菇，某喵姓mod发布站，n网等。\n\n"
        + "本mod纯免费发布，[color=#FF4D4D]【不含任何收费内容】[/color]，若您从付费渠道获取了本mod，说明其为盗版。\n\n"
        + "若您暂时无法负担杀戮尖塔2的购买费用，只能暂时游玩盗版无法使用创意工坊，欢迎加入交流群，群主会免费为您提供mod，但希望您有能力后会自觉补票。\n\n"
        + "如果您喜欢本mod，欢迎前往爱发电赞助作者（注意不提供任何收费专属内容，仅为单纯赞助）。";
}

/// <summary>处理版本感知的抑制状态、每日提示与主菜单 UI。</summary>
internal static partial class AntiPiracyNoticeController
{
    private const string NoticeNodeName = "YjcmAntiPiracyNotice";
    private const string NudgeNodeName = "YjcmAntiPiracyNoticeNudge";

    internal static async Task PresentAtMainMenu(NGame game)
    {
        string version = GetInstalledVersion();
        if (WuZhugeliangSettings.ShouldShowAntiPiracyNotice(version))
        {
            bool firstInstall = !WuZhugeliangSettings.HasSeenAnyAntiPiracyNotice();
            if (await WaitForAvailableModal(game) &&
                (firstInstall ? TryShowNotice(version) : TryShowNudge(version, autoDismissAfterTenSeconds: true)))
            {
                WuZhugeliangSettings.MarkAntiPiracyNoticeSeen(version);
                WuZhugeliangSettings.MarkAntiPiracyNoticePromptedToday();
            }
            return;
        }

        if (!WuZhugeliangSettings.ShouldShowAntiPiracyNoticeNudge())
            return;

        if (await WaitForAvailableModal(game) && TryShowNudge(version, autoDismissAfterTenSeconds: false))
            WuZhugeliangSettings.MarkAntiPiracyNoticePromptedToday();
    }

    internal static void ShowFromSettings()
    {
        string version = GetInstalledVersion();
        if (!TryShowNotice(version))
            MainFile.Logger.Warn("反盗版公告未能打开：当前存在其他模态窗口或主界面尚未初始化。");
    }

    private static async Task<bool> WaitForAvailableModal(NGame game)
    {
        // 等待游戏自己的首次启动弹窗结束，避免抢占或清除它们。
        for (int frame = 0; frame < 600; frame++)
        {
            NModalContainer? modalContainer = NModalContainer.Instance;
            if (modalContainer != null && modalContainer.OpenModal == null)
                return true;

            await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        MainFile.Logger.Warn("反盗版公告等待主界面模态窗口超时，已跳过本次显示。");
        return false;
    }

    private static bool TryShowNotice(string version)
    {
        NModalContainer? modalContainer = NModalContainer.Instance;
        if (modalContainer == null || modalContainer.OpenModal != null ||
            modalContainer.GetNodeOrNull<AntiPiracyNoticePopup>(NoticeNodeName) != null)
            return false;

        modalContainer.Add(new AntiPiracyNoticePopup(version));
        return true;
    }

    private static bool TryShowNudge(string version, bool autoDismissAfterTenSeconds)
    {
        NModalContainer? modalContainer = NModalContainer.Instance;
        if (modalContainer == null || modalContainer.OpenModal != null ||
            modalContainer.GetNodeOrNull<Control>(NudgeNodeName) != null)
            return false;

        modalContainer.AddChild(new AntiPiracyNoticeNudge(version, autoDismissAfterTenSeconds));
        return true;
    }

    private static string GetInstalledVersion()
    {
        string? directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            foreach (string name in new[] { "mod_manifest.json", "WuZhugeliang.json" })
            {
                try
                {
                    using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, name)));
                    if (manifest.RootElement.TryGetProperty("version", out JsonElement version) &&
                        !string.IsNullOrWhiteSpace(version.GetString()))
                        return version.GetString()!;
                }
                catch (Exception ex) when (ex is IOException or JsonException)
                {
                    MainFile.Logger.Warn($"读取反盗版公告版本失败（{name}）：{ex.Message}");
                }
            }
        }

        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    }

    private sealed partial class AntiPiracyNoticePopup : Control, IScreenContext
    {
        private readonly string _version;
        private CheckBox? _dontShowAgain;
        private Button? _closeButton;

        public AntiPiracyNoticePopup(string version)
        {
            _version = version;
            Name = NoticeNodeName;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            BuildUi();
        }

        public Control? DefaultFocusedControl => _closeButton;

        private void BuildUi()
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(780, 520),
                Position = new Vector2(-390, -260)
            };
            panel.SetAnchorsPreset(LayoutPreset.Center);
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
            AddChild(panel);

            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", 16);
            panel.AddChild(layout);

            var title = new Label
            {
                Text = AntiPiracyNoticeContent.Title,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 30);
            layout.AddChild(title);

            var body = new RichTextLabel
            {
                Text = AntiPiracyNoticeContent.Body,
                BbcodeEnabled = true,
                FitContent = false,
                ScrollActive = true,
                CustomMinimumSize = new Vector2(700, 330),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            body.AddThemeFontSizeOverride("normal_font_size", 18);
            layout.AddChild(body);

            _dontShowAgain = new CheckBox { Text = "以后不再弹出" };
            layout.AddChild(_dontShowAgain);

            _closeButton = new Button
            {
                Text = "我已知晓",
                CustomMinimumSize = new Vector2(180, 44),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };
            _closeButton.Pressed += Close;
            layout.AddChild(_closeButton);
        }

        private void Close()
        {
            WuZhugeliangSettings.SetAntiPiracyNoticeSuppressedForVersion(_version, _dontShowAgain?.ButtonPressed == true);
            if (GetParent() == NModalContainer.Instance)
                NModalContainer.Instance!.Clear();
            else
                QueueFree();
        }
    }

    private sealed partial class AntiPiracyNoticeNudge : PanelContainer
    {
        private const double AutoDismissDurationSeconds = 10.0;
        private readonly bool _autoDismissAfterTenSeconds;
        private ProgressBar? _dismissProgress;
        private double _remainingDismissSeconds = AutoDismissDurationSeconds;

        public AntiPiracyNoticeNudge(string version, bool autoDismissAfterTenSeconds)
        {
            _autoDismissAfterTenSeconds = autoDismissAfterTenSeconds;
            Name = NudgeNodeName;
            SetAnchorsPreset(LayoutPreset.TopRight);
            Position = new Vector2(-360, 32);
            CustomMinimumSize = new Vector2(328, 66);
            MouseFilter = MouseFilterEnum.Stop;
            AddThemeStyleboxOverride("panel", CreatePanelStyle());

            var content = new VBoxContainer();
            content.AddThemeConstantOverride("separation", 6);
            AddChild(content);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            content.AddChild(row);

            var show = new Button
            {
                Text = "点击查看反盗版公告",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            show.Pressed += () =>
            {
                QueueFree();
                TryShowNotice(version);
            };
            row.AddChild(show);

            var close = new Button { Text = "×", TooltipText = "关闭" };
            close.Pressed += QueueFree;
            row.AddChild(close);

            if (_autoDismissAfterTenSeconds)
            {
                _dismissProgress = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = AutoDismissDurationSeconds,
                    Value = AutoDismissDurationSeconds,
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(0, 4),
                    MouseFilter = MouseFilterEnum.Ignore
                };
                _dismissProgress.AddThemeStyleboxOverride("background", CreateCountdownBackgroundStyle());
                _dismissProgress.AddThemeStyleboxOverride("fill", CreateCountdownFillStyle());
                content.AddChild(_dismissProgress);
            }
        }

        public override void _Ready()
        {
            if (!_autoDismissAfterTenSeconds)
                return;

            var autoClose = new Godot.Timer { OneShot = true, WaitTime = AutoDismissDurationSeconds };
            autoClose.Timeout += QueueFree;
            AddChild(autoClose);
            autoClose.Start();
        }

        public override void _Process(double delta)
        {
            if (!_autoDismissAfterTenSeconds || _dismissProgress == null)
                return;

            _remainingDismissSeconds = System.Math.Max(0, _remainingDismissSeconds - delta);
            _dismissProgress.Value = _remainingDismissSeconds;
        }
    }

    private static StyleBoxFlat CreateCountdownBackgroundStyle() => new()
    {
        BgColor = new Color("4a404e"),
        CornerRadiusTopLeft = 2,
        CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2,
        CornerRadiusBottomRight = 2
    };

    private static StyleBoxFlat CreateCountdownFillStyle() => new()
    {
        BgColor = new Color("d6ad50"),
        CornerRadiusTopLeft = 2,
        CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2,
        CornerRadiusBottomRight = 2
    };

    private static StyleBoxFlat CreatePanelStyle() => new()
    {
        BgColor = new Color("1d1a24"),
        BorderColor = new Color("d6ad50"),
        BorderWidthLeft = 2,
        BorderWidthTop = 2,
        BorderWidthRight = 2,
        BorderWidthBottom = 2,
        CornerRadiusTopLeft = 8,
        CornerRadiusTopRight = 8,
        CornerRadiusBottomLeft = 8,
        CornerRadiusBottomRight = 8,
        ContentMarginLeft = 28,
        ContentMarginTop = 24,
        ContentMarginRight = 28,
        ContentMarginBottom = 24
    };
}
