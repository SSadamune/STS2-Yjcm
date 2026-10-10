using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 才瑕：造成或受到伤害后，抽3张牌，然后本效果失效直到使用了3张牌。
/// </summary>
public sealed class CaiXiaPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/cai_xia.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/cai_xia.png";

    private bool _charged = true;
    private int _cardsPlayed;

    public static readonly string TriggerVoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(CaiXiaPower).Assembly.Location)!,
        "Voice", "Bank", "Assets", "cai_xia_buff.wav");

    /// <summary>造成伤害时触发：仅限持有者自身造成伤害</summary>
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        if (dealer == Owner)
            await TryTrigger(choiceContext);
    }

    /// <summary>受到伤害时触发：仅限持有者自身受到伤害</summary>
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target == Owner)
            await TryTrigger(choiceContext);
    }

    /// <summary>出牌计数恢复：仅限持有者自己出牌</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (_charged) return;
        // 只统计持有者自己打出的牌
        if (cardPlay.Card?.Owner?.Creature != Owner) return;

        _cardsPlayed++;
        if (_cardsPlayed >= 5)
        {
            _charged = true;
            _cardsPlayed = 0;
        }
    }

    private async Task TryTrigger(PlayerChoiceContext choiceContext)
    {
        if (!_charged || Owner == null) return;
        _charged = false;
        _cardsPlayed = 0;
        VoicePlayer.PlayWav(TriggerVoicePath);
        await CardPileCmd.Draw(choiceContext, 5, Owner.Player);
    }
}
