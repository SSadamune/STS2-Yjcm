using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 无中生有：0费罕见技能牌。消耗，抽2张牌。升级后去除消耗。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class WuZhongShengYou : WuZhugeliangCard
{
    public WuZhongShengYou()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(WuZhongShengYou).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_zhong_sheng_you.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await CardPileCmd.Draw(choiceContext, 2, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
