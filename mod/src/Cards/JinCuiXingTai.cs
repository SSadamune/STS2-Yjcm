using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 尽瘁形态：3费稀有能力牌。每回合开始观星7，若体力值小于牌组数则调整为牌组数。
/// 升级后获得固有。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class JinCuiXingTai : WuZhugeliangCard
{
    public JinCuiXingTai()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.GuanXing];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.GuanXing),
         HoverTipFactory.FromPower<JinCuiPower>(),
         WuJiangHoverTip.ForFaction(YjcmCardTags.Shu)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(JinCuiXingTai).Assembly.Location)!,
        "Voice", "Bank", "Assets", "jin_cui.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        JinCuiPower.PlayBgm();
        await PowerCmd.Apply<JinCuiPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
