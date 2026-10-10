using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>事件专属先古能力牌；注册到通用 Token 池，避免进入董卓普通卡池、奖励和商店。</summary>
[RegisterCard(typeof(TokenCardPool), FullPublicEntry = "YJCM_CARD_WEI_DONG_ZHUO_QU_YUAN_LI_SAO")]
public sealed class WeiDongZhuoQuYuanLiSao : WeiDongZhuoCard
{
    private const string SecondsKey = "Seconds";
    private static readonly string VoicePath = RuntimeVoice("qu_yuan_li_sao.wav");

    public WeiDongZhuoQuYuanLiSao()
        : base(0, CardType.Power, CardRarity.Ancient, TargetType.Self, shouldShowInCardLibrary: false) { }

    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedInCombat => false;

    public override string CustomPortraitPath => "res://WeiDongZhuo/images/qu_yuan_li_sao.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(SecondsKey, 10)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        if (IsUpgraded)
            await PowerCmd.Apply<JieLiSaoPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        else
            await PowerCmd.Apply<LiSaoPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade() => DynamicVars[SecondsKey].UpgradeValueBy(5);

    internal static string RuntimeVoice(string file) => Path.Combine(
        Path.GetDirectoryName(typeof(WeiDongZhuoQuYuanLiSao).Assembly.Location)!,
        "Voice", "WeiDongZhuo", file);
}

public sealed class LiSaoPower : LiSaoPowerBase
{
    protected override int AnswerSeconds => 10;
    protected override string DescriptionKey => "YJCM_POWER_LI_SAO.description";
}

public sealed class JieLiSaoPower : LiSaoPowerBase
{
    protected override int AnswerSeconds => 15;
    protected override string DescriptionKey => "YJCM_POWER_JIE_LI_SAO.description";
}

/// <summary>
/// Amount 是已打出的同版本离骚数量；每层每回合独立出题。
/// CorrectThisTurn 独立记录答对数，倍率为 2 的答对数次方。
/// 普通与升级版是两个模型，因此混合打出时各自保留 10/15 秒答题时间，倍率自然相乘。
/// </summary>
[RegisterPower(Inherit = true)]
public abstract class LiSaoPowerBase : ModPowerTemplate
{
    private int _correctThisTurn;
    private static readonly string PunishVoicePath = WeiDongZhuoQuYuanLiSao.RuntimeVoice("li_sao_punish.wav");

    protected abstract int AnswerSeconds { get; }
    protected abstract string DescriptionKey { get; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://WeiDongZhuo/images/li_sao_power.png";
    public override string CustomBigIconPath => CustomIconPath;
    public override LocString Title => new("powers", "YJCM_POWER_LI_SAO.title");
    public override LocString Description => new("powers", DescriptionKey);

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _correctThisTurn = 0;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature != Owner || Owner.IsDead || Amount <= 0) return;

        _correctThisTurn = 0;
        for (int i = 0; i < Amount && Owner.IsAlive; i++)
        {
            var rng = player.RunState.Rng.CombatCardSelection;
            int questionIndex = rng.NextInt(LiSaoQuestionBank.Count);
            int wrongIndex = rng.NextInt(LiSaoQuestionBank.Count - 1);
            if (wrongIndex >= questionIndex) wrongIndex++;
            bool correctFirst = rng.NextInt(2) == 0;
            var question = LiSaoQuestionBank.Get(questionIndex);
            string wrong = LiSaoQuestionBank.Get(wrongIndex).Answer;
            int correctChoice = correctFirst ? 0 : 1;
            string first = correctFirst ? question.Answer : wrong;
            string second = correctFirst ? wrong : question.Answer;

            int choice = await LiSaoQuizChoice.Ask(
                choiceContext, player, question.Prompt, first, second, AnswerSeconds, correctChoice);
            if (choice == correctChoice)
            {
                _correctThisTurn++;
                Flash();
            }
            else
            {
                Flash();
                VoicePlayer.PlayWav(PunishVoicePath);
                await PowerCmd.Apply<LiSaoCannotPlayPower>(choiceContext, Owner, 1, Owner, null, false);
            }
        }
    }

#if GAME_BETA
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
#else
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
#endif
    {
        if (_correctThisTurn <= 0 || dealer != Owner || !props.IsPoweredAttack() || cardSource == null)
            return 1m;
        decimal multiplier = 1m;
        for (int i = 0; i < _correctThisTurn; i++) multiplier *= 2m;
        return multiplier;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (participants.Contains(Owner)) _correctThisTurn = 0;
    }
}

[RegisterPower]
public sealed class LiSaoCannotPlayPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://WeiDongZhuo/images/li_sao_power.png";
    public override string CustomBigIconPath => CustomIconPath;
    public override LocString Title => new("powers", "YJCM_POWER_LI_SAO_CANNOT_PLAY.title");
    public override LocString Description => new("powers", "YJCM_POWER_LI_SAO_CANNOT_PLAY.description");

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner?.Creature != Owner;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

internal static class LiSaoQuizChoice
{
    internal static async Task<int> Ask(
        PlayerChoiceContext context, Player player, string prompt,
        string first, string second, int seconds, int correctChoice)
    {
        if (TestMode.IsOn) return correctChoice;

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
#if GAME_BETA
        await context.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.CancelPlayCardActions);
#else
        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.CancelPlayCardActions);
#endif
        try
        {
            int result;
            if (LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay)
            {
                NPlayerHand.Instance?.CancelAllCardPlay();
                result = await LiSaoQuizOverlay.Show(prompt, first, second, seconds);
                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                    player, choiceId, PlayerChoiceResult.FromIndex(result));
            }
            else
            {
                result = (await RunManager.Instance.PlayerChoiceSynchronizer
                    .WaitForRemoteChoice(player, choiceId)).AsIndex();
            }
            return result;
        }
        finally
        {
            await context.SignalPlayerChoiceEnded();
        }
    }
}

internal sealed partial class LiSaoQuizOverlay : Control
{
    private readonly TaskCompletionSource<int> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Label _timerLabel;
    private double _remaining;
    private bool _finished;

    private LiSaoQuizOverlay(string prompt, string first, string second, int seconds)
    {
        Name = "LiSaoQuizOverlay";
        MouseFilter = MouseFilterEnum.Stop;
        _remaining = seconds;

        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.78f), MouseFilter = MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dim);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.OffsetLeft = -660;
        panel.OffsetRight = 660;
        panel.OffsetTop = -285;
        panel.OffsetBottom = 285;
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color("251A16F2"),
            BorderColor = new Color("B98A31"),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 52);
        margin.AddThemeConstantOverride("margin_right", 52);
        margin.AddThemeConstantOverride("margin_top", 35);
        margin.AddThemeConstantOverride("margin_bottom", 35);
        panel.AddChild(margin);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 22);
        margin.AddChild(column);

        var title = MakeLabel("《离骚》下句是？", 42, new Color("F4D06F"));
        column.AddChild(title);
        var question = MakeLabel(prompt, 38, Colors.White);
        question.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        question.CustomMinimumSize = new Vector2(0, 90);
        column.AddChild(question);

        _timerLabel = MakeLabel(string.Empty, 30, new Color("FFCC80"));
        column.AddChild(_timerLabel);

        var options = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        options.AddThemeConstantOverride("separation", 28);
        column.AddChild(options);
        var firstButton = MakeButton(first);
        var secondButton = MakeButton(second);
        firstButton.Pressed += () => Complete(0);
        secondButton.Pressed += () => Complete(1);
        options.AddChild(firstButton);
        options.AddChild(secondButton);
        firstButton.GrabFocus();
        UpdateTimer();
    }

    internal static async Task<int> Show(string prompt, string first, string second, int seconds)
    {
        if (Engine.GetMainLoop() is not SceneTree tree) return -1;
        var layer = new CanvasLayer { Name = "LiSaoQuizLayer", Layer = 90 };
        var overlay = new LiSaoQuizOverlay(prompt, first, second, seconds);
        layer.AddChild(overlay);
        tree.Root.AddChild(layer);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        try
        {
            return await overlay._completion.Task;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(layer)) layer.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        _remaining -= delta;
        UpdateTimer();
        if (_remaining <= 0d) Complete(-1);
    }

    private void Complete(int result)
    {
        if (_finished) return;
        _finished = true;
        MouseFilter = MouseFilterEnum.Ignore;
        _completion.TrySetResult(result);
    }

    private void UpdateTimer()
    {
        _timerLabel.Text = $"剩余时间：{Math.Max(0, Math.Ceiling(_remaining)):0} 秒";
        _timerLabel.Modulate = _remaining <= 3d ? new Color("FF665E") : new Color("FFCC80");
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static Button MakeButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(575, 118),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.All
        };
        button.AddThemeFontSizeOverride("font_size", 29);
        return button;
    }
}
