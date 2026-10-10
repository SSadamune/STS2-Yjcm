using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 白银狮子（防具）：当无实体层数减少时，恢复7点体力。
/// 打出装备牌时额外获得1层无实体；装备球占据防具栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class BaiYinShiZiOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Armor;

    public override Color DarkenedColor => new("6A8EB3");

    private const string IconRes = "res://Yjcm/Images/Powers/bai_yin_shi_zi.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        // 当无实体层数减少时触发（amount < 0 表示减少）
        if (power is IntangiblePower && power.Owner == Owner.Creature && amount < 0)
            await CreatureCmd.Heal(Owner.Creature, 7);
    }
}
