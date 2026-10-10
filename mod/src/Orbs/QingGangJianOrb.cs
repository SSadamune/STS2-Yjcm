using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 青钢剑（武器）：所有攻击无视格挡。
/// 效果由 <see cref="QingGangJianIgnoreBlockPatch"/> 实现——
/// 检测武器栏是否装有本球，装备/替换自动生效，无需生命周期管理。
/// </summary>
[RegisterOrb]
public sealed class QingGangJianOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;

    public override Color DarkenedColor => new("8E9AAE");

    private const string IconRes = "res://Yjcm/Images/Powers/qing_gang_jian.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);
}
