using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 借东风：7费稀有攻击牌。造成{Damage}点伤害，获得{HuoGong}层火攻。
/// 本场战斗每获得或消耗一颗星，费用减一。升级后目标改为所有敌人。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class JieDongFeng : WuZhugeliangCard
{
    private const string HgKey = "HuoGong";

    public JieDongFeng()
        : base(7, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<HuoGongPower>(), HoverTipFactory.FromPower<XingPower>(),
         WuJiangHoverTip.ForFaction(YjcmCardTags.Shu)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(77, ValueProp.Move),
        new DynamicVar(HgKey, 7)
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(JieDongFeng).Assembly.Location)!,
        "Voice", "Bank", "Assets", "jie_dong_feng.wav");

    /// <summary>
    /// 每次所属玩家的星数量发生变化时立即减费。
    /// 使用游戏原生、会被等待的能力变化钩子，避免静态监听列表漏注册或串玩家。
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power is not XingPower || power.Owner != Owner.Creature || amount == 0)
            return;

        EnergyCost.AddThisCombat(-(int)Math.Abs(amount));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var dmg = (int)DynamicVars.Damage.BaseValue;
        var hgStacks = (int)DynamicVars[HgKey].BaseValue;
        var targets = IsUpgraded
            ? CombatState.HittableEnemies.ToList()
            : [cardPlay.Target!];

#if GAME_BETA
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, cardPlay);
#else
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, this);
#endif
        var results = new List<DamageResult>();
        foreach (var target in targets)
        {
#if GAME_BETA
            results.AddRange(await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Move, this, cardPlay));
#else
            results.AddRange(await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Move, this));
#endif
            await PowerCmd.Apply<HuoGongPower>(choiceContext, target, hgStacks, Owner.Creature, this, false);
        }
        attack.AddHit(results);
    }

    protected override void OnUpgrade() { }
}
