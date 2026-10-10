using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// 知机-保留：本回合结束时保留所有手牌，保留关键词在回合结束后移除。
/// </summary>
[RegisterPower]
public sealed class ZhiJiRetainPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/zhi_ji_retain.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/zhi_ji_retain.png";

    private readonly List<CardModel> _retainedCards = [];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        var player = Owner?.Player;
        if (side != CombatSide.Player || player == null) return;

        var handPile = PileTypeExtensions.GetPile(PileType.Hand, player);
        if (handPile?.Cards == null) return;

        _retainedCards.Clear();
        foreach (var card in handPile.Cards)
        {
            if (!card.Keywords.Contains(CardKeyword.Retain))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Retain);
                _retainedCards.Add(card);
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side != CombatSide.Player) return;
        foreach (var card in _retainedCards)
        {
            if (card.Keywords.Contains(CardKeyword.Retain))
                CardCmd.RemoveKeyword(card, CardKeyword.Retain);
        }
        _retainedCards.Clear();
        if (Owner != null)
            await PowerCmd.Remove(this);
    }
}
