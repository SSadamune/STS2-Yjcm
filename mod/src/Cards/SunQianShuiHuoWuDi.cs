using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class SunQianShuiHuoWuDi : WuZhugeliangCard
{
    public SunQianShuiHuoWuDi()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<HuoGongPower>(),
        CollaborationHoverTip.For("YJCM_HOVER_COLLABORATION_SUN_QIAN.description")
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(SunQianShuiHuoWuDi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shui_huo_wu_di.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!);
#else
            .FromCard(this).Targeting(cardPlay.Target!);
#endif
        await attack.Execute(choiceContext);
        var results = attack.Results.SelectMany(hit => hit).ToList();
        var damage = results.Sum(HealthDamage);
        if (damage <= 0) return;

        // Equal to actual HP lost: neither blocked damage nor overkill grants a reward.
        foreach (var group in results.GroupBy(result => result.Receiver))
        {
            var stacks = group.Sum(HealthDamage);
            if (stacks > 0 && group.Key.IsAlive)
                await PowerCmd.Apply<HuoGongPower>(
                    choiceContext, group.Key, stacks, Owner.Creature, this, false);
        }
        // Unpowered keeps the granted Block equal, rather than applying Dexterity/Frail again.
        await CreatureCmd.GainBlock(Owner.Creature, damage, ValueProp.Unpowered, cardPlay);
    }

    internal static int HealthDamage(DamageResult result) =>
        Math.Max(0, result.UnblockedDamage - result.OverkillDamage);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}
