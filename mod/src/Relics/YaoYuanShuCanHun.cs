using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 先古之民遗物「爻袁术残魂」：Ancient 稀有度。
/// 第一个回合开始时消耗所有手牌，将等量的背置牌加入手牌；
/// 之后每使用两张牌，可以消耗一张手牌，将一张背置牌加入手牌。
/// </summary>
[RegisterRelic(typeof(SharedRelicPool))]
public sealed class YaoYuanShuCanHun : ModRelicTemplate
{
    /// <summary>累计使用牌数（每两张触发一次）</summary>
    private int _playsCount;
    /// <summary>首回合触发标记（只触发一次）</summary>
    private bool _firstTurnHandled;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override string PackedIconPath =>
        "res://Yjcm/Images/Relics/yao_yuan_shu_can_hun.png";
    protected override string PackedIconOutlinePath =>
        "res://Yjcm/Images/Relics/yao_yuan_shu_can_hun.png";
    protected override string BigIconPath =>
        "res://Yjcm/Images/Relics/Big/yao_yuan_shu_can_hun.png";

    public override Task BeforeCombatStart()
    {
        _playsCount = 0;
        _firstTurnHandled = false;
        return base.BeforeCombatStart();
    }

    /// <summary>第一个回合开始：消耗所有手牌 → 等量背置牌入手</summary>
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side != CombatSide.Player || Owner == null) return;
        if (_firstTurnHandled) return;
        _firstTurnHandled = true;

        var ctx = new ThrowingPlayerChoiceContext();
        var hand = PileTypeExtensions.GetPile(PileType.Hand, Owner)?.Cards.ToList() ?? [];
        if (hand.Count == 0) return;

        // 消耗所有手牌（副本遍历，避免集合被修改）
        foreach (var c in hand)
            await CardCmd.Exhaust(ctx, c, causedByEthereal: false, skipVisuals: false);

        // 等量背置牌加入手牌
        for (var i = 0; i < hand.Count; i++)
        {
            var beizhi = Owner.Creature.CombatState.CreateCard<BeiZhiCard>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(beizhi, PileType.Hand, Owner, CardPilePosition.Random);
        }
    }

    /// <summary>每使用两张牌：可消耗一张手牌（玩家选）→ 一张背置牌入手</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (Owner == null) return;
        var card = cardPlay.Card;
        if (card?.Owner != Owner) return; // 多人：只数自己打出的牌

        _playsCount++;
        if (_playsCount % 2 != 0) return;

        // 可选消耗一张手牌（0~1 张；不选则本次不换牌）
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(
                new MegaCrit.Sts2.Core.Localization.LocString("cards", "yao_yuan_shu.select_prompt"),
                0, 1),
            null, this);
        var toExhaust = selected.FirstOrDefault();
        if (toExhaust == null) return;

        await CardCmd.Exhaust(choiceContext, toExhaust, causedByEthereal: false, skipVisuals: false);
        var beizhi = Owner.Creature.CombatState.CreateCard<BeiZhiCard>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(beizhi, PileType.Hand, Owner, CardPilePosition.Random);
    }
}
