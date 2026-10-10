using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 紫骍（进攻马）：每回合开始时获得6点活力。
/// 继承进攻马被动：获得力量时额外获得一点。
/// </summary>
[RegisterOrb]
public sealed class ZiXingOrb : OffensiveHorseOrb
{
    public override Color DarkenedColor => new("6B3FA0");

    private const string IconRes = "res://Yjcm/Images/Powers/zi_xing.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes, 54f);

    // 每回合开始时获得6点活力（参考酒：PowerCmd.Apply&lt;VigorPower&gt;）
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await base.AfterTurnStartOrbTrigger(choiceContext);
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, 10m, Owner.Creature, null, false);
    }
}
