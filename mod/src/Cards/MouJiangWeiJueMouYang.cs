using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;

using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.HoverTips;
namespace Yjcm;

/// <summary>
/// 谋姜维-绝谋-阳：1费技能牌，转换技。失去7→4HP，摸(已损失生命/7)→(+1)张牌
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouJiangWeiJueMouYang : WuZhugeliangCard
{
    private const string HpLossKey = "HpLoss";
    private const string DrawCountKey = "DrawCount";

    public MouJiangWeiJueMouYang()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(HpLossKey, 7m), new DynamicVar(DrawCountKey, 0m)];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        var drawCount = 0;
        // 图鉴展示使用 canonical card；canonical model 不能读取 Owner，
        // 否则会触发 CanonicalModelException 并导致图鉴卡牌网格崩溃。
        if (IsMutable && Owner?.Creature is { } creature)
        {
            var hpLoss = DynamicVars[HpLossKey].BaseValue;
            var hpAfterPlay = Math.Max(1m, creature.CurrentHp - hpLoss);
            var lostHp = creature.MaxHp - hpAfterPlay;
            drawCount = (int)(lostHp / 7m);
            if (IsUpgraded) drawCount++;
        }
        description.Add(DrawCountKey, drawCount);
    }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouJiangWeiJueMouYang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "mou_jiang_wei_yang.wav");

    // 为描述中的 [gold]转换技[/gold] 提供悬浮提示；并显示转换后的阴形态
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromCard<MouJiangWeiJueMouYin>(IsUpgraded)];
    // 在卡牌打出前标记消耗，实现「转换技」消耗效果但不显示消耗标签
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var creature = Owner.Creature;
        var hpLoss = DynamicVars[HpLossKey].BaseValue;

        // 失去生命值
        var newHp = creature.CurrentHp - hpLoss;
        if (newHp < 1) newHp = 1;
        await CreatureCmd.SetCurrentHp(creature, newHp);

        // 计算摸牌数：已损失生命 / 7（升级后 +1）
        var lostHp = creature.MaxHp - creature.CurrentHp;
        var drawCount = (int)(lostHp / 7);
        if (IsUpgraded) drawCount++;

        if (drawCount > 0)
        {
            var drawn = (await CardPileCmd.Draw(choiceContext, drawCount, Owner)).ToList();
            if (drawn.Any(card => card.Type == CardType.Skill))
                await PlayerCmd.GainEnergy(1, Owner);
        }

        // 转换技：将阴形态加入抽牌堆（保留升级）
        var yinCard = CombatState.CreateCard<MouJiangWeiJueMouYin>(Owner);
        if (IsUpgraded) yinCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yinCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HpLossKey].UpgradeValueBy(-3);
    }
}
