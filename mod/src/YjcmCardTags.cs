using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 三国杀 Mod 自定义卡牌 Tag（势力、转换技等卡面标签）
/// 使用 [RegisterOwnedCardTag] 属性注册，由 RegisterModAssembly 自动扫描
/// </summary>
[RegisterOwnedCardTag(nameof(Wei))]
[RegisterOwnedCardTag(nameof(Shu))]
[RegisterOwnedCardTag(nameof(Wu))]
[RegisterOwnedCardTag(nameof(Qun))]
[RegisterOwnedCardTag(nameof(WuJiang))]
[RegisterOwnedCardTag(nameof(ShenJiang))]
[RegisterOwnedCardTag(nameof(ZhuanHuanJi))]
public class YjcmCardTags
{
    public static readonly CardTag Wei         = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(Wei)).GetModCardTag();
    public static readonly CardTag Shu         = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(Shu)).GetModCardTag();
    public static readonly CardTag Wu          = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(Wu)).GetModCardTag();
    public static readonly CardTag Qun         = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(Qun)).GetModCardTag();
    public static readonly CardTag WuJiang     = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(WuJiang)).GetModCardTag();
    public static readonly CardTag ShenJiang   = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(ShenJiang)).GetModCardTag();
    public static readonly CardTag ZhuanHuanJi = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(ZhuanHuanJi)).GetModCardTag();
}
