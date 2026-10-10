using MegaCrit.Sts2.Core.Commands;
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
/// 黄舞蝶-铩雪：0费攻击Token。消耗，造成4点伤害，抽1张牌。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class HuangWudieShaXue : CardModel
{
    public override string PortraitPath =>
        "huang_wudie_sha_xue.png".CardImagePath();

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(HuangWudieShaXue).Assembly.Location)!,
        "Voice", "Bank", "Assets", "sha_xue.wav");

    public HuangWudieShaXue()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, shouldShowInCardLibrary: false) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() { }
}
