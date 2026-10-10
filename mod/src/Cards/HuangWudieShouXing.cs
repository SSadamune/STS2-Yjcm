using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Yjcm.Extensions;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 黄舞蝶-狩星：0费攻击Token。消耗，造成6点无法格挡的伤害。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class HuangWudieShouXing : CardModel
{
    public override string PortraitPath =>
        "huang_wudie_shou_xing.png".CardImagePath();

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move | ValueProp.Unblockable)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(HuangWudieShouXing).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shou_xing.wav");

    public HuangWudieShouXing()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, shouldShowInCardLibrary: false) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
#if GAME_BETA
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, cardPlay);
#else
        await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, this);
#endif
        attack.AddHit(await CreatureCmd.Damage(choiceContext, cardPlay.Target!,
#if GAME_BETA
            DynamicVars.Damage.BaseValue, ValueProp.Move | ValueProp.Unblockable, this, cardPlay));
#else
            DynamicVars.Damage.BaseValue, ValueProp.Move | ValueProp.Unblockable, this));
#endif
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2); // 6→8
    }
}
