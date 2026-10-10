using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Concurrent;

namespace Yjcm;

/// <summary>
/// 卡牌选择助手：从全卡池（含所有角色与 mod 卡）筛选可生成进战斗的牌，
/// 供背置牌 / 荀勖残魂 复用。
/// </summary>
internal static class CardSelectionHelper
{
    private static readonly ConcurrentDictionary<Type, byte> UnusableCardTypes = new();

    /// <summary>筛选全卡池候选：排除 Token/隐藏、诅咒、状态与指定排除牌；可按需排除攻击牌</summary>
    internal static List<CardModel> GetEligibleCards(bool includeAttacks, CardModel? exclude)
        => ModelDb.AllCards
            .Where(c => IsEligible(c, includeAttacks, exclude))
            .ToList();

    private static bool IsEligible(CardModel card, bool includeAttacks, CardModel? exclude)
    {
        if (ReferenceEquals(card, exclude)) return false;

        var type = card.GetType();
        // AutoAnthony 的牌在读取 Type 时会即时构建整套角色卡池；构建失败会让胡笳弃牌等动作反复异常。
        // 这类动态模板也不适合被当成可直接生成的普通卡牌。
        if (type.FullName == "AutoAnthony.ChaosCardModel" || UnusableCardTypes.ContainsKey(type))
            return false;

        try
        {
            return card.ShouldShowInCardLibrary &&
                   card.CanBeGeneratedInCombat &&
                   (includeAttacks || card.Type != CardType.Attack) &&
                   card.Type != CardType.Curse &&
                   card.Type != CardType.Status;
        }
        catch (Exception ex)
        {
            if (UnusableCardTypes.TryAdd(type, 0))
                MainFile.Logger.Warn($"跳过无法安全生成的外部卡牌 {type.FullName}: {ex.Message}");
            return false;
        }
    }
}
