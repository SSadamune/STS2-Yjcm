using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 乐不思蜀：1费稀有技能牌。3/4概率击晕一名敌人。虚无，升级后去除虚无。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class LeBuSiShu : WuZhugeliangCard
{
    public LeBuSiShu()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [StunIntent.GetStaticHoverTip()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(LeBuSiShu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "le_bu_si_shu.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 刘焉的诏令使击晕率变为100%；否则维持3/4概率。
        bool guaranteed = Owner.Relics.OfType<LiuYanZhaoLing>().Any();
        if (guaranteed || Owner.RunState.Rng.CombatCardSelection.NextDouble() < 0.75)
            await CreatureCmd.Stun(cardPlay.Target!);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
