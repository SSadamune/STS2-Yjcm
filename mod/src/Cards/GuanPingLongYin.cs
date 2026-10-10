using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 关平-龙吟：2费罕见技能牌。下{Stacks}张攻击牌免费打出。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class GuanPingLongYin : WuZhugeliangCard
{
    private const string StacksKey = "Stacks";

    public GuanPingLongYin()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<LongYinPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(StacksKey, 1)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(GuanPingLongYin).Assembly.Location)!,
        "Voice", "Bank", "Assets", "long_yin.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var stacks = (int)DynamicVars[StacksKey].BaseValue;
        await PowerCmd.Apply<LongYinPower>(choiceContext, Owner.Creature, stacks, Owner.Creature, this, false);
    }

    /// <summary>升级：1张→2张</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[StacksKey].UpgradeValueBy(1);
    }
}
