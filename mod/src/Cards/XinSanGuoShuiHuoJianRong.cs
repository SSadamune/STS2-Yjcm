using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class XinSanGuoShuiHuoJianRong : WuZhugeliangCard
{
    public XinSanGuoShuiHuoJianRong()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<ShuiHuoJianRongPower>(),
        CollaborationHoverTip.For("YJCM_HOVER_COLLABORATION_XIN_SAN_GUO.description")
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(XinSanGuoShuiHuoJianRong).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shui_huo_jian_rong.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await PowerCmd.Apply<ShuiHuoJianRongPower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
