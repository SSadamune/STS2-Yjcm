using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 绝伐：失去或获得星时，对随机敌人造成伤害。
/// </summary>
[RegisterPower]
public sealed class JueFaPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/jue_fa.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/jue_fa.png";

    /// <summary>每次触发造成的伤害值</summary>
    public int DamagePerTrigger { get; set; } = 4;

    /// <summary>星获得或消耗后触发；使用战斗 RNG 与当前选择上下文保证多人一致。</summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power is not XingPower || power.Owner != Owner || amount == 0)
            return;

        var enemies = CombatState?.HittableEnemies.ToList();
        if (enemies is not { Count: > 0 })
            return;

        var target = Owner.Player!.RunState.Rng.CombatCardSelection.NextItem(enemies);
        await CreatureCmd.Damage(choiceContext, target, DamagePerTrigger,
            ValueProp.Move, null, null);
    }
}
