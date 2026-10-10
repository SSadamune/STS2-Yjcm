using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Powers;

#nullable enable

namespace Yjcm;

/// <summary>
/// 巡使标记：本场战斗中地狱审判次数+1（不可见buff）
/// </summary>
public sealed class XunShiJudgmentPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/xun_shi.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/xun_shi.png";
}
