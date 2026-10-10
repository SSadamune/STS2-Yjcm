using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// ���ŷ�-Ѳʹ��1�Ѽ����ƣ�ϡ�С���������״̬/����/��Ŀ�������ת��Ϊ��ɫɱ��
/// �����������ã�����ս���е������д���+1�����������ɵ���ɫɱΪ�����档
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ShenZhangFeiXunShi : WuZhugeliangCard
{
    private static readonly HashSet<Type> MultiTargetTypes = new()
    {
        typeof(TieSuoLianHuan),
        typeof(NanManRuQin),
        typeof(WanJianQiFa),
    };

    public ShenZhangFeiXunShi()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    // 为描述中的 [无色杀] 提供卡牌悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<WuSeSha>(IsUpgraded)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenZhangFeiXunShi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "xun_shi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 从抽牌堆、手牌、弃牌堆收集可转换的牌
        var toConvert = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
        {
            var pile = PileTypeExtensions.GetPile(pileType, Owner);
            if (pile?.Cards == null) continue;
            toConvert.AddRange(pile.Cards
                .Where(c => c != this && (c.Type == CardType.Status || c.Type == CardType.Curse
                         || MultiTargetTypes.Contains(c.GetType()))));
        }

        if (toConvert.Count == 0) return;

        foreach (var card in toConvert)
        {
            await CardPileCmd.RemoveFromCombat(card);
            var sha = Owner.Creature.CombatState.CreateCard<WuSeSha>(Owner);
            if (IsUpgraded) sha.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(sha, PileType.Hand, Owner, CardPilePosition.Random);
        }

    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
