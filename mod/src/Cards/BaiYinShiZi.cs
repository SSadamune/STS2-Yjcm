using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 白银狮子：1费稀有能力牌（防具）。装备到防具栏并获得1层无实体；无实体层数减少时恢复7点体力。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class BaiYinShiZi : WuZhugeliangCard
{
    public BaiYinShiZi()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    // 为描述中的 [无实体] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<IntangiblePower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(BaiYinShiZi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;

        // 装备防具（同类防具自动替换防具栏旧球），并获得1层无实体
        var orb = (EquipmentOrbModel)ModelDb.Orb<BaiYinShiZiOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);
        await PowerCmd.Apply<IntangiblePower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
