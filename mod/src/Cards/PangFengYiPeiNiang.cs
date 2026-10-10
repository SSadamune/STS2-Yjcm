using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// �ӷ���-����1�Ѽ����ƣ����ɡ���2�žƼ������ƣ�������Ϊ�������ľ�
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class PangFengYiPeiNiang : WuZhugeliangCard
{
    public PangFengYiPeiNiang()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    // 为描述中的 [酒] 提供卡牌悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Jiu>(IsUpgraded)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(PangFengYiPeiNiang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "pang_feng_yi_pei_niang.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        for (int i = 0; i < 2; i++)
        {
            var jiuCard = CombatState.CreateCard<Jiu>(Owner);
            if (IsUpgraded)
                jiuCard.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(jiuCard, PileType.Hand, Owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
