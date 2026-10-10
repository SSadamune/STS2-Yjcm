using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 刘巴-流转：1费普通技能牌。攻击意图敌人获得{Amount}层虚弱，防御意图敌人获得{Amount}层易伤。升级后2→3层。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class LiuBaLiuZhuan : WuZhugeliangCard
{
    private const string AmountKey = "Amount";

    public LiuBaLiuZhuan()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(AmountKey, 2m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(LiuBaLiuZhuan).Assembly.Location)!,
        "Voice", "Bank", "Assets", "liu_zhuan.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var amount = DynamicVars[AmountKey].BaseValue;

        foreach (var enemy in CombatState.HittableEnemies)
        {
            if (enemy.Monster?.IntendsToAttack == true)
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, amount, Owner.Creature, this, false);
            else
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, amount, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[AmountKey].UpgradeValueBy(1); // 2→3
    }
}
