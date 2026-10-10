using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// 斩决-免费攻击：下一张攻击牌费用为0（一次性）
/// </summary>
public sealed class ZhanJueFreeAttackPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // buff 图标：复用用户提供的斩决buff.png（256x150）
    public override string CustomIconPath => "res://Yjcm/Images/Powers/zhan_jue_buff.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/zhan_jue_buff.png";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != base.Owner) return false;
        if (card.Type != CardType.Attack) return false;
        var inHandOrPlay = card.Pile?.Type is PileType.Hand or PileType.Play;
        if (!inHandOrPlay) return false;
        modifiedCost = default;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == base.Owner && cardPlay.Card.Type == CardType.Attack)
        {
            var inHandOrPlay = cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play;
            if (inHandOrPlay)
                await PowerCmd.Decrement(this);
        }
    }
}
