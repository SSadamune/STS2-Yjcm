using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 一枕清梦：2费稀有技能牌。触发所有敌人的火攻2次，回合结束敌人回复剩余火攻生命并失去火攻。升级后1费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class YiZhenQingMeng : WuZhugeliangCard
{
    public YiZhenQingMeng()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<HuoGongPower>(),
         HoverTipFactory.FromPower<XingPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(YiZhenQingMeng).Assembly.Location)!,
        "Voice", "Bank", "Assets", "yi_zhen_qing_meng.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 先使所有敌人获得7层火攻
        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<HuoGongPower>(choiceContext, enemy, 7, Owner.Creature, this, false);

        // 触发所有敌人的火攻2次（每次触发减少一层）
        foreach (var enemy in CombatState.HittableEnemies)
        {
            var huoGong = enemy.Powers?.OfType<HuoGongPower>().FirstOrDefault();
            if (huoGong == null || huoGong.Amount <= 0) continue;

            for (int i = 0; i < 2; i++)
            {
                if (huoGong.Amount <= 0) break;
                await CreatureCmd.Damage(choiceContext, enemy, huoGong.Amount,
#if GAME_BETA
                    ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);
#else
                    ValueProp.Unblockable | ValueProp.Unpowered, this);
#endif
                huoGong.SetAmount(huoGong.Amount - 1);
            }
        }

        // 获得1颗星
        await XingPower.Gain(choiceContext, Owner.Creature, 1, Owner.Creature, this);

        // 回合结束时敌人回复并失去火攻
        await PowerCmd.Apply<YiZhenQingMengPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2→1
    }
}
