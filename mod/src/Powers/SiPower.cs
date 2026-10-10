using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

#nullable enable

namespace Yjcm;

/// <summary>
/// 「死」Debuff：当层数大于场上存活单位总数时，回合结束直接死亡
/// </summary>
public sealed class SiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/si.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/si.png";

    public static readonly string DeathVoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(SiPower).Assembly.Location)!,
        "Voice", "Bank", "Assets", "si_trigger.wav");

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (base.Owner == null || base.Amount <= 0) return;

        var combatState = base.Owner.CombatState;
        if (combatState == null) return;

        // 存活单位 = 存活的玩家 + 存活的敌人
        var aliveCount = combatState.Players.Count(p => !p.Creature.IsDead)
                       + combatState.HittableEnemies.Count(e => !e.IsDead);

        if (base.Amount > aliveCount)
        {
            VoicePlayer.PlayWav(DeathVoicePath);
            await CreatureCmd.Kill(base.Owner, force: true);
        }
    }
}
