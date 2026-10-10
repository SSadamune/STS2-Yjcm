using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 紫骍：1费罕见能力牌（进攻马）。装备到进攻马栏。
/// 进攻马被动：获得力量时额外获得一点。效果：每回合开始时获得6点活力。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZiXing : WuZhugeliangCard
{
    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZiXing).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    public ZiXing()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;

        // 装备进攻马（同类进攻马自动替换进攻马栏旧球）
        var orb = (EquipmentOrbModel)ModelDb.Orb<ZiXingOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1→0
    }
}
