using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.HoverTips;
namespace Yjcm;

/// <summary>
/// 庞宏-评骘-阳：1费技能牌，转换技。2→3层虚弱+易伤，随机打出手牌中一张技能牌
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class PangHongPingZhiYang : WuZhugeliangCard
{
    public PangHongPingZhiYang()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m)
    ];

    private static bool _autoPlaying;

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(PangHongPingZhiYang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "pang_hong_ping_zhi_yang.wav");

    // 为描述中的 [gold]转换技[/gold] 提供悬浮提示；并显示转换后的阴形态
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromCard<PangHongPingZhiYin>(IsUpgraded)];
    // 在卡牌打出前标记消耗，实现「转换技」消耗效果但不显示消耗标签
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var target = cardPlay.Target!;

        // 施加虚弱和易伤
        var amount = DynamicVars[typeof(WeakPower).Name].BaseValue;
        await PowerCmd.Apply<WeakPower>(choiceContext, target, amount, Owner.Creature, this, false);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, amount, Owner.Creature, this, false);

        // 随机打出手牌中的一张技能牌（防递归）
        if (!_autoPlaying)
        {
            var handPile = PileTypeExtensions.GetPile(PileType.Hand, Owner);
            if (handPile?.Cards != null)
            {
                var skillCards = handPile.Cards
                    .Where(c => c != this && c.Type == CardType.Skill)
                    .ToList();

                if (skillCards.Count > 0)
                {
                    var randomCard = Owner.RunState.Rng.CombatCardSelection.NextItem(skillCards);
                    if (randomCard != null)
                    {
                        _autoPlaying = true;
                        try { await CardCmd.AutoPlay(choiceContext, randomCard, target, AutoPlayType.Default, false, false); }
                        finally { _autoPlaying = false; }
                    }
                }
            }
        }

        // 转换技：将阴形态加入抽牌堆（保留升级）
        var yinCard = CombatState.CreateCard<PangHongPingZhiYin>(Owner);
        if (IsUpgraded) yinCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yinCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[typeof(WeakPower).Name].UpgradeValueBy(1);
        DynamicVars[typeof(VulnerablePower).Name].UpgradeValueBy(1);
    }
}
