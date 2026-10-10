using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Combat.CardTargeting;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 决斗：2费攻击牌 只能对攻击意图敌人使用 造成(30-意图伤害)/升级(40-意图伤害)
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class JueDou : WuZhugeliangCard
{
    public JueDou()
        : base(2, CardType.Attack, CardRarity.Common, CustomTargetType.AnyAttackingEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30, ValueProp.Move)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(JueDou).Assembly.Location)!,
        "Voice", "Bank", "Assets", "jue_dou.wav");

    // 有攻击意图的敌人时卡牌高亮提示
    protected override bool ShouldGlowGoldInternal =>
        CombatState?.HittableEnemies.Any(e => e.Monster?.IntendsToAttack == true) ?? false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var target = cardPlay.Target!;
        var baseDmg = IsUpgraded ? 40m : 30m;
        var intentDmg = GetIntentDamage(target);
        var damage = Math.Max(0, baseDmg - intentDmg);

#if GAME_BETA
        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target).Execute(choiceContext);
#else
        await DamageCmd.Attack(damage).FromCard(this).Targeting(target).Execute(choiceContext);
#endif
    }

    // 获取敌人的意图伤害值
    private static decimal GetIntentDamage(Creature enemy)
    {
        var intents = enemy.Monster?.NextMove?.Intents;
        if (intents == null) return 0;

        foreach (var intent in intents)
        {
            if (intent is AttackIntent ai && ai.DamageCalc != null)
                return ai.DamageCalc();
        }
        return 0;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
    }
}
