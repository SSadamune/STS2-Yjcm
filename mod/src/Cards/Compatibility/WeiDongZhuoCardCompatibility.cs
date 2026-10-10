using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

// The host's existing assembly PatchAll discovery finds these classes automatically.
// Every postfix adds clone-only behavior; original cards keep their original result.
[HarmonyPatch(typeof(ModCardTemplate), "get_ExtraHoverTips")]
public static class WeiDongZhuoWuJiangHoverTipsPatch
{
    public static void Postfix(ModCardTemplate __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is not WeiDongZhuoCard card || !card.TryGetWuJiangHoverTip(out var tip)) return;
        __result = __result.Concat(new[] { tip }).ToList();
    }
}

// Keep the existing tracker shared, so either WuSeSha type increments the same combat count.
// The original private method synchronizes original ShenCai instances; append only our copies.
[HarmonyPatch(typeof(ShenCaiJudgmentPower), "SyncCardVariables")]
public static class WeiDongZhuoShenCaiVariableSyncPatch
{
    private static readonly PileType[] CombatPiles =
        [PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play];

    public static void Postfix(Player player, int count)
    {
        foreach (var pileType in CombatPiles)
        {
            var pile = PileTypeExtensions.GetPile(pileType, player);
            if (pile?.Cards == null) continue;
            foreach (var card in pile.Cards.OfType<WeiDongZhuoShenZhangFeiShenCai>())
                card.SetJudgmentCount(count);
        }
    }
}

// The original `is Sha` branch cannot match a sealed, independent Sha copy.
// Reproduce precisely its owner/powered-attack predicate for this one new type only.
[HarmonyPatch(typeof(ZhangLuBuDaoFan), nameof(ZhangLuBuDaoFan.ModifyDamageAdditive))]
public static class WeiDongZhuoShaBuDaoFanPatch
{
    public static void Postfix(ZhangLuBuDaoFan __instance, ValueProp props,
        Creature? dealer, CardModel? cardSource, ref decimal __result)
    {
        if (cardSource is not WeiDongZhuoSha || !props.IsPoweredAttack()) return;
        var owner = __instance.Owner;
        if (owner == null || (dealer != owner.Creature && cardSource.Owner != owner)) return;
        __result += owner.Deck.Cards.Count(card => card.Tags.Contains(YjcmCardTags.WuJiang));
    }
}
