using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Runtime.CompilerServices;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 威张星彩-煌怒：2费稀有攻击牌，具有保留。
/// 受到伤害时若在手牌中，自动打出：造成{Damage}点伤害，使敌人失去{StrengthLoss}点力量，恢复一个装备栏。
/// 升级后1费、15伤害。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class WeiZhangXingCaiHuangNu : WuZhugeliangCard
{
    private const string StrengthLossKey = "StrengthLoss";
    private static readonly ConditionalWeakTable<DamageResult, object> HandledDamage = new();

    public WeiZhangXingCaiHuangNu()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    // 武将+蜀 tag：纳入武将牌相关机制（天任、尽瘁等）
    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12, ValueProp.Move),
        new DynamicVar(StrengthLossKey, 2m)
    };

    // 为描述中的 [力量] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(WeiZhangXingCaiHuangNu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "huang_nu.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif

        // 敌人失去1点力量（永久：StrengthPower 负值=永久降低；ManglePower 是临时）
        await PowerCmd.Apply<StrengthPower>(choiceContext,
            cardPlay.Target!, -DynamicVars[StrengthLossKey].BaseValue, Owner.Creature, this, false);

        // 恢复一个装备栏
        await EquipCmd.RestoreOneSlot(choiceContext, Owner);
    }

    /// <summary>
    /// 受到未被格挡的伤害时自动打出手牌中的第一张煌怒。
    /// 直接使用游戏正在等待的选择上下文，确保自动出牌属于当前战斗动作，
    /// 主机与客户端会在同一个校验点完成结算。
    /// </summary>
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);

        if (CombatManager.Instance.IsOverOrEnding || result.UnblockedDamage <= 0)
            return;
        if (target != Owner.Creature || Pile?.Type != PileType.Hand)
            return;

        var hand = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        var firstHuangNu = hand?.Cards.OfType<WeiZhangXingCaiHuangNu>().FirstOrDefault();
        if (!ReferenceEquals(firstHuangNu, this))
            return;
        if (HandledDamage.TryGetValue(result, out _))
            return;

        var hittable = Owner.Creature.CombatState?.HittableEnemies.ToList();
        Creature? autoTarget = null;
        if (dealer != null && hittable?.Contains(dealer) == true)
            autoTarget = dealer;
        else if (hittable is { Count: > 0 })
            autoTarget = Owner.RunState.Rng.CombatCardSelection.NextItem(hittable);

        if (autoTarget != null)
        {
            HandledDamage.Add(result, new object());
            await CardCmd.AutoPlay(choiceContext, this, autoTarget,
                AutoPlayType.Default, false, false);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);      // 2→1
        DynamicVars.Damage.UpgradeValueBy(3); // 12→15
    }
}
