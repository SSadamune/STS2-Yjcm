using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 的卢：1费罕见能力牌（防御马）。装备到防御马栏并获得1层虚弱/易伤/脆弱。
/// 防御马被动：获得敏捷时额外获得一点。效果：每回合第一次突破格挡的伤害，溢出部分无效。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class DiLu : WuZhugeliangCard
{
    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(DiLu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "fang_ju.wav");

    public DiLu()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    // 为描述中的 [虚弱]/[易伤]/[脆弱] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(),
         HoverTipFactory.FromPower<VulnerablePower>(),
         HoverTipFactory.FromPower<FrailPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var owner = cardPlay.Card.Owner;
        if (owner == null) return;

        // 装备防御马（同类防御马自动替换防御马栏旧球）
        var orb = (EquipmentOrbModel)ModelDb.Orb<DiLuOrb>().ToMutable();
        await EquipCmd.ChannelEquipment(choiceContext, owner, orb);

        // 代价：获得1层虚弱、易伤、脆弱
        await PowerCmd.Apply<WeakPower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);
        await PowerCmd.Apply<FrailPower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1→0
    }
}
