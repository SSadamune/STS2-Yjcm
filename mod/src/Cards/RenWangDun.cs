using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 仁王盾：1费能力牌（防具）。装备到防具栏：每回合首次受到伤害时，下回合开始获得等量格挡。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class RenWangDun : WuZhugeliangCard
{
    public RenWangDun()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords => [];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(RenWangDun).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        // 装备防具：同类防具自动替换防具栏旧球
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;
        var orb = (EquipmentOrbModel)ModelDb.Orb<RenWangDunOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1→0
    }
}
