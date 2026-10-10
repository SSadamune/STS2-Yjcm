// Token card inheriting CardModel directly (not ModCardTemplate)
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Yjcm.Extensions;
using MegaCrit.Sts2.Core.HoverTips;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Yjcm;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "STS001")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "STS003")]
[RegisterCard(typeof(TokenCardPool))]
public sealed class PangHongPingZhiYin : CardModel
{
    public override string PortraitPath =>
        "pang_hong_ping_zhi_yin.png".CardImagePath();

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [YjcmKeywords.ZhuanHuanJi];

    private const string MultiplierKey = "Multiplier";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(MultiplierKey, 2m)];

    private static bool _autoPlaying;

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(PangHongPingZhiYin).Assembly.Location)!,
        "Voice", "Bank", "Assets", "pang_hong_ping_zhi_yin.wav");

    public PangHongPingZhiYin()
        : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, shouldShowInCardLibrary: false) { }

    // 为描述中的 [gold]转换技[/gold] 提供悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi),
         HoverTipFactory.FromCard<PangHongPingZhiYang>(IsUpgraded),
         WuJiangHoverTip.ForFaction(YjcmCardTags.Shu)];
    // 在卡牌打出前标记消耗，实现「转换技」消耗效果但不显示消耗标签
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var target = cardPlay.Target!;

        var weakAmt = target.Powers?.OfType<WeakPower>().FirstOrDefault()?.Amount ?? 0;
        var vulnAmt = target.Powers?.OfType<VulnerablePower>().FirstOrDefault()?.Amount ?? 0;
        var multiplier = (int)DynamicVars[MultiplierKey].BaseValue;
        var damage = (weakAmt + vulnAmt) * multiplier;

        // 即使基础伤害为0，活力/力量仍可赋予这次攻击伤害。
#if GAME_BETA
        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target).Execute(choiceContext);
#else
        await DamageCmd.Attack(damage).FromCard(this).Targeting(target).Execute(choiceContext);
#endif

        if (!_autoPlaying)
        {
            var handPile = PileTypeExtensions.GetPile(PileType.Hand, Owner);
            if (handPile?.Cards != null)
            {
                var atkCards = handPile.Cards.Where(c => c != this && c.Type == CardType.Attack).ToList();
                if (atkCards.Count > 0)
                {
                    var randomCard = Owner.RunState.Rng.CombatCardSelection.NextItem(atkCards);
                    if (randomCard != null)
                    {
                        _autoPlaying = true;
                        try { await CardCmd.AutoPlay(choiceContext, randomCard, target, AutoPlayType.Default, false, false); }
                        finally { _autoPlaying = false; }
                    }
                }
            }
        }

        var yangCard = CombatState.CreateCard<PangHongPingZhiYang>(Owner);
        if (IsUpgraded) yangCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(yangCard, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MultiplierKey].UpgradeValueBy(1);
    }
}
