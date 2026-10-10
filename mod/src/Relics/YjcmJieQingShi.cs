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
/// 遗物「界情势」：情势升级版，每回合限两次
/// 攻击牌 → 额外伤害 / 技能牌 → 抽牌 / 能力牌 → 加能
/// </summary>
[RegisterRelic(typeof(SharedRelicPool))]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "STS001")]
public sealed class YjcmJieQingShi : ModRelicTemplate
{
    private int _usedThisTurn;

    public static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(YjcmJieQingShi).Assembly.Location)!,
        "Voice", "Bank", "Assets", "jie_qing_shi.wav");

    public override RelicRarity Rarity => RelicRarity.Starter;

    // 复用情势的图标
    public override string PackedIconPath =>
        "res://Yjcm/Images/Relics/wuzhugeliang-wu_zhugeliang_qing_shi.png";
    protected override string PackedIconOutlinePath =>
        "res://Yjcm/Images/Relics/wuzhugeliang-wu_zhugeliang_qing_shi.png";
    protected override string BigIconPath =>
        "res://Yjcm/Images/Relics/Big/wuzhugeliang-wu_zhugeliang_qing_shi.png";

    // 界情势由情势升级而来，也必须建立完整的五个装备栏。
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        var player = Owner;
        if (player == null) return;

        var ctx = new ThrowingPlayerChoiceContext();
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyWeaponSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyArmorSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyDefensiveHorseSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyOffensiveHorseSlotOrb>().ToMutable(), false);
        await EquipCmd.ChannelEquipment(ctx, player, (EquipmentOrbModel)ModelDb.Orb<EmptyTreasureSlotOrb>().ToMutable(), false);
    }

    /// <summary>
    /// 界情势替代情势后仍需保留暮锐的回合开始自动出牌，并从战斗流程提供的
    /// await 钩子中执行，避免多人模式在校验和生成后才修改牌堆。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner)
            await MuRuiPatch.AutoPlayAtTurnStart(choiceContext, player);
    }

    // 每回合开始重置计数
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side == CombatSide.Player)
            _usedThisTurn = 0;
    }

    // 同名牌检测 + 效果触发（每回合限两次）
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);

        if (_usedThisTurn >= 2 || cardPlay.Card == null)
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

        _usedThisTurn++;
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

    private async Task DoAttackEffect(CardPlay cardPlay, Player owner)
    {
        if (cardPlay.Target == null) return;
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
            null
#else
            cardPlay.Card
#endif
        );
    }
}
