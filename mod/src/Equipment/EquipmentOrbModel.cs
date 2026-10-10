using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// 装备充能球基类：代表三国杀中的一件已装备的装备（武器/防具/马）。
/// 每个装备充能球对应一个固定栏位（<see cref="SlotType"/>），
/// 同类装备替换时由 <see cref="EquipCmd.ChannelEquipment"/> 原地替换，不触发激发。
/// 装备效果（武器加距离、防具防御、马调整距离等）后续步骤在具体装备球中实现。
/// </summary>
public abstract class EquipmentOrbModel : ModOrbTemplate
{
    /// <summary>该装备所属的栏位类型（武器/防具/防马/攻马）。</summary>
    public abstract EquipmentSlotType SlotType { get; }

    // 装备是持续效果，没有被动/激发数值（占位阶段为 0）
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;

    // 装备球不进入随机充能球池（ModOrbTemplate 默认即 false，这里显式声明意图）
    public override bool AllowInRandomOrbPool => false;

    // 充能球入栏音效：复用原版闪电球激发音效。
    // 若不提供有效音效，PlayChannelSfx 会回退到缺失的 debug_audio 资源导致报错。
    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";

    // 回合触发/被动/激发均无效果：装备效果是持续被动，由具体装备球后续实现
}
