using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// �����-˫��2�ѹ����� 8/11�˺� ���佫������
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class HuangWudieShuangRui : WuZhugeliangCard
{
    public HuangWudieShuangRui()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<HuangWudieShouXing>(IsUpgraded),
         HoverTipFactory.FromCard<HuangWudieShaXue>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
#if GAME_BETA
            .FromCard(this, cardPlay).Targeting(cardPlay.Target!).Execute(choiceContext);
#else
            .FromCard(this).Targeting(cardPlay.Target!).Execute(choiceContext);
#endif
        var results = attack.Results.SelectMany(hit => hit).ToList();
        var blocked = results.Sum(r => r.BlockedDamage) > 0
            && results.Sum(r => r.TotalDamage) == results.Sum(r => r.BlockedDamage);

        CardModel derivedCard = blocked
            ? Owner.Creature.CombatState.CreateCard<HuangWudieShouXing>(Owner)
            : Owner.Creature.CombatState.CreateCard<HuangWudieShaXue>(Owner);
        if (IsUpgraded) derivedCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(derivedCard, PileType.Hand, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(HuangWudieShuangRui).Assembly.Location)!,
        "Voice", "Bank", "Assets", "shuang_rui.wav");
}
