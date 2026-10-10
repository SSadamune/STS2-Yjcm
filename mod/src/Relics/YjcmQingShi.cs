using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Yjcm;

/// <summary>
/// 初始遗物「情势」：每回合首次使用手中有同名牌的牌时触发效果
/// 攻击牌 → 额外造成一次等量伤害 / 技能牌 → 抽一张牌 / 能力牌 → 获得一点能量
/// </summary>
[RegisterRelic(typeof(SharedRelicPool))]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "STS001")]
public sealed class YjcmQingShi : ModRelicTemplate
{
    public bool UsedThisTurn { get; set; }

    public static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(YjcmQingShi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "qing_shi.wav");

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath =>
        "res://Yjcm/Images/Relics/wuzhugeliang-wu_zhugeliang_qing_shi.png";
    protected override string PackedIconOutlinePath =>
        "res://Yjcm/Images/Relics/wuzhugeliang-wu_zhugeliang_qing_shi.png";
    protected override string BigIconPath =>
        "res://Yjcm/Images/Relics/Big/wuzhugeliang-wu_zhugeliang_qing_shi.png";

    // 战斗开始：生成 5 个空栏位充能球（武器/防具/防御马/进攻马/宝物），显示装备区名字。
    // 装备真实装备时由 EquipCmd.ChannelEquipment 同类替换这些空球；
    // 非装备充能球由 OrbCmd.Channel 拦截补丁路由到宝物栏。
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        var player = Owner;
        if (player == null) return;
        var ctx = new ThrowingPlayerChoiceContext();
        // 空槽球静默生成（playSfx: false），不播放入栏音效
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyWeaponSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyArmorSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyDefensiveHorseSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyOffensiveHorseSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyTreasureSlotOrb>().ToMutable(), false);
    }

    // 每回合开始时重置使用标记
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        // 仅在己方回合重置，敌方回合不处理
        if (side == CombatSide.Player)
            UsedThisTurn = false;
    }

    /// <summary>
    /// 正常摸牌完成后，将带有赏誉关键词的牌从抽牌堆或弃牌堆加入手牌。
    /// 使用已被战斗流程等待的回合钩子，避免异步补丁与正常摸牌交错导致联机状态分歧。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player != Owner)
            return;

        var drawPile = PileTypeExtensions.GetPile(PileType.Draw, player);
        var discardPile = PileTypeExtensions.GetPile(PileType.Discard, player);
        var shangYuCards = new List<CardModel>();

        if (drawPile?.Cards != null)
            shangYuCards.AddRange(drawPile.Cards.Where(c => c.Keywords.Contains(YjcmKeywords.ShangYu)));
        if (discardPile?.Cards != null)
            shangYuCards.AddRange(discardPile.Cards.Where(c => c.Keywords.Contains(YjcmKeywords.ShangYu)));

        foreach (var card in shangYuCards)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);

        // 暮锐必须在游戏正在等待的回合钩子中执行，确保多人校验和生成前两端
        // 已完成完全相同的自动出牌流程。
        await MuRuiPatch.AutoPlayAtTurnStart(choiceContext, player);
    }

    // 同名牌检测 + 效果触发
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);

        if (UsedThisTurn || cardPlay.Card == null)
            return;

        var card = cardPlay.Card;
        var owner = card.Owner;
        if (owner == null)
            return;
        // 多人模式只响应持有者（武诸葛）自己打出的牌，队友打出的牌不触发遗物
        if (owner != Owner)
            return;

        var handPile = PileTypeExtensions.GetPile(PileType.Hand, owner);
        if (handPile?.Cards == null)
            return;

        var playedId = card.Id;
        bool hasDuplicate = false;
        foreach (var c in handPile.Cards)
        {
            if (c != card && c.Id.Entry == playedId.Entry)
            {
                hasDuplicate = true;
                break;
            }
        }
        if (!hasDuplicate)
            return;

        UsedThisTurn = true;
        VoicePlayer.PlayWav(VoicePath);

        switch (card.Type)
        {
            case CardType.Attack:
                await DoAttackEffect(cardPlay, owner);
                break;
            case CardType.Skill:
                await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, owner, false);
                break;
            case CardType.Power:
                await PlayerCmd.GainEnergy(1, owner);
                break;
        }
    }

    // 额外造成一次等量伤害
    private async Task DoAttackEffect(CardPlay cardPlay, Player owner)
    {
        if (cardPlay.Target == null) return;
        // 安全获取 Damage，避免无此动态变量的卡牌抛异常
        var vars = cardPlay.Card.DynamicVars;
        decimal amount;
        try { amount = vars.Damage?.BaseValue ?? 6m; }
        catch (KeyNotFoundException) { amount = 6m; }
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            cardPlay.Target,
            amount,
            ValueProp.Move,
#if GAME_BETA
            cardPlay.Card,
            cardPlay
#else
            cardPlay.Card
#endif
        );
    }
}
