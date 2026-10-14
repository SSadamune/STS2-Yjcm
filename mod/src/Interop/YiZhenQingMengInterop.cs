using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm.Interop;

/// <summary>
/// 《一枕清梦》与“孙乾宇宙”的可选联动。龙套演员使用此牌时，牌面与全部结算
/// 统一改为操作“灼烧”；未安装联动模组时保持原“火攻”逻辑。
/// </summary>
internal static class YiZhenQingMengInterop
{
    private const string EffectTextArg = "DreamFireText";
    private const string FireAttackTextKey = "YJCM_CARD_YI_ZHEN_QING_MENG.fireAttackInlineText";
    private const string BurningTextKey = "YJCM_CARD_YI_ZHEN_QING_MENG.burningInlineText";
    private const string HoverTitleKey = "YJCM_CARD_YI_ZHEN_QING_MENG.interopHoverTitle";
    private const string LibraryHoverKey = "YJCM_CARD_YI_ZHEN_QING_MENG.interopLibraryHover";

    public static bool IsModReady => SunqianUniverseInterop.IsReady;

    public static bool Affects(Player? player) =>
        IsModReady && SunqianUniverseInterop.IsSupportingActorCharacter(player?.Character);

    public static IEnumerable<IHoverTip> EffectHoverTips(CardModel card)
    {
        if (ShowsCombatEffect(MutableOwner(card)))
        {
            if (SunqianUniverseInterop.CreateBurningHoverTip() is { } burningTip)
                yield return burningTip;
            yield break;
        }

        yield return HoverTipFactory.FromPower<HuoGongPower>();
    }

    public static IEnumerable<IHoverTip> LibraryHoverTips()
    {
        if (!IsModReady || IsCombatInProgress())
            yield break;

        yield return new HoverTip(
            new LocString("cards", HoverTitleKey),
            new LocString("cards", LibraryHoverKey));
    }

    public static void AddEffectText(LocString description, CardModel card) =>
        description.Add(
            EffectTextArg,
            new LocString(
                "cards",
                ShowsCombatEffect(MutableOwner(card)) ? BurningTextKey : FireAttackTextKey)
                .GetFormattedText());

    public static async Task ApplyEffect(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel cardSource,
        bool useBurning)
    {
        if (useBurning)
        {
            await SunqianUniverseInterop.ApplyBurning(
                choiceContext, target, amount, applier, cardSource, false);
            return;
        }

        await PowerCmd.Apply<HuoGongPower>(
            choiceContext, target, amount, applier, cardSource, false);
    }

    public static PowerModel? FindEffect(Creature target, bool useBurning) =>
        useBurning
            ? target.Powers?.FirstOrDefault(SunqianUniverseInterop.IsBurningPower)
            : target.Powers?.OfType<HuoGongPower>().FirstOrDefault();

    private static Player? MutableOwner(CardModel card) =>
        card.IsMutable ? card.Owner : null;

    private static bool ShowsCombatEffect(Player? owner) =>
        IsCombatInProgress() && Affects(owner);

    private static bool IsCombatInProgress() =>
        CombatManager.Instance is { IsInProgress: true };
}
