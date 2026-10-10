using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 乐诸葛果-乘烟：2费罕见攻击牌（奇巧/武将/蜀）。造成6点伤害，抽1张牌；
/// 若抽到的牌以敌人为目标则自动打出，否则该牌本回合获得保留。升级后1费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class LeZhuGeGuoChengYan : WuZhugeliangCard
{
    public LeZhuGeGuoChengYan()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    // 奇巧：被弃置时推到弃牌堆顶（游戏内置关键词，自动显示）
    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(LeZhuGeGuoChengYan).Assembly.Location)!,
        "Voice", "Bank", "Assets", "zhu_ge_guo_cheng_yan.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif

        // 抽1张牌并取到抽到的牌
        var drawn = (await CardPileCmd.Draw(choiceContext, 1, Owner, false)).FirstOrDefault();
        if (drawn == null) return;

        // 抽到的牌以敌人为目标 → 自动打出（免费，目标随机）
        if (drawn.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy)
        {
            var hittable = Owner.Creature.CombatState?.HittableEnemies.ToList();
            if (hittable is { Count: > 0 })
            {
                var target = Owner.RunState.Rng.CombatCardSelection.NextItem(hittable);
                await CardCmd.AutoPlay(choiceContext,
                    drawn, target, AutoPlayType.Default, false, false);
            }
            else
            {
                // 没有可指定的敌人时改为保留
                drawn.GiveSingleTurnRetain();
            }
        }
        else
        {
            // 否则该牌本回合获得保留
            drawn.GiveSingleTurnRetain();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
