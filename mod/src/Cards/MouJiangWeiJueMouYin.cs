using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.HoverTips;
namespace Yjcm;

/// <summary>
/// 谋姜维-绝谋-阴：1费攻击牌，转换技。8→12伤害 + 随机敌人本回合失去4→6力量
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouJiangWeiJueMouYin : WuZhugeliangCard
{
    private const string StrengthLossKey = "StrengthLoss";

    public MouJiangWeiJueMouYin()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8, ValueProp.Move),
        new DynamicVar(StrengthLossKey, 4m)
    };

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouJiangWeiJueMouYin).Assembly.Location)!,
        "Voice", "Bank", "Assets", "mou_jiang_wei_yin.wav");

    // 为描述中的 [gold]转换技[/gold] 提供悬浮提示；并显示转换后的阳形态
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromCard<MouJiangWeiJueMouYang>(IsUpgraded)];
    // 在卡牌打出前标记消耗，实现「转换技」消耗效果但不显示消耗标签
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 对目标敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif

        // 随机敌人失去力量（本回合）→ 统一用「力竭」buff
        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count > 0)
        {
            var randomEnemy = Owner.RunState.Rng.CombatCardSelection.NextItem(enemies);
            if (randomEnemy != null)
            {
                await PowerCmd.Apply<LiJiePower>(choiceContext, 
                    randomEnemy, DynamicVars[StrengthLossKey].BaseValue, Owner.Creature, this, false);
            }
        }

        // 转换技：将阳形态加入抽牌堆（保留升级）
        var yangCard = CombatState.CreateCard<MouJiangWeiJueMouYang>(Owner);
        if (IsUpgraded) yangCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yangCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars[StrengthLossKey].UpgradeValueBy(2);
    }
}
