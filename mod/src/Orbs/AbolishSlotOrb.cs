using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 废除装备栏充能球：代表某个装备栏被「废除」。
/// 被废除的栏位不能再装备任何装备（<see cref="EquipCmd.ChannelEquipment"/> 会拦截），
/// 直到被「恢复」——即移除废除球并换回对应的空槽球（<see cref="EquipCmd.RestoreSlot"/>）。
/// <see cref="SlotType"/> 可设置，因此一个废除球可出现在任意装备栏位。
/// </summary>
[RegisterOrb]
public sealed class AbolishSlotOrb : EquipmentOrbModel
{
    // 被废除的装备栏位（内部可变：废除球需能出现在任意栏位）
    private EquipmentSlotType _slotType;

    public override EquipmentSlotType SlotType => _slotType;

    /// <summary>设置被废除的栏位（创建/放置废除球时调用）。</summary>
    public void SetSlotType(EquipmentSlotType slotType) => _slotType = slotType;

    // 无效果数值
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;

    // 隐藏数值标签，保持"废除"外观
    public override ModOrbValueDisplayMode ValueDisplayMode => ModOrbValueDisplayMode.Hidden;

    // 废除栏位主色调（灰暗，象征栏位被封锁）
    public override Color DarkenedColor => new("2B2B2B");

    // 贴图采用「废充能球」素材
    private const string IconRes = "res://Yjcm/Images/Powers/abolish_orb.png";

    public override OrbAssetProfile AssetProfile => new(
        IconPath: IconRes,
        VisualsScenePath: null);

    // 纯代码创建废除球外观的充能球视觉（比默认 77px 缩小 10%）
    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes, 69.3f);
}
