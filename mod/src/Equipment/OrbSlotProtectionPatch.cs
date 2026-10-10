using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Yjcm;

/// <summary>
/// 武诸葛充能球栏位保护补丁：充能球栏位**始终为 4 个**，不能被任何其他牌影响。
/// 通过拦截 <see cref="OrbCmd.AddSlots"/> 与 <see cref="OrbCmd.RemoveSlots"/>，
/// 让所有增减栏位的效果对武诸葛无效。
/// 补丁由 MainFile 中的 <c>harmony.PatchAll()</c> 自动应用。
/// </summary>
[HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.AddSlots))]
public static class OrbAddSlotsProtectionPatch
{
    // 返回 false 跳过原方法时，Task 返回值不能为 null。
    public static bool Prefix(Player player, ref Task __result)
    {
        if (player.Character is not WuZhugeliang)
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 同上：武诸葛的栏位不允许减少。
/// </summary>
[HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.RemoveSlots))]
public static class OrbRemoveSlotsProtectionPatch
{
    public static bool Prefix(Player player) => player.Character is not WuZhugeliang;
}
