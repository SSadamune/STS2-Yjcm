using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 谋吴懿-庇部：1费罕见能力牌。每回合敌人第一次获得力量时，
/// 使其失去等量力量，你获得等量力量。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouWuYiBiBu : WuZhugeliangCard
{
    public MouWuYiBiBu()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouWuYiBiBu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "bi_bu.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await PowerCmd.Apply<BiBuPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    /// <summary>升级：1费→0费</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
