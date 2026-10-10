using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 空·宝物栏充能球：占据宝物栏，显示装备区名字「宝物栏」，无任何效果。
/// 战斗开始时由起始遗物自动生成。
/// 宝物栏是收容栏位：装备宝物时被 <see cref="EquipCmd.ChannelEquipment"/> 同类替换；
/// 非装备充能球（闪电、冰霜等）被 <see cref="EquipCmd.ChannelTreasure"/> 替换。
/// </summary>
[RegisterOrb]
public sealed class EmptyTreasureSlotOrb : EquipmentOrbModel
{
    // 对应宝物栏
    public override EquipmentSlotType SlotType => EquipmentSlotType.Treasure;

    // 无效果数值
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;

    // 隐藏数值标签，保持"空槽"外观
    public override ModOrbValueDisplayMode ValueDisplayMode => ModOrbValueDisplayMode.Hidden;

    // 空槽主色调（暗淡，接近无充能球状态）
    public override Color DarkenedColor => new("5A5A5A");

    // 贴图采用充能球栏位无球状态的贴图
    private const string EmptySlotIcon = "res://images/orbs/empty_orb.png";

    public override OrbAssetProfile AssetProfile => new(
        IconPath: EmptySlotIcon,
        VisualsScenePath: null);

    // 纯代码创建空槽外观的充能球视觉（空槽贴图太大，缩放到 50%）
    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.Create(EmptySlotIcon, 0.5f);
}
