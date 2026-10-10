using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>武关羽-武佑：2费罕见技能牌，奇巧。从武诸葛卡池的5张随机牌中选择1→2张。</summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class WuGuanYuWuYou : WuZhugeliangCard
{
    private const int CandidateCount = 5;
    private const string ChoiceCountKey = "ChoiceCount";

    public WuGuanYuWuYou()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new(ChoiceCountKey, 1m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(WuGuanYuWuYou).Assembly.Location)!,
        "Voice", "Bank", "Assets", "wu_you.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        // 只从当前武诸葛亮角色可获得的卡池挑选；排除衍生牌和不在图鉴展示的牌。
        var candidates = Owner.Character.CardPool.GetUnlockedCards(
                Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity != CardRarity.Token && card.ShouldShowInCardLibrary)
            .ToList();
        if (candidates.Count == 0) return;

        var options = CardFactory.GetDistinctForCombat(
            Owner, candidates, Math.Min(CandidateCount, candidates.Count),
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (options.Count == 0) return;

        var choiceCount = (int)DynamicVars[ChoiceCountKey].BaseValue;
        var chosen = await CardSelectCmd.FromSimpleGrid(
            choiceContext, options, Owner,
            new CardSelectorPrefs(
                new LocString("cards", "YJCM_CARD_WU_GUAN_YU_WU_YOU.select_prompt"),
                choiceCount, choiceCount));

        foreach (var card in chosen)
        {
            card.SetToFreeThisTurn();
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ChoiceCountKey].UpgradeValueBy(1);
    }
}
