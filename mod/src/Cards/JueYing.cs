using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 绝影：1费罕见能力牌（防御马）。装备到防御马栏。
/// 防御马被动：获得敏捷时额外获得一点。效果：每回合第一次获得的格挡变为2倍。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class JueYing : WuZhugeliangCard
{
    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(JueYing).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    public JueYing()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;

        // 装备防御马（同类防御马自动替换防御马栏旧球）
        var orb = (EquipmentOrbModel)ModelDb.Orb<JueYingOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1→0
    }
}
