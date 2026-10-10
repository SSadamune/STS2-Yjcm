using MegaCrit.Sts2.Core.Commands;
#if GAME_BETA
#else
using MegaCrit.Sts2.Core.Commands.Builders;
#endif
using MegaCrit.Sts2.Core.Entities.Cards;
#if GAME_BETA
using MegaCrit.Sts2.Core.Commands.Builders;
#else
#endif
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 南蛮入侵：1费攻击牌 对所有敌人12/16伤害 对攻击意图敌人伤害减半
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class NanManRuQin : WuZhugeliangCard
{
    public NanManRuQin()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16, ValueProp.Move)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(NanManRuQin).Assembly.Location)!,
        "Voice", "Bank", "Assets", "nanman_ru_qin.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var baseDamage = DynamicVars.Damage.BaseValue;
        var enemies = CombatState.HittableEnemies.ToList();
#if GAME_BETA
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, cardPlay);
#else
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, this);
#endif
        var results = new List<DamageResult>();

        foreach (var enemy in enemies)
        {
            var dmg = enemy.Monster?.IntendsToAttack == true ? baseDamage / 2 : baseDamage;
#if GAME_BETA
            results.AddRange(await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Move, this, cardPlay));
#else
            results.AddRange(await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Move, this));
#endif
        }
        attack.AddHit(results);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
