using Godot;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

#nullable enable

namespace Yjcm;

/// <summary>第三幕首领「谋司马懿」。</summary>
[RegisterMonster]
public sealed class MouSimaYi : ModMonsterTemplate
{
    public const string HeavyMoveId = "HEAVY_ATTACK";
    public const string MultiMoveId = "MULTI_ATTACK";
    public const string GrowthMoveId = "GROWTH";
    public const string DefenseMoveId = "DEFENSE";
    public const string AttackDefenseMoveId = "ATTACK_DEFENSE";
    public const string AttackGrowthMoveId = "ATTACK_GROWTH";
    public const string DefenseGrowthMoveId = "DEFENSE_GROWTH";

    [Flags]
    public enum MoveCategory
    {
        None = 0,
        Attack = 1,
        Defense = 2,
        Growth = 4
    }

    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 450, 300);

    public override int MaxInitialHp => MinInitialHp;

    public override bool HasDeathSfx => false;

    public bool IsYangForm { get; private set; } = true;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://Yjcm/scenes/creature_visuals/mou_sima_yi.tscn");

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    public override async Task AfterAddedToRoom()
    {
        TryEnsureSpineVisuals();
        MouSimaYiBgmPlayer.Play();
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<QuanMouPower>(context, Creature, 1, Creature, null);
        await PowerCmd.Apply<PingLiaoPower>(context, Creature, 1, Creature, null);
        await EnterForm(context, true);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        MouSimaYiSpineVisuals.StopActionOverlay();
        MouSimaYiBgmPlayer.Stop();
    }

    /// <summary>切换本体形态，并给所有玩家叠加对应的权谋。</summary>
    public async Task EnterForm(PlayerChoiceContext choiceContext, bool yang)
    {
        bool previousYang = IsYangForm;
        bool formChanged = IsYangForm != yang;
        IsYangForm = yang;
        if (formChanged)
            _ = PlayFormTransitionVisual(previousYang, yang);
        else
            RefreshFormVisuals();

        // 开局仅初始化阳形态；之后真正发生阴阳转换时才播放对应权谋语音。
        if (formChanged)
            MouSimaYiSfxPlayer.PlayQuanMou(IsYangForm);

        if (Creature.CombatState is not { } combatState)
            return;

        foreach (var player in combatState.Players)
        {
            if (yang)
                await PowerCmd.Apply<QuanMouYangPower>(choiceContext, player.Creature, 3, Creature, null);
            else
                await PowerCmd.Apply<QuanMouYinPower>(choiceContext, player.Creature, 3, Creature, null);
        }
    }

    /// <summary>同步阳/阴 Spine 节点的可见性；旧 PCK 自动回退到序列帧。</summary>
    public void RefreshFormVisuals()
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        Node2D? visuals = creatureNode?.GetSpecialNode<Node2D>("Visuals");
        if (visuals != null && MouSimaYiSpineVisuals.EnsureAttached(visuals))
        {
            MouSimaYiSpineVisuals.SetFormVisibility(visuals, IsYangForm);
            return;
        }

        AnimatedSprite2D? yang = creatureNode?.GetSpecialNode<AnimatedSprite2D>("Visuals/YangVisual");
        AnimatedSprite2D? yin = creatureNode?.GetSpecialNode<AnimatedSprite2D>("Visuals/YinVisual");
        SetFormVisualVisibility(yang, yin, IsYangForm);
    }

    /// <summary>播放阴阳过渡，并在动画中段切换本体立绘。</summary>
    private async Task PlayFormTransitionVisual(bool previousYang, bool targetYang)
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        Node2D? visuals = creatureNode?.GetSpecialNode<Node2D>("Visuals");
        if (visuals != null
            && MouSimaYiSpineVisuals.EnsureAttached(visuals)
            && MouSimaYiSpineVisuals.GetTransition(visuals) is { } spineTransition)
        {
            await PlaySpineTransitionVisual(visuals, spineTransition, previousYang, targetYang);
            return;
        }

        AnimatedSprite2D? yang = creatureNode?.GetSpecialNode<AnimatedSprite2D>("Visuals/YangVisual");
        AnimatedSprite2D? yin = creatureNode?.GetSpecialNode<AnimatedSprite2D>("Visuals/YinVisual");
        AnimatedSprite2D? transition = creatureNode?.GetSpecialNode<AnimatedSprite2D>(
            "Visuals/TransitionVisual");
        if (yang is null || yin is null || transition is null)
        {
            RefreshFormVisuals();
            return;
        }

        SetFormVisualVisibility(yang, yin, previousYang);
        transition.Stop();
        transition.Frame = 0;
        transition.Visible = true;
        transition.Play("play");

        try
        {
            int frameCount = transition.SpriteFrames?.GetFrameCount("play") ?? 0;
            int switchFrame = Math.Max(1, frameCount / 2);
            while (GodotObject.IsInstanceValid(transition)
                   && transition.IsPlaying()
                   && transition.Frame < switchFrame)
            {
                await transition.ToSignal(transition, AnimatedSprite2D.SignalName.FrameChanged);
            }

            if (GodotObject.IsInstanceValid(yang) && GodotObject.IsInstanceValid(yin))
                SetFormVisualVisibility(yang, yin, targetYang);

            if (GodotObject.IsInstanceValid(transition) && transition.IsPlaying())
                await transition.ToSignal(transition, AnimatedSprite2D.SignalName.AnimationFinished);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(transition))
                transition.Visible = false;
            if (GodotObject.IsInstanceValid(yang) && GodotObject.IsInstanceValid(yin))
                SetFormVisualVisibility(yang, yin, targetYang);
        }
    }

    private static void SetFormVisualVisibility(
        AnimatedSprite2D? yang,
        AnimatedSprite2D? yin,
        bool showYang)
    {
        if (yang != null)
            yang.Visible = showYang;
        if (yin != null)
            yin.Visible = !showYang;
    }

    private void TryEnsureSpineVisuals()
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        Node2D? visuals = creatureNode?.GetSpecialNode<Node2D>("Visuals");
        if (visuals != null)
            MouSimaYiSpineVisuals.EnsureAttached(visuals);
    }

    private static async Task PlaySpineTransitionVisual(
        Node2D visuals,
        CanvasItem transition,
        bool previousYang,
        bool targetYang)
    {
        MouSimaYiSpineVisuals.SetFormVisibility(visuals, previousYang);
        transition.Visible = true;
        MouSimaYiSpineVisuals.Play(transition, "play", false);

        try
        {
            await transition.ToSignal(
                transition.GetTree().CreateTimer(0.68),
                SceneTreeTimer.SignalName.Timeout);
            MouSimaYiSpineVisuals.SetFormVisibility(visuals, targetYang);
            await transition.ToSignal(
                transition.GetTree().CreateTimer(0.68),
                SceneTreeTimer.SignalName.Timeout);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(transition))
                transition.Visible = false;
            if (GodotObject.IsInstanceValid(visuals))
                MouSimaYiSpineVisuals.SetFormVisibility(visuals, targetYang);
        }
    }

    /// <summary>供平辽按下一行动判断攻击、防御与成长分支。</summary>
    public static MoveCategory GetMoveCategory(string moveId) => moveId switch
    {
        HeavyMoveId or MultiMoveId => MoveCategory.Attack,
        GrowthMoveId => MoveCategory.Growth,
        DefenseMoveId => MoveCategory.Defense,
        AttackDefenseMoveId => MoveCategory.Attack | MoveCategory.Defense,
        AttackGrowthMoveId => MoveCategory.Attack | MoveCategory.Growth,
        DefenseGrowthMoveId => MoveCategory.Defense | MoveCategory.Growth,
        _ => MoveCategory.None
    };

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var heavy = new MoveState(
            HeavyMoveId,
            _ => ExecuteMove(MoveCategory.Attack, () => Attack(45)),
            new HiddenIntent());
        var multi = new MoveState(
            MultiMoveId,
            _ => ExecuteMove(MoveCategory.Attack, () => Attack(7, 4)),
            new HiddenIntent());
        var growth = new MoveState(
            GrowthMoveId,
            _ => ExecuteMove(MoveCategory.Growth, () => GainStrength(3)),
            new HiddenIntent());
        var defense = new MoveState(
            DefenseMoveId,
            _ => ExecuteMove(MoveCategory.Defense, () => GainBlock(40)),
            new HiddenIntent());
        var attackDefense = new MoveState(
            AttackDefenseMoveId,
            targets => ExecuteMove(
                MoveCategory.Attack | MoveCategory.Defense,
                () => AttackAndDefense(targets)),
            new HiddenIntent());
        var attackGrowth = new MoveState(
            AttackGrowthMoveId,
            targets => ExecuteMove(
                MoveCategory.Attack | MoveCategory.Growth,
                () => AttackAndGrowth(targets)),
            new HiddenIntent());
        var defenseGrowth = new MoveState(
            DefenseGrowthMoveId,
            targets => ExecuteMove(
                MoveCategory.Defense | MoveCategory.Growth,
                () => DefenseAndGrowth(targets)),
            new HiddenIntent());

        var random = new RandomBranchState("RANDOM_MOVE");
        MoveState[] moves = [heavy, multi, growth, defense, attackDefense, attackGrowth, defenseGrowth];
        foreach (MoveState move in moves)
        {
            random.AddBranch(move, MoveRepeatType.CannotRepeat, 1f);
            move.FollowUpState = random;
        }

        return new MonsterMoveStateMachine([.. moves, random], random);
    }

    private async Task ExecuteMove(MoveCategory category, Func<Task> action)
    {
        bool attack = category.HasFlag(MoveCategory.Attack);
        Task visualTask = MouSimaYiSpineVisuals.PlayActionOverlay(IsYangForm, attack);
        await action();
        await visualTask;
    }

    private Task Attack(int damage, int hitCount = 1)
    {
        // 每个攻击行动只播放一次当前形态的平辽语音，多段伤害不会重复触发。
        MouSimaYiSfxPlayer.PlayPingLiao(IsYangForm);
        return DamageCmd
            .Attack(damage)
            .FromMonster(this)
            .WithNoAttackerAnim()
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private Task GainBlock(int amount) =>
        CreatureCmd.GainBlock(Creature, amount, ValueProp.Move, null);

    private Task GainStrength(int amount) => PowerCmd.Apply<StrengthPower>(
        new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);

    private async Task AttackAndDefense(IReadOnlyList<Creature> _)
    {
        await Attack(25);
        await GainBlock(30);
    }

    private async Task AttackAndGrowth(IReadOnlyList<Creature> _)
    {
        await Attack(4, 5);
        await GainStrength(2);
    }

    private async Task DefenseAndGrowth(IReadOnlyList<Creature> _)
    {
        await GainBlock(35);
        await GainStrength(2);
    }
}
