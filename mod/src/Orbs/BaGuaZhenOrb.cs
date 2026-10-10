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
/// 八卦阵（防具）：回合开始获得意图为攻击的敌人数量×10的格挡。
/// 装备球占据防具栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class BaGuaZhenOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Armor;

    public override Color DarkenedColor => new("6A8EB3");

    private const string IconRes = "res://Yjcm/Images/Powers/ba_gua_zhen.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);
        if (player.Creature != Owner.Creature) return;

        var attackCount = combatState.HittableEnemies
            .Count(e => e.Monster?.IntendsToAttack == true);
        if (attackCount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, attackCount * 10, ValueProp.Move, null, false);
    }
}
