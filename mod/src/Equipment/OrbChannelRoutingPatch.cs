using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Yjcm;

/// <summary>
/// 充能球路由补丁：对武诸葛，任何**非装备**充能球的 Channel 调用都重定向到宝物栏。
///
/// 原版 <see cref="OrbCmd.Channel"/> 是 FIFO 入队、满时激发最旧球；
/// 武诸葛的宝物栏是收容栏位——非装备球（闪电、冰霜等）统一进入宝物栏并同类替换。
/// 装备球（<see cref="EquipmentOrbModel"/>）不受影响，走 <see cref="EquipCmd.ChannelEquipment"/>。
///
/// 注意：Harmony 此版本不支持异步跳过前缀（Task&lt;bool&gt;），
/// 因此这里用**同步** bool 前缀，路由逻辑见 <see cref="EquipCmd.RouteToTreasure"/>。
/// 补丁由 MainFile 中的 <c>harmony.PatchAll()</c> 自动应用。
/// </summary>
[HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.Channel),
    new[] { typeof(PlayerChoiceContext), typeof(OrbModel), typeof(Player) })]
public static class OrbChannelTreasureRoutingPatch
{
    public static bool Prefix(
        PlayerChoiceContext choiceContext, OrbModel orb, Player player, ref Task __result)
    {
        // 仅武诸葛需要宝物栏收容路由，其他角色走原版逻辑
        if (player.Character is not WuZhugeliang)
        {
            return true;
        }

        // 装备球不拦截（由 EquipCmd.ChannelEquipment 处理同类替换）
        if (orb is EquipmentOrbModel)
        {
            return true;
        }

        // 非装备球 → 宝物栏（同步路由）
        EquipCmd.RouteToTreasure(player, orb);
        // 被 Harmony 跳过的方法返回 Task；必须显式提供已完成任务，
        // 否则泛型 Channel<T> 会 await null 并抛出 NullReferenceException。
        __result = Task.CompletedTask;
        return false; // 跳过原版 Channel
    }
}
