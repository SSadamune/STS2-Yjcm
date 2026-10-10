using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 贯石斧：1费能力牌（武器）。装备到武器栏：伤害被完全格挡后可弃2张牌造成等量不可格挡伤害。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class GuanShiFu : WuZhugeliangCard
{
    public GuanShiFu()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(GuanShiFu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_qi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备武器：同类武器自动替换武器栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<GuanShiFuOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
