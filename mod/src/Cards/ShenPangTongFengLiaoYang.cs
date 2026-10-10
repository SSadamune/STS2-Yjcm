using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.HoverTips;
namespace Yjcm;

/// <summary>
/// 神庞统-凤燎-阳：1费技能牌，稀有。全体5层连环+摸1张牌，打出后生成阴形态入抽牌堆。升级后0费。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ShenPangTongFengLiaoYang : WuZhugeliangCard
{
    public ShenPangTongFengLiaoYang()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenPangTongFengLiaoYang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shen_pang_tong_feng_liao_yang.wav");

    // 为描述中的 [转换技]/[连环] 提供悬浮提示；并显示转换后的阴形态
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromPower<LianHuanPower>(),
         HoverTipFactory.FromCard<ShenPangTongFengLiaoYin>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var enemies = CombatState.HittableEnemies.ToList();
        foreach (var enemy in enemies)
            await PowerCmd.Apply<LianHuanPower>(choiceContext, enemy, 5, Owner.Creature, this, false);

        await CardPileCmd.Draw(choiceContext, 1, Owner);

        // 生成阴形态加入抽牌堆
        var yinCard = Owner.Creature.CombatState.CreateCard<ShenPangTongFengLiaoYin>(Owner);
        if (IsUpgraded) yinCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yinCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetThisCombat(0);
    }
}
