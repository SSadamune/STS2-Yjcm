using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm;

/// <summary>
/// 威张星彩-贤贶：每回合有牌第一次被弃置时，可以废除一个装备栏，
/// 然后摸已废除装备栏数量的牌。
/// 每回合只触发一次（己方回合开始时重置触发标记）。
/// </summary>
[RegisterPower]
public sealed class WeiZhangXingCaiXianKuangPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/wei_zhang_xing_cai_buff.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/wei_zhang_xing_cai_buff.png";

    // 本回合是否已触发过贤贶
    private bool _triggeredThisTurn;

    // 每回合开始时重置触发标记（己方回合）
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side == CombatSide.Player)
            _triggeredThisTurn = false;
    }

    // 有牌被弃置时触发：本回合第一次弃置可废除一个装备栏并摸牌
    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        await base.AfterCardDiscarded(choiceContext, card);
        if (Owner == null || _triggeredThisTurn) return;
        // 多人模式只响应持有者自己弃置的牌，队友弃牌不触发
        if (card.Owner?.Creature != Owner) return;

        var player = Owner.Player;
        if (player == null) return;

        // 无论最终是否废除，本回合的触发机会已用掉
        _triggeredThisTurn = true;

        // 选择要废除的装备栏（仅未废除的栏位）
        var available = EquipCmd.GetAvailableSlots(player);
        if (available.Count == 0) return;
        var chosen = await EquipCmd.ChooseSlot(choiceContext, player, available);
        if (chosen == null) return;

        await EquipCmd.AbolishSlot(choiceContext, player, chosen.Value);

        await PlayerCmd.GainEnergy(1, player);

        // 摸已废除装备栏数量的牌
        var count = EquipCmd.CountAbolishedSlots(player);
        if (count > 0)
            await CardPileCmd.Draw(choiceContext, count, player, false);
    }
}
