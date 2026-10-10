using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 悉性：下一张牌造成伤害大于本次时，将悉性返回手牌；否则失去效果。
/// </summary>
[RegisterPower]
public sealed class XiXingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/xi_xing.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/xi_xing.png";

    /// <summary>需要返回手牌的悉性卡牌实例</summary>
    public CardModel? SourceCard { get; set; }
    /// <summary>本次造成的伤害值，下张牌需超过此值才能回手</summary>
    public decimal ThresholdDamage { get; set; }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        // 只结算持有者自己造成的伤害，队友的伤害不算
        if (dealer != Owner) return;
        if (SourceCard == null || cardSource == SourceCard) return;
        var card = SourceCard;
        SourceCard = null;

        // 伤害大于阈值才回手
        if (result.TotalDamage > ThresholdDamage)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);

        if (Owner != null)
            await PowerCmd.Remove(this);
    }

}
