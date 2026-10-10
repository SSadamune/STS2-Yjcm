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
/// 诸葛亮-观星：0费普通技能牌。观星{GuanXing}，抽{Draw}张牌，获得1颗星。
/// 升级后观星5抽2。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhugeliangGuanXing : WuZhugeliangCard
{
    private const string GxKey = "GuanXing";
    private const string DrawKey = "Draw";

    public ZhugeliangGuanXing()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.GuanXing];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<XingPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(GxKey, 3),
        new DynamicVar(DrawKey, 1)
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhugeliangGuanXing).Assembly.Location)!,
        "Voice", "Bank", "Assets", "guan_xing.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var gxCount = (int)DynamicVars[GxKey].BaseValue;
        var drawCount = (int)DynamicVars[DrawKey].BaseValue;

        await GuanXingHelper.GuanXing(choiceContext, Owner, gxCount);
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        await XingPower.Gain(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    /// <summary>升级：观星3→5，抽1→2</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[GxKey].UpgradeValueBy(2);
        DynamicVars[DrawKey].UpgradeValueBy(1);
    }
}
