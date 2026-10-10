using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 进攻马（-1马）基类：所有进攻马都占据进攻马栏，
/// 且具有「获得力量时额外获得一点」的被动（普通力量 + 临时力量）。
/// </summary>
public abstract class OffensiveHorseOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.OffensiveHorse;

    // 防止 ModifyAmount(+1) 再次触发 AfterPowerAmountChanged 导致无限递归
    private bool _adding;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (_adding) return;
        if (power.Owner != Owner.Creature) return;
        if (amount <= 0) return;
        if (power is not (StrengthPower or TemporaryStrengthPower)) return;

        // 获得力量时额外获得一点
        _adding = true;
        try { await PowerCmd.ModifyAmount(choiceContext, power, 1, Owner.Creature, cardSource); }
        finally { _adding = false; }
    }
}
