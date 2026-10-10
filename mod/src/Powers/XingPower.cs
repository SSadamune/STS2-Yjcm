using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 星：特殊资源，无被动效果。使用星牌时若星不足，改为失去3倍生命值。
/// </summary>
public sealed class XingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/xing.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/xing.png";

    /// <summary>获得星。借东风与绝伐通过原生能力变化钩子响应。</summary>
    public static async Task Gain(
        PlayerChoiceContext choiceContext, Creature owner, int amount,
        Creature applier, CardModel? source)
    {
        if (amount <= 0) return;
        await PowerCmd.Apply<XingPower>(choiceContext, owner, amount, applier, source, false);
    }

    /// <summary>
    /// 尝试消耗星。若足够则扣除并返回true；若不足则失去3倍HP并返回false。
    /// </summary>
    public static async Task<bool> TryConsume(PlayerChoiceContext choiceContext, Creature owner, int amount, CardModel? source)
    {
        var xing = owner.Powers?.OfType<XingPower>().FirstOrDefault();
        if (xing != null && xing.Amount >= amount)
        {
            // 必须通过 PowerCmd 修改，才能触发 AfterPowerAmountChanged；
            // 直接 SetAmount 会绕过借东风减费和绝伐。
            await PowerCmd.ModifyAmount(choiceContext, xing, -amount, owner, source, false);

            return true;
        }

        // 星不足，失去3倍HP
        var hpLoss = amount * 3;
        await CreatureCmd.Damage(choiceContext, owner, hpLoss,
#if GAME_BETA
            ValueProp.Unblockable | ValueProp.Unpowered, source, null);
#else
            ValueProp.Unblockable | ValueProp.Unpowered, source);
#endif
        return false;
    }
}
