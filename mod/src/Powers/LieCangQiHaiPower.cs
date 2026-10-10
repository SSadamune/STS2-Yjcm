using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 气海：下一次获得的格挡值降为0（一次性debuff）
/// </summary>
public sealed class LieCangQiHaiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/qi_hai.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/qi_hai.png";

    public override decimal ModifyBlockMultiplicative(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != base.Owner) return 1m;
        return 0m;
    }

    public override async Task AfterBlockGained(
        Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == base.Owner && amount > 0)
            await PowerCmd.Decrement(this);
    }
}
