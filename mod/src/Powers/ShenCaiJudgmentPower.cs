using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
#if GAME_BETA
#else
using MegaCrit.Sts2.Core.Entities.Powers;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
#if GAME_BETA
using MegaCrit.Sts2.Core.Entities.Powers;
#else
#endif
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 神裁的战斗内总次数。初始1次，每打出一张无色杀增加1次，最多5次。
/// </summary>
[RegisterPower]
public sealed class ShenCaiJudgmentPower : ModPowerTemplate
{
    public const int DefaultJudgments = 1;
    public const int MaxJudgments = 5;
    private static readonly PileType[] CombatPiles =
        [PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public int JudgmentCount => Math.Clamp(Amount, DefaultJudgments, MaxJudgments);

    public static int GetJudgmentCount(Player player)
    {
        var tracker = player.Creature.Powers?
            .OfType<ShenCaiJudgmentPower>()
            .FirstOrDefault();
        return tracker?.JudgmentCount ?? DefaultJudgments;
    }

    public static async Task IncrementJudgments(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel source)
    {
        var tracker = player.Creature.Powers?
            .OfType<ShenCaiJudgmentPower>()
            .FirstOrDefault();
        int current = tracker?.JudgmentCount ?? DefaultJudgments;
        int next = Math.Min(MaxJudgments, current + 1);

        SyncCardVariables(player, next);
        if (next == current)
            return;

        int amountToApply = tracker == null ? next : next - current;
        await PowerCmd.Apply<ShenCaiJudgmentPower>(
            choiceContext,
            player.Creature,
            amountToApply,
            player.Creature,
            source,
            true);
    }

    private static void SyncCardVariables(Player player, int count)
    {
        foreach (var pileType in CombatPiles)
        {
            var pile = PileTypeExtensions.GetPile(pileType, player);
            if (pile?.Cards == null)
                continue;

            foreach (var shenCai in pile.Cards.OfType<ShenZhangFeiShenCai>())
                shenCai.SetJudgmentCount(count);
        }
    }
}
