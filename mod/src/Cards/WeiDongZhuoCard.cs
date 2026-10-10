using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// Independent base: deliberately does not inherit WuZhugeliangCard's pool registration.
/// Concrete copies carry their own registration and stable, distinct public entry.
/// </summary>
public abstract class WeiDongZhuoCard(
    int cost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool shouldShowInCardLibrary = true)
    : ModCardTemplate(cost, type, rarity, target, shouldShowInCardLibrary)
{
    public override CardPoolModel Pool => ModelDb.CardPool<WeiDongZhuoCardPool>();

    // Resolve the original portrait explicitly; a model ID is not a resource filename.
    public override string CustomPortraitPath => WeiDongZhuoCloneMap.OriginalPortraitPath(GetType());

    internal bool TryGetWuJiangHoverTip(out IHoverTip tip)
    {
        tip = null!;
        if (!CanonicalTags.Contains(YjcmCardTags.WuJiang)) return false;
        tip = WuJiangHoverTip.ForCard(CanonicalTags);
        return true;
    }
}
