using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 寒冰剑（武器）：每回合第一次对敌人造成伤害后，使其失去2点力量。
/// 装备球占据武器栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class HanBingJianOrb : EquipmentOrbModel
{
    /// <summary>本回合是否已触发过寒冰剑效果</summary>
    private bool _usedThisTurn;

    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;

    public override Color DarkenedColor => new("4FA3C2");

    private const string IconRes = "res://Yjcm/Images/Powers/han_bing_jian.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        if (dealer != Owner.Creature) return;
        // 每回合仅触发一次，且必须对敌人造成实际伤害
        if (_usedThisTurn || result.UnblockedDamage <= 0) return;
        if (target.Player != null) return; // 只对敌人生效

        _usedThisTurn = true;
        // 使敌人永久失去2点力量（StrengthPower 负值=永久降低）
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, -2, Owner.Creature, cardSource, false);
    }

    // 己方回合开始时重置触发标记
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await base.AfterTurnStartOrbTrigger(choiceContext);
        _usedThisTurn = false;
    }
}
