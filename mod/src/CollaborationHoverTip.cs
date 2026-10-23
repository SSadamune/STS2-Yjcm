using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Yjcm;

internal static class CollaborationHoverTip
{
    internal static IHoverTip For(string sourceKey) => new HoverTip(
        new LocString("cards", "YJCM_HOVER_COLLABORATION.title"),
        new LocString("cards", sourceKey), icon: null);
}
