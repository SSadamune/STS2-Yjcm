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
/// 防御马（+1马）基类：所有防御马都占据防御马栏，
/// 且具有「获得敏捷时额外获得一点」的被动（普通敏捷 + 临时敏捷）。
/// </summary>
public abstract class DefensiveHorseOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.DefensiveHorse;

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
        if (power is not (DexterityPower or TemporaryDexterityPower)) return;

        // 获得敏捷时额外获得一点
        _adding = true;
        try { await PowerCmd.ModifyAmount(choiceContext, power, 1, Owner.Creature, cardSource); }
        finally { _adding = false; }
    }
}
