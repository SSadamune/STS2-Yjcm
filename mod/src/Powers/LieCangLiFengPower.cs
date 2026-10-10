using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 力烽：下一次造成的伤害固定为1点（一次性debuff）
/// </summary>
public sealed class LieCangLiFengPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/li_feng.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/li_feng.png";

    public override decimal ModifyDamageMultiplicative(
#if GAME_BETA
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (dealer != base.Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return amount > 0 ? 1m / amount : 1m;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        // 目标做出攻击即消耗（无论伤害是否被格挡/为0），避免效果残留
        if (dealer == base.Owner && props.IsPoweredAttack())
            await PowerCmd.Decrement(this);
    }
}
