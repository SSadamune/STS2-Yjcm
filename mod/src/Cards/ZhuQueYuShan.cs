using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 朱雀羽扇：1费能力牌（武器）。装备到武器栏：每次攻击使敌人获得2层火攻。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhuQueYuShan : WuZhugeliangCard
{
    public ZhuQueYuShan()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    // 为描述中的 [火攻] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<HuoGongPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhuQueYuShan).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_qi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备武器：同类武器自动替换武器栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<ZhuQueYuShanOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
