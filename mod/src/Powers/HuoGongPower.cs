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
/// 火攻：弃置手牌时，受到层数点伤害。
/// </summary>
public sealed class HuoGongPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/huo_gong.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/huo_gong.png";

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        await base.AfterCardDiscarded(choiceContext, card);
        if (Owner == null || Amount <= 0) return;
        // 火攻是施加到敌人身上的 debuff，触发者是打出火攻的玩家（Applier）。
        // 只有施加者自己弃置的牌触发伤害，队友弃牌不触发（多人模式）。
        if (card.Owner?.Creature != Applier) return;

        var dmg = (decimal)Amount;
        // 狂风联动：伤害翻倍，狂风减少一层
        var kuangFeng = Owner.Powers?.OfType<KuangFengPower>().FirstOrDefault();
        if (kuangFeng != null && kuangFeng.Amount > 0)
        {
            dmg *= 2;
            await PowerCmd.Decrement(kuangFeng);
        }

        // 将火攻伤害标记为有力量的攻击，使铁索能够接收并传导；传导伤害仍保持无力量，避免递归触发。
        await CreatureCmd.Damage(choiceContext, Owner, dmg,
            ValueProp.Unblockable | ValueProp.Move, null, null);
        await PowerCmd.Decrement(this);
    }
}
