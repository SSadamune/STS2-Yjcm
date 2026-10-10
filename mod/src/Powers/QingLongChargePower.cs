using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 青龙蓄势：下一张攻击牌费用为0，打出后消耗。
/// </summary>
[RegisterPower]
public sealed class QingLongChargePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://Yjcm/Images/Powers/qing_long_dao.png",
        BigIconPath: "res://Yjcm/Images/Powers/qing_long_dao.png"
    );

    public override int DisplayAmount => 0; // 不显示数字

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner) return false;
        if (card.Type != CardType.Attack) return false;
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play)) return false;
        modifiedCost = 0;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            if (cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play)
                await PowerCmd.Decrement(this);
        }
    }
}
