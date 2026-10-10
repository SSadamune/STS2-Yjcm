using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Yjcm;

/// <summary>
/// 三国杀 Mod 自定义卡牌关键词（转换技、武将、神、死、连环、击伤）
/// 使用 [RegisterOwnedCardKeyword] 属性注册，由 RegisterModAssembly 自动扫描
/// 本地化：card_keywords.json，key 格式 YJCM_KEYWORD_{NAME}
/// </summary>
[RegisterOwnedCardKeyword(nameof(ZhuanHuanJi), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(WuJiang))]
[RegisterOwnedCardKeyword(nameof(Shen))]
[RegisterOwnedCardKeyword(nameof(Si))]
[RegisterOwnedCardKeyword(nameof(LianHuan))]
[RegisterOwnedCardKeyword(nameof(JiShang))]
[RegisterOwnedCardKeyword(nameof(DiYuShenPan))]
[RegisterOwnedCardKeyword(nameof(ShangYu))]
[RegisterOwnedCardKeyword(nameof(GuanXing))]
public class YjcmKeywords
{
	public static readonly CardKeyword ZhuanHuanJi = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(ZhuanHuanJi)).GetModCardKeyword();
	public static readonly CardKeyword WuJiang     = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(WuJiang)).GetModCardKeyword();
	public static readonly CardKeyword Shen        = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Shen)).GetModCardKeyword();
	public static readonly CardKeyword Si          = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Si)).GetModCardKeyword();
	public static readonly CardKeyword LianHuan    = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(LianHuan)).GetModCardKeyword();
	public static readonly CardKeyword JiShang     = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(JiShang)).GetModCardKeyword();
	public static readonly CardKeyword DiYuShenPan = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(DiYuShenPan)).GetModCardKeyword();
	public static readonly CardKeyword ShangYu     = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(ShangYu)).GetModCardKeyword();
	public static readonly CardKeyword GuanXing    = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(GuanXing)).GetModCardKeyword();
}
