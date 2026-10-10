using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 神马超-狩骊：1费稀有攻击牌。造成{Damage}点伤害，本回合你与该怪物受到的伤害变为2倍。
/// 升级后获得保留。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ShenMaChaoShouLi : WuZhugeliangCard
{
    public ShenMaChaoShouLi()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move)];

    // 狩骊 + 力量悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<EnemyShouLiPower>(),
         HoverTipFactory.FromPower<StrengthPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenMaChaoShouLi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shou_li.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 先给目标上狩骊（敌人→敌人回合结束时消失）
        await PowerCmd.Apply<EnemyShouLiPower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this, false);

        // 给自己上狩骊（玩家→敌人回合结束时消失，即下回合开始）
        await PowerCmd.Apply<PlayerShouLiPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);

        // 额外造成等同于目标力量数的伤害
        var str = cardPlay.Target!.Powers?.OfType<StrengthPower>().FirstOrDefault()?.Amount ?? 0;

        // 再造成伤害（此时已受狩骊翻倍）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + str)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif
    }

    /// <summary>升级：获得保留</summary>
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
