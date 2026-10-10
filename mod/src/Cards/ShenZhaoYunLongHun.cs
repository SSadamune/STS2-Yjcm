using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>神赵云-龙魂：2费稀有能力牌；升级后费用变为 1。</summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ShenZhaoYunLongHun : WuZhugeliangCard
{
    public ShenZhaoYunLongHun()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenZhaoYunLongHun).Assembly.Location)!,
        "Voice", "Bank", "Assets", "long_hun.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await PowerCmd.Apply<ShenZhaoYunLongHunPower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
