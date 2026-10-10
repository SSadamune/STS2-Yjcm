using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 青龙偃月刀（武器）：伤害被完全格挡后，获得「青龙蓄势」使下一张攻击牌免费打出。
/// 装备球占据武器栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class QingLongYanYueDaoOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;

    public override Color DarkenedColor => new("B36A00");

    private const string IconRes = "res://Yjcm/Images/Powers/qing_long_dao.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        if (dealer != Owner.Creature) return;
        if (result.BlockedDamage <= 0 || result.TotalDamage != result.BlockedDamage) return;

        // 移除旧蓄势再添加新的，确保不叠层
        var old = Owner.Creature.Powers?.OfType<QingLongChargePower>().FirstOrDefault();
        if (old != null) await PowerCmd.Remove(old);
        await PowerCmd.Apply<QingLongChargePower>(choiceContext, Owner.Creature, 1, Owner.Creature, cardSource, false);
    }
}
