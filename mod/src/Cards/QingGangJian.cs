using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 青钢剑：1费稀有武器（能力牌）。装备到武器栏：所有攻击无视格挡。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class QingGangJian : WuZhugeliangCard
{
    public QingGangJian()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(QingGangJian).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_qi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备武器：同类武器自动替换武器栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<QingGangJianOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
