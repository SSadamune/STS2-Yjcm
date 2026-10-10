using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 桃：0费罕见技能牌。消耗，回复{Heal}点血量，升级后7→10。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class Tao : WuZhugeliangCard
{
    private const string HealKey = "Heal";

    public Tao()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(HealKey, 7m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars[HealKey].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HealKey].UpgradeValueBy(3); // 7→10
    }
}
