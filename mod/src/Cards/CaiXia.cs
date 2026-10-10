using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 才瑕：2费罕见能力牌。造成或受到伤害后抽3张牌，之后失效直到使用3张牌重新激活。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class CaiXia : WuZhugeliangCard
{
    public CaiXia()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<CaiXiaPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(CaiXia).Assembly.Location)!,
        "Voice", "Bank", "Assets", "cai_xia.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await PowerCmd.Apply<CaiXiaPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2费 → 1费
    }
}
