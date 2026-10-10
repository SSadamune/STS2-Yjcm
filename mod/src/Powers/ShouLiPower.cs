using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 狩骊：受到的伤害变为2倍。通过 BeforeSideTurnEnd 手动移除。
/// </summary>
[RegisterPower(Inherit = true)]
public abstract class ShouLiPowerBase : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/shou_li.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/shou_li.png";

    /// <summary>受到伤害翻倍</summary>
    public override decimal ModifyDamageMultiplicative(
#if GAME_BETA
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (target == Owner && props.IsPoweredAttack())
            return 2m;
        return 1m;
    }
}

/// <summary>敌人身上的狩骊：敌人回合结束时移除</summary>
public sealed class EnemyShouLiPower : ShouLiPowerBase
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (Owner != null && side == CombatSide.Enemy && participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}

/// <summary>玩家身上的狩骊：敌人回合结束时移除（下回合开始前消失）</summary>
public sealed class PlayerShouLiPower : ShouLiPowerBase
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (Owner != null && side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
