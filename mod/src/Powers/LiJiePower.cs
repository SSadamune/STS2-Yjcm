using System.Collections.Generic;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 力竭（统一"回合内失去力量"buff）：施加负力量，回合结束时恢复。
/// 行为与游戏原版 ManglePower（TemporaryStrengthPower）一致，仅更换图标与本地化。
/// 由谋姜维-绝谋-阴等"本回合失去力量"效果施加。
/// </summary>
[RegisterPower]
public sealed class LiJiePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>图标：力竭（回合内失去力量）</summary>
    public override string CustomIconPath => "res://Yjcm/Images/Powers/li_jie.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/li_jie.png";

    /// <summary>施加时：直接给持有者减等量力量（临时）</summary>
    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), target, -amount, applier, cardSource, silent: true);
    }

    /// <summary>层数变化时：同步增减临时力量</summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || amount == Amount) return;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -amount, applier, cardSource, silent: true);
    }

    /// <summary>回合结束：移除自身并把力量加回来（恢复）</summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
