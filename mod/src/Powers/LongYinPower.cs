using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm;

/// <summary>
/// 龙吟：下{Amount}张攻击牌免费打出。
/// </summary>
[RegisterPower]
public sealed class LongYinPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/long_yin.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/long_yin.png";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        // 只对持有者（武诸葛）自己打出的攻击牌免费，队友的攻击牌不受影响
        if (card.Owner?.Creature != Owner)
        {
            modifiedCost = originalCost;
            return false;
        }
        if (card.Type == CardType.Attack)
        {
            modifiedCost = 0;
            return true;
        }
        modifiedCost = originalCost;
        return false;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);
        // 只消耗持有者自己打出的攻击牌次数，队友打出的不算
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Attack && Owner != null)
            await PowerCmd.Decrement(this);
    }
}
