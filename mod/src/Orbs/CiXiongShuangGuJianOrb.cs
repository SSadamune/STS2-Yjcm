using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 雌雄双股剑（武器）：攻击指定非攻击意图敌人→获得1能量；技能指定攻击意图敌人→获得1能量。
/// 装备球占据武器栏，由 <see cref="EquipCmd.ChannelEquipment"/> 同类替换。
/// </summary>
[RegisterOrb]
public sealed class CiXiongShuangGuJianOrb : EquipmentOrbModel
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;

    public override Color DarkenedColor => new("B36A00");

    private const string IconRes = "res://Yjcm/Images/Powers/ci_xiong_jian.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes);

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;
        var target = cardPlay.Target;
        if (target == null || target.IsPlayer) return;

        var intents = target.Monster?.NextMove?.Intents;
        bool targetAttacking = intents?.Any(i => i is AttackIntent) == true;

        if (cardPlay.Card.Type == CardType.Attack && !targetAttacking)
            await PlayerCmd.GainEnergy(1, cardPlay.Card.Owner);
        else if (cardPlay.Card.Type == CardType.Skill && targetAttacking)
            await PlayerCmd.GainEnergy(1, cardPlay.Card.Owner);
    }
}
