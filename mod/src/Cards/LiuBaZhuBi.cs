using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 刘巴-铸币：1费罕见技能牌。将{Count}张无中生有加入抽牌堆。升级后1→2张。
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class LiuBaZhuBi : WuZhugeliangCard
{
    private const string CountKey = "Count";

    public LiuBaZhuBi()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(CountKey, 1m)];

    // 为描述中的 [无中生有] 提供卡牌悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<WuZhongShengYou>(IsUpgraded)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(LiuBaZhuBi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "zhu_bi.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var count = (int)DynamicVars[CountKey].BaseValue;
        for (int i = 0; i < count; i++)
        {
            var card = Owner.Creature.CombatState.CreateCard<WuZhongShengYou>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Random);
        }
        await PlayerCmd.GainEnergy(1, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[CountKey].UpgradeValueBy(1); // 1→2
    }
}
