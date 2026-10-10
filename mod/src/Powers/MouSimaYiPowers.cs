using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>权谋：负责每回合的阴阳转换。</summary>
[RegisterPower]
public sealed class QuanMouPower : ModPowerTemplate
{
    private bool _hasSeenFirstPlayerTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    // TODO：用户制作专属图标后替换临时图标。
    public override string CustomIconPath =>
        "res://Yjcm/Images/Powers/mou_sima_yi_quan_mou.png";
    public override string CustomBigIconPath => CustomIconPath;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
        if (side != CombatSide.Player || Owner?.Monster is not MouSimaYi boss || !Owner.IsAlive)
            return;

        // 开局已经进入阳形态，首个玩家回合不切换。
        if (!_hasSeenFirstPlayerTurn)
        {
            _hasSeenFirstPlayerTurn = true;
            boss.RefreshFormVisuals();
            return;
        }

        await boss.EnterForm(choiceContext, !boss.IsYangForm);
    }
}

/// <summary>权谋·阳：玩家每次获得格挡时，谋司马懿获得固定数值格挡。</summary>
[RegisterPower]
public sealed class QuanMouYangPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // TODO：用户制作专属图标后替换临时图标。
    public override string CustomIconPath =>
        "res://Yjcm/Images/Powers/mou_sima_yi_quan_mou_yang.png";
    public override string CustomBigIconPath => CustomIconPath;

    public override async Task AfterBlockGained(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        await base.AfterBlockGained(creature, amount, props, cardSource);
        if (creature != Owner || amount <= 0 || Owner?.CombatState is not { } combatState)
            return;

        foreach (Creature enemy in combatState.Enemies)
        {
            if (enemy.IsAlive && enemy.Monster is MouSimaYi)
                await CreatureCmd.GainBlock(enemy, Amount, ValueProp.Unpowered, null);
        }
    }
}

/// <summary>权谋·阴：谋司马懿的每段攻击对持有者追加层数等量伤害。</summary>
[RegisterPower]
public sealed class QuanMouYinPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // TODO：用户制作专属图标后替换临时图标。
    public override string CustomIconPath =>
        "res://Yjcm/Images/Powers/mou_sima_yi_quan_mou_yin.png";
    public override string CustomBigIconPath => CustomIconPath;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
#if GAME_BETA
        CardModel? cardSource,
        CardPlay? cardPlay)
#else
        CardModel? cardSource)
#endif
    {
        if (target == Owner && dealer?.Monster is MouSimaYi && props.IsPoweredAttack())
            return Amount;
        return 0;
    }
}

/// <summary>平辽：隐藏意图，并记录玩家针对下一行动打出的对应牌。</summary>
[RegisterPower]
public sealed class PingLiaoPower : ModPowerTemplate
{
    private sealed class PendingEffects
    {
        public int Strength;
        public int Buffer;
        public Dictionary<ulong, int> PlayerDebuffs { get; } = [];
    }

    protected override object InitInternalData() => new PendingEffects();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    // TODO：用户制作专属图标后替换临时图标。
    public override string CustomIconPath =>
        "res://Yjcm/Images/Powers/mou_sima_yi_ping_liao.png";
    public override string CustomBigIconPath => CustomIconPath;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (Owner?.Monster is not MouSimaYi boss || !Owner.IsAlive)
            return;
        if (cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
            return;

        PendingEffects pending = GetInternalData<PendingEffects>();
        MouSimaYi.MoveCategory category = MouSimaYi.GetMoveCategory(boss.NextMove.Id);
        switch (cardPlay.Card.Type)
        {
            case CardType.Attack when category.HasFlag(MouSimaYi.MoveCategory.Attack):
                pending.Strength++;
                break;
            case CardType.Skill when category.HasFlag(MouSimaYi.MoveCategory.Defense):
                pending.Buffer++;
                break;
            case CardType.Power when category.HasFlag(MouSimaYi.MoveCategory.Growth):
#if GAME_BETA
                ulong playerId = cardPlay.Player.NetId;
#else
                if (cardPlay.Card.Owner is not { } player)
                    break;
                ulong playerId = player.NetId;
#endif
                pending.PlayerDebuffs[playerId] = pending.PlayerDebuffs.GetValueOrDefault(playerId) + 1;
                break;
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
        if (side != CombatSide.Player || Owner?.Monster is not MouSimaYi || !Owner.IsAlive)
            return;

        PendingEffects pending = GetInternalData<PendingEffects>();
        // 力量必须逐层获得，避免与偷取力量的效果发生歧义。
        for (int i = 0; i < pending.Strength; i++)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 1, Owner, null);

        if (pending.Buffer > 0)
            await PowerCmd.Apply<BufferPower>(choiceContext, Owner, pending.Buffer, Owner, null);

        foreach (var player in combatState.Players)
        {
            if (!pending.PlayerDebuffs.TryGetValue(player.NetId, out int amount) || amount <= 0)
                continue;

            await PowerCmd.Apply<WeakPower>(choiceContext, player.Creature, amount, Owner, null);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, player.Creature, amount, Owner, null);
            await PowerCmd.Apply<FrailPower>(choiceContext, player.Creature, amount, Owner, null);
        }

        pending.Strength = 0;
        pending.Buffer = 0;
        pending.PlayerDebuffs.Clear();
    }
}
