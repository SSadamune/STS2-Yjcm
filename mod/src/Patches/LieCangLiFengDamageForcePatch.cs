using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Yjcm;

/// <summary>
/// 力烽伤害强制补丁：目标带 <see cref="LieCangLiFengPower"/> 时，其本次攻击伤害**强制为 1**。
///
/// 修复原 bug：力烽原本用 <see cref="AbstractModel.ModifyDamageMultiplicative"/> 返回 1/amount，
/// 但乘算阶段是按顺序累乘的，若虚弱(0.75)等其他减伤乘算在力烽之后执行，
/// 最终伤害会被继续乘成 0.75 → 0。本补丁在全部修正结算后直接覆写最终伤害为 1，
/// 顺序无关，同时也让 <c>AfterDamageGiven</c> 收到 TotalDamage=1 从而正确消耗力烽。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class LieCangLiFengDamageForcePatch
{
    public static void Postfix(Creature? dealer, ValueProp props, ModifyDamageHookType modifyDamageHookType, ref decimal __result)
    {
        // 仅在完整结算（Additive+Multiplicative+Cap）时强制，避免影响局部/预览计算
        if (modifyDamageHookType != ModifyDamageHookType.All) return;
        if (dealer == null) return;
        if (!props.IsPoweredAttack()) return;
        if (dealer.Powers?.OfType<LieCangLiFengPower>().Any() != true) return;

        __result = 1m;
    }
}
