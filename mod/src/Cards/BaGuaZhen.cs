using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 八卦阵：1费能力牌（防具）。装备到防具栏：回合开始获得意图为攻击的敌人数量×10的格挡。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class BaGuaZhen : WuZhugeliangCard
{
    public BaGuaZhen()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(BaGuaZhen).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备防具：同类防具自动替换防具栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<BaGuaZhenOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
