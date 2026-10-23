using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;

#nullable enable

namespace Yjcm.Interop;

/// <summary>
/// 联动提示分三种，互不叠加。
/// A. 不显示任何联动信息。
/// B. 只显示「※模组联动」悬停提示，不改卡面。
/// C. 只把卡面上的火攻关键词换成紫色灼烧，不显示「※模组联动」悬停。
/// </summary>
internal static class SunqianLinkHover
{
    public const string TitleKey = "sunqian_burning_link.hoverTitle";
    public const string BodyKey = "sunqian_burning_link.libraryHover";

    /// <summary>情况 B。</summary>
    public static bool ShouldShow() => CurrentMode() == 1;

    public static bool ShowsBurningKeyword() => CurrentMode() == 2;

    // 0: 原效果，1: 仅联动提示，2: 仅灼烧关键词。独立决策便于覆盖联机场景。
    internal static int SelectMode(bool ready, bool inRun, bool inCombat, bool partyHasActor, bool localIsActor)
    {
        if (!ready) return 0;
        if (!inRun) return 1;
        if (!partyHasActor) return 0;
        return inCombat && localIsActor ? 2 : 1;
    }

    private static int CurrentMode()
    {
        if (!SunqianUniverseInterop.IsReady)
            return SelectMode(false, false, false, false, false);

        // 激活孙乾宇宙，且不在游戏中。
        if (RunManager.Instance is not { IsInProgress: true })
            return SelectMode(true, false, false, false, false);

        RunState? run = RunManager.Instance.DebugOnlyGetState();
        if (run == null || !PartyHasSunqian(run))
            return SelectMode(true, true, false, false, false);

        // 在游戏中、不在战斗中，且队伍里有孙乾宇宙角色。
        if (CombatManager.Instance is not { IsInProgress: true })
            return SelectMode(true, true, false, true, false);

        // 在战斗中，队伍里有孙乾宇宙角色，且当前控制的不是孙乾宇宙角色。
        return SelectMode(true, true, true, true, LocalPlayerIsSunqian(run));
    }

    private static bool PartyHasSunqian(RunState run) =>
        run.Players.Any(player =>
            SunqianUniverseInterop.IsSupportingActorCharacter(player.Character));

    private static bool LocalPlayerIsSunqian(RunState run) =>
        run.Players.Any(player =>
            LocalContext.IsMe(player) &&
            SunqianUniverseInterop.IsSupportingActorCharacter(player.Character));
}
