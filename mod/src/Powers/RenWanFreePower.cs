using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm;

/// <summary>
/// 任婉残魂效果：下一张牌免费打出（本回合有效）。
/// 任婉残魂在回合开始时施加，牌被打出时消耗，回合结束时兜底移除。
/// </summary>
[RegisterPower]
public sealed class RenWanFreePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/free_play.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/free_play.png";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        // 只对持有者（武诸葛）自己打出的任意类型牌免费，队友的牌不受影响
        if (card.Owner?.Creature != Owner)
        {
            modifiedCost = originalCost;
            return false;
        }
        modifiedCost = 0;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);
        // 只消耗持有者自己打出的牌次数，队友打出的不算
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (Owner != null)
            await PowerCmd.Decrement(this);
    }

    /// <summary>回合结束时兜底移除：本回合没用掉则下回合不残留</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Player && Owner != null)
            await PowerCmd.Remove(this);
    }
}
