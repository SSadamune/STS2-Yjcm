using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>龙魂：根据打出的牌类型分别获得格挡、随机伤害或力量与敏捷。</summary>
[RegisterPower]
public sealed class ShenZhaoYunLongHunPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/shen_zhao_yun_long_hun.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/shen_zhao_yun_long_hun.png";

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        var player = cardPlay.Card.Owner;
        if (player?.Creature != Owner || Owner?.CombatState == null)
            return;

        Flash();
        switch (cardPlay.Card.Type)
        {
            case CardType.Attack:
                await CreatureCmd.GainBlock(Owner, 6, ValueProp.Move, cardPlay);
                break;

            case CardType.Skill:
            {
                var enemies = Owner.CombatState.HittableEnemies.ToList();
                if (enemies.Count == 0)
                    return;

                var target = player.RunState.Rng.CombatTargets.NextItem(enemies);
#if GAME_BETA
                await CreatureCmd.Damage(
                    choiceContext, target, 6, ValueProp.Unpowered, cardPlay.Card, cardPlay);
#else
                await CreatureCmd.Damage(
                    choiceContext, target, 6, ValueProp.Unpowered, Owner, cardPlay.Card);
#endif
                break;
            }

            case CardType.Power:
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext, Owner, 1, Owner, cardPlay.Card, false);
                await PowerCmd.Apply<DexterityPower>(
                    choiceContext, Owner, 1, Owner, cardPlay.Card, false);
                break;
        }
    }
}
