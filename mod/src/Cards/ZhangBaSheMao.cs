using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 丈八蛇矛：1费罕见能力牌（武器）。装备到武器栏：每回合开始时可以弃置两张手牌，
/// 然后将一张无色杀加入手牌，并选择一张攻击牌获得重放1。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhangBaSheMao : WuZhugeliangCard
{
    public ZhangBaSheMao()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhangBaSheMao).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_qi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备武器：同类武器自动替换武器栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<ZhangBaSheMaoOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
