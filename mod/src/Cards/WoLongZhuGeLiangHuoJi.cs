using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 卧龙诸葛-火计：1费罕见技能牌。消耗一颗星，使所有敌人获得{HuoGong}层火攻。升级后5层。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class WoLongZhuGeLiangHuoJi : WuZhugeliangCard
{
    private const string MultiplierKey = "Multiplier";

    public WoLongZhuGeLiangHuoJi()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    // 蜀+武将 tag
    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<XingPower>(), HoverTipFactory.FromPower<HuoGongPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(MultiplierKey, 2m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(WoLongZhuGeLiangHuoJi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "huo_ji.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 消耗一颗星（星不足时扣血，但效果依然触发）
        await XingPower.TryConsume(choiceContext, Owner.Creature, 1, this);

        // 将目标现有火攻层数翻倍；没有火攻时保持0层。
        var target = cardPlay.Target!;
        var current = target.Powers?.OfType<HuoGongPower>().FirstOrDefault()?.Amount ?? 0;
        var multiplier = (int)DynamicVars[MultiplierKey].BaseValue;
        if (current > 0 && multiplier > 1)
            await PowerCmd.Apply<HuoGongPower>(choiceContext, target, current * (multiplier - 1), Owner.Creature, this, false);
    }

    /// <summary>升级：火攻翻倍→三倍</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[MultiplierKey].UpgradeValueBy(1);
    }
}
