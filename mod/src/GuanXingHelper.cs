using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Yjcm;

/// <summary>
/// 观星：观看抽牌堆顶一定数量的牌，将任意张置于抽牌堆底
/// </summary>
public static class GuanXingHelper
{
    public static async Task GuanXing(PlayerChoiceContext choiceContext, Player owner, int amount)
    {
        var drawPile = PileTypeExtensions.GetPile(PileType.Draw, owner);
        if (drawPile?.Cards == null || drawPile.Cards.Count == 0) return;

        // 抽牌堆列表开头 = 牌堆顶（Draw 从 FirstOrDefault 取牌），观星应取顶部 N 张
        var topCards = drawPile.Cards.Take(Math.Min(amount, drawPile.Cards.Count)).ToList();
        if (topCards.Count == 0) return;

        // 让玩家选择要放到抽牌堆底的牌（可以全选或不选）
        var chosen = await CardSelectCmd.FromSimpleGrid(
            choiceContext, topCards, owner,
            new CardSelectorPrefs(
                new LocString("cards", "guanxing.select_prompt"),
                0, topCards.Count));

        // 将选中的牌移到抽牌堆底
        foreach (var card in chosen)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom);
        }

        // 未被选中的牌保持原位置（在抽牌堆顶）
    }
}
