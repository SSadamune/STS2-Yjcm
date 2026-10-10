using STS2RitsuLib.Interop.AutoRegistration;
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
/// 狂风：Debuff。受到伤害时追加层数等量伤害，然后减少一层。
/// 若同时拥有火攻，火攻触发时伤害翻倍且狂风也减少一层。
/// </summary>
[RegisterPower]
public sealed class KuangFengPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/kuang_feng.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/kuang_feng.png";

    /// <summary>受到伤害时追加狂风层数的伤害</summary>
#if GAME_BETA
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (target == Owner && Amount > 0)
            return Amount;
        return 0;
    }

    /// <summary>受到伤害后减少一层</summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target == Owner && Amount > 0)
            await PowerCmd.Decrement(this);
    }
}
