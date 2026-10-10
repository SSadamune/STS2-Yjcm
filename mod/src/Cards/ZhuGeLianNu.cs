using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 诸葛连弩：1费稀有能力牌（武器）。装备到武器栏：所有攻击牌费用-1。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhuGeLianNu : WuZhugeliangCard
{
    public ZhuGeLianNu()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhuGeLianNu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_qi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备武器：同类武器自动替换武器栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<ZhuGeLianNuOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
