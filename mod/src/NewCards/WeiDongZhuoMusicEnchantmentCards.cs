using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// 音律附魔的首回合施加牌。它们以固有确保进入起始手牌，并在第一个玩家回合开始时自动打出。
/// 卡牌在结算前已离开手牌，因此“当前所有手牌”不会包含这张即将消耗的来源牌。
/// </summary>
public abstract class WeiDongZhuoMusicEnchantmentCard : WeiDongZhuoQunCard
{
    private bool _autoPlayedThisCombat;

    protected WeiDongZhuoMusicEnchantmentCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected abstract string VoiceFile { get; }
    protected abstract bool CanApplyMusicEnchantment(CardModel card);
    protected abstract void ApplyMusicEnchantment(CardModel card);

    public override int MaxUpgradeLevel => 0;
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WeiDongZhuoQunEffects.PlayVoice(VoiceFile);
        foreach (var card in PileType.Hand.GetPile(Owner).Cards.Where(card => card != this).ToList())
        {
            // 状态与诅咒等不符合原生附魔资格的牌必须跳过；
            // 否则 CardCmd.Enchant 会抛异常并中断首回合自动打出流程。
            if (!CanApplyMusicEnchantment(card))
                continue;

            // 合格牌才覆盖其原有附魔，不能因目标无效而清掉旧附魔。
            if (card.Enchantment != null)
                CardCmd.ClearEnchantment(card);
            ApplyMusicEnchantment(card);
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (_autoPlayedThisCombat || player != Owner || Pile?.Type != PileType.Hand ||
            Owner.Creature.CombatState?.RoundNumber != 1 || CombatManager.Instance.IsOverOrEnding)
            return;

        _autoPlayedThisCombat = true;
        await CardCmd.AutoPlay(choiceContext, this, null, AutoPlayType.Default, false, false);
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _autoPlayedThisCombat = false;
    }
}

[RegisterCard(typeof(WeiDongZhuoCardPool), FullPublicEntry = "YJCM_CARD_LE_CAI_WEN_JI_SHUANG_QIE")]
public sealed class WeiDongZhuoLeCaiWenJiShuangQie : WeiDongZhuoMusicEnchantmentCard
{
    protected override string VoiceFile => "shuang_qie.wav";
    public override string CustomPortraitPath => "res://WeiDongZhuo/images/shuang_qie.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<HuJia>();

    protected override bool CanApplyMusicEnchantment(CardModel card) =>
        ModelDb.Enchantment<HuJia>().CanEnchant(card);

    protected override void ApplyMusicEnchantment(CardModel card) =>
        CardCmd.Enchant<HuJia>(card, 1m);
}

[RegisterCard(typeof(WeiDongZhuoCardPool), FullPublicEntry = "YJCM_CARD_LE_CAI_YONG_JIAO_WEI")]
public sealed class WeiDongZhuoLeCaiYongJiaoWei : WeiDongZhuoMusicEnchantmentCard
{
    protected override string VoiceFile => "jiao_wei.wav";
    public override string CustomPortraitPath => "res://WeiDongZhuo/images/jiao_wei.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<JiaoWei>();

    protected override bool CanApplyMusicEnchantment(CardModel card) =>
        ModelDb.Enchantment<JiaoWei>().CanEnchant(card);

    protected override void ApplyMusicEnchantment(CardModel card) =>
        CardCmd.Enchant<JiaoWei>(card, 1m);
}

[RegisterCard(typeof(WeiDongZhuoCardPool), FullPublicEntry = "YJCM_CARD_LE_DIAO_CHAN_TAN_BAN")]
public sealed class WeiDongZhuoLeDiaoChanTanBan : WeiDongZhuoMusicEnchantmentCard
{
    protected override string VoiceFile => "tan_ban.wav";
    public override string CustomPortraitPath => "res://WeiDongZhuo/images/tan_ban.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<TanBan>();

    protected override bool CanApplyMusicEnchantment(CardModel card) =>
        ModelDb.Enchantment<TanBan>().CanEnchant(card);

    protected override void ApplyMusicEnchantment(CardModel card) =>
        CardCmd.Enchant<TanBan>(card, 1m);
}
