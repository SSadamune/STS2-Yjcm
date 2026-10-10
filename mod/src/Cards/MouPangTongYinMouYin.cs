using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>谋庞统-寅谋-阴：1费罕见技能牌，转换技、奇巧。所有敌人失去2→3点力量。</summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouPangTongYinMouYin : WuZhugeliangCard
{
    public MouPangTongYinMouYin()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi, CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(2m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouPangTongYinMouYin).Assembly.Location)!,
        "Voice", "Bank", "Assets", "yin_mou_yin.wav");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromCard<MouPangTongYinMouYang>(IsUpgraded),
         HoverTipFactory.FromPower<StrengthPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var loss = DynamicVars[typeof(StrengthPower).Name].BaseValue;
        foreach (var enemy in CombatState.HittableEnemies.ToList())
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, -loss, Owner.Creature, this, false);

        var yangCard = CombatState.CreateCard<MouPangTongYinMouYang>(Owner);
        if (IsUpgraded) yangCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yangCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[typeof(StrengthPower).Name].UpgradeValueBy(1);
    }
}
