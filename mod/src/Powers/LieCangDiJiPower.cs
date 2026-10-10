using STS2RitsuLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// 地机：下一次受到的伤害增加100%（一次性debuff）
/// </summary>
public sealed class LieCangDiJiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/di_ji.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/di_ji.png";

    public override decimal ModifyDamageMultiplicative(
#if GAME_BETA
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (target != base.Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return 2m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && result.TotalDamage > 0)
            await PowerCmd.Decrement(this);
    }
}
