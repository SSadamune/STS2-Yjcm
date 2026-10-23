using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

[RegisterPower]
public sealed class ShuiHuoJianRongPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    // Reuse the supplied card art, rather than a placeholder from another character.
    public override string CustomIconPath => "res://Yjcm/Images/Cards/xin_san_guo_shui_huo_jian_rong.png";
    public override string CustomBigIconPath => CustomIconPath;
}
