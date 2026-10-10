using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 诸葛连弩（武器）：所有攻击牌费用-1。
/// 装备球占据武器栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class ZhuGeLianNuOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;

    public override Color DarkenedColor => new("B36A00");

    private const string IconRes = "res://Yjcm/Images/Powers/zhu_ge_lian_nu.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner.Creature) return false;
        if (card.Type != CardType.Attack) return false;
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play)) return false;
        modifiedCost = Math.Max(0, originalCost - 1);
        return true;
    }
}
