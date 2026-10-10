using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 谋法正-悉性：0费罕见攻击牌。造成{Damage}点伤害，
/// 下一张牌造成伤害后，此卡返回手牌。升级后4→6伤。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouFaZhengXiXing : WuZhugeliangCard
{
    public MouFaZhengXiXing()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouFaZhengXiXing).Assembly.Location)!,
        "Voice", "Bank", "Assets", "xi_xing.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 造成伤害，捕获攻击命令以读取实际造成的伤害
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!);
#else
            .FromCard(this).Targeting(cardPlay.Target!);
#endif
        await attack.Execute(choiceContext);

        // 实际造成伤害 = 各次命中的 TotalDamage 之和（含力量/易伤等修正）
        var actualDamage = attack.Results.SelectMany(hit => hit).Sum(r => r.TotalDamage);

        // 给予玩家悉性能力，绑定此卡实例及伤害阈值（取实际造成伤害，而非纸面数值）
        var power = await PowerCmd.Apply<XiXingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        // 最后一名敌人死亡等情况下，能力施加会被跳过并返回 null。
        if (power == null) return;
        power.SourceCard = this;
        power.ThresholdDamage = actualDamage;
    }

    /// <summary>升级：4→6</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
