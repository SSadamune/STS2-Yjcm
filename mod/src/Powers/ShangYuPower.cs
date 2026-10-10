using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Yjcm;

/// <summary>
/// 赏誉：回合开始时将带有赏誉关键词的牌从抽牌堆/弃牌堆加入手牌。
/// 战斗开始时自动赋予玩家，不可见。
/// </summary>
[RegisterPower]
public sealed class ShangYuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override bool ShouldPlayVfx => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature != Owner) return;

        var drawPile = PileTypeExtensions.GetPile(PileType.Draw, player);
        var discardPile = PileTypeExtensions.GetPile(PileType.Discard, player);

        var shangYuCards = new List<CardModel>();
        if (drawPile?.Cards != null)
            shangYuCards.AddRange(drawPile.Cards.Where(c => c.Keywords.Contains(YjcmKeywords.ShangYu)));
        if (discardPile?.Cards != null)
            shangYuCards.AddRange(discardPile.Cards.Where(c => c.Keywords.Contains(YjcmKeywords.ShangYu)));

        if (shangYuCards.Count == 0) return;

        var sorted = shangYuCards.OrderBy(c => c.Id.Entry).ToList();
        foreach (var card in sorted)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
    }
}
