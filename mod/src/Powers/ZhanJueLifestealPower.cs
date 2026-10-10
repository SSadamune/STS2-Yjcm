using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 斩决-嗜血：下一张攻击牌造成伤害后恢复等量体力（一次性）
/// </summary>
public sealed class ZhanJueLifestealPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // buff 图标：复用用户提供的斩决buff.png（256x150）
    public override string CustomIconPath => "res://Yjcm/Images/Powers/zhan_jue_buff.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/zhan_jue_buff.png";

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != base.Owner) return;
        if (!props.IsPoweredAttack()) return;
        if (result.TotalDamage <= 0) return;

        var healAmt = (int)result.TotalDamage;
        await CreatureCmd.Heal(base.Owner, healAmt);
        await PowerCmd.Decrement(this);
    }
}
