using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Content;

namespace Yjcm;

/// <summary>
/// 只在原生按玩家抽取新奖励/商店遗物时叠加候选过滤。
/// 不改全局池、持有列表、初始遗物或显式指定的事件/先古奖励。
/// </summary>
[HarmonyPatch]
public static class SharedRelicScopePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        Type[] args = [typeof(Player), typeof(RelicRarity), typeof(Func<RelicModel, bool>)];
        yield return AccessTools.Method(typeof(RelicFactory), nameof(RelicFactory.PullNextRelicFromFront), args);
        yield return AccessTools.Method(typeof(RelicFactory), nameof(RelicFactory.PullNextRelicFromBack), args);
    }

    private static void Prefix(Player player, ref Func<RelicModel, bool> filter)
    {
        if (WuZhugeliangSettings.MatchesCharacterScope(
                WuZhugeliangSettings.SharedRelicScope, player.Character.GetType()))
            return;

        // 使用 RitsuLib 官方注册表限定本 Mod 的共享池内容，不猜测 ID 或影响其他 Mod。
        // 每次抽取取一次快照，避免提前缓存空池；同次候选检查不重复查询注册表。
        var sharedRelicTypes = ModContentRegistry
            .GetRegisteredModelsInPool(MainFile.ModId, typeof(SharedRelicPool))
            .ToHashSet();
        filter = FilterCandidates(filter, sharedRelicTypes);
    }

    internal static Func<RelicModel, bool> FilterCandidates(
        Func<RelicModel, bool> originalFilter, IReadOnlySet<Type> sharedRelicTypes) =>
        relic => originalFilter(relic) &&
            !(sharedRelicTypes.Contains(relic.GetType()) &&
              relic.Rarity is RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare or RelicRarity.Shop);
}
