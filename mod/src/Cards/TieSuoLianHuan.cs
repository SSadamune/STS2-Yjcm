using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 铁索连环：1费技能牌 所有敌人4/7层连环
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class TieSuoLianHuan : WuZhugeliangCard
{
    public TieSuoLianHuan()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LianHuanPower>(6)];

    // 为描述中的 [连环] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<LianHuanPower>()];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(TieSuoLianHuan).Assembly.Location)!,
        "Voice", "Bank", "Assets", "tie_suo_lian_huan.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var amount = DynamicVars["LianHuanPower"].BaseValue;
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<LianHuanPower>(choiceContext, enemy, amount, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["LianHuanPower"].UpgradeValueBy(3);
    }
}
