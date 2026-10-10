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
/// 诸葛亮-空城：1费罕见技能牌，消耗。弃置所有手牌，获得1层缓冲，结束回合。
/// 升级后获得保留。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhugeliangKongCheng : WuZhugeliangCard
{
    public ZhugeliangKongCheng()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords => [];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    // 为描述中的 [缓冲] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<BufferPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhugeliangKongCheng).Assembly.Location)!,
        "Voice", "Bank", "Assets", "kong_cheng.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 弃置所有手牌（除自身外）
        var handPile = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        var toDiscard = handPile?.Cards?.Where(c => c != this).ToList() ?? [];
        foreach (var card in toDiscard)
            await CardCmd.Discard(choiceContext, card);

        // 获得1层缓冲
        await PowerCmd.Apply<BufferPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);

        // 结束回合
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    /// <summary>升级：获得保留</summary>
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
