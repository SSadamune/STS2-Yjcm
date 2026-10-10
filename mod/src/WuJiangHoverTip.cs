using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Yjcm;

/// <summary>
/// 武将悬浮框：按势力生成"这是X的武将"提示
/// 文案：蜀/魏/吴/群 → "这是X国的武将"，神将 → "这是神将"
/// </summary>
public static class WuJiangHoverTip
{
    private const string LocTable = "cards";
    private const string TitleKey = "YJCM_HOVER_WU_JIANG.title";

    // 按势力 tag 生成武将悬浮框
    public static IHoverTip ForFaction(CardTag faction)
    {
        string descKey = faction switch
        {
            // 神将无"国"，文案固定为"这是神将"
            _ when faction == YjcmCardTags.ShenJiang => "YJCM_HOVER_WU_JIANG_SHEN.description",
            _ when faction == YjcmCardTags.Wei => "YJCM_HOVER_WU_JIANG_WEI.description",
            _ when faction == YjcmCardTags.Wu => "YJCM_HOVER_WU_JIANG_WU.description",
            _ when faction == YjcmCardTags.Qun => "YJCM_HOVER_WU_JIANG_QUN.description",
            // 默认蜀
            _ => "YJCM_HOVER_WU_JIANG_SHU.description",
        };
        return new HoverTip(
            new LocString(LocTable, TitleKey),
            new LocString(LocTable, descKey),
            icon: null);
    }

    // 从牌的势力 tag 集合解析武将悬浮框（神将优先）
    public static IHoverTip ForCard(IEnumerable<CardTag> tags)
        => ForFaction(ResolveFaction(tags));

    private static CardTag ResolveFaction(IEnumerable<CardTag> tags)
    {
        if (tags.Contains(YjcmCardTags.ShenJiang)) return YjcmCardTags.ShenJiang;
        if (tags.Contains(YjcmCardTags.Wei)) return YjcmCardTags.Wei;
        if (tags.Contains(YjcmCardTags.Wu)) return YjcmCardTags.Wu;
        if (tags.Contains(YjcmCardTags.Qun)) return YjcmCardTags.Qun;
        return YjcmCardTags.Shu;
    }
}
