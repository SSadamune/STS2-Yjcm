using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 酒：0费技能牌 获得3→5点活力 消耗
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class Jiu : WuZhugeliangCard
{
    public Jiu()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(5m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(Jiu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "jiu.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(3);
    }
}
