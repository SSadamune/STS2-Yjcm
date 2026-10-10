using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 绝影（防御马）：每回合第一次获得的格挡变为2倍。
/// 继承防御马被动：获得敏捷时额外获得一点。
/// </summary>
[RegisterOrb]
public sealed class JueYingOrb : DefensiveHorseOrb
{
    public override Color DarkenedColor => new("4A5568");

    private const string IconRes = "res://Yjcm/Images/Powers/jue_ying.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes, 54f);

    // 每次获得格挡时额外获得6点格挡。
    public override decimal ModifyBlockAdditive(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner.Creature) return 0m;
        if (block <= 0) return 0m;
        return 6m;
    }
}
