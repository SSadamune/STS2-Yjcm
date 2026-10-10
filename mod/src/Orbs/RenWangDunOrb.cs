using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 仁王盾（防具）：每回合首次受到伤害时记录伤害量，下回合开始获得等量格挡。
/// 装备球占据防具栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class RenWangDunOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Armor;

    public override Color DarkenedColor => new("6A8EB3");

    private const string IconRes = "res://Yjcm/Images/Powers/ren_wang_dun.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    // 待结算格挡量与本回合是否已触发
    private decimal _pendingBlock;
    private bool _triggeredThisTurn;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (_triggeredThisTurn || target != Owner.Creature) return;
        if (!props.IsPoweredAttack()) return;

        _triggeredThisTurn = true;
        _pendingBlock = result.TotalDamage;
    }

    // 回合开始前（摸牌前）获得格挡
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);
        if (player.Creature != Owner.Creature) return;

        _triggeredThisTurn = false;
        if (_pendingBlock > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, _pendingBlock, ValueProp.Move, null, false);
            _pendingBlock = 0;
        }
    }
}
