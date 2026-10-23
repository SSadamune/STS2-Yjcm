using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

// Only replace the native multiplier for the power holder. Native debuff application,
// stacking, immunity, removal and turn-end duration ticking remain untouched.
[HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
internal static class ShuiHuoJianRongWeakPatch
{
    internal static void Postfix(WeakPower __instance, Creature? dealer, ValueProp props, ref decimal __result)
    {
        if (dealer == __instance.Owner && props.IsPoweredAttack() &&
            __instance.Owner?.HasPower<ShuiHuoJianRongPower>() == true)
            __result = 1.25m;
    }
}

[HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
internal static class ShuiHuoJianRongVulnerablePatch
{
    internal static void Postfix(VulnerablePower __instance, Creature? target, ValueProp props, ref decimal __result)
    {
        if (target == __instance.Owner && props.IsPoweredAttack() &&
            __instance.Owner?.HasPower<ShuiHuoJianRongPower>() == true)
            __result = 0.5m;
    }
}

[HarmonyPatch(typeof(FrailPower), nameof(FrailPower.ModifyBlockMultiplicative))]
internal static class ShuiHuoJianRongFrailPatch
{
    internal static void Postfix(FrailPower __instance, Creature target, CardModel? cardSource,
        ValueProp props, ref decimal __result)
    {
        if (target == __instance.Owner && cardSource != null && props.IsPoweredCardOrMonsterMoveBlock() &&
            __instance.Owner?.HasPower<ShuiHuoJianRongPower>() == true)
            __result = 1.25m;
    }
}
