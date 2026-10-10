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

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 先古之民遗物「任婉残魂」：Ancient 稀有度。
/// 回合开始将手牌摸至手牌上限；下一张牌免费打出（本回合）；
/// 回合结束弃置所有手牌，每弃置一张随机对敌人造成 5 点伤害。
/// </summary>
[RegisterRelic(typeof(SharedRelicPool))]
public sealed class RenWanCanHun : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override string PackedIconPath =>
        "res://Yjcm/Images/Relics/ren_wan_can_hun.png";
    protected override string PackedIconOutlinePath =>
        "res://Yjcm/Images/Relics/ren_wan_can_hun.png";
    protected override string BigIconPath =>
        "res://Yjcm/Images/Relics/Big/ren_wan_can_hun.png";

    /// <summary>回合开始：摸至手牌上限 + 下一张牌免费（本回合）</summary>
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side != CombatSide.Player || Owner == null) return;
        var ctx = new ThrowingPlayerChoiceContext();

        // 摸至手牌上限（CardPile.MaxCardsInHand，玩家环境为 10）
        var handCount = PileTypeExtensions.GetPile(PileType.Hand, Owner)?.Cards.Count ?? 0;
        var toDraw = Math.Max(0, CardPile.MaxCardsInHand - handCount);
        if (toDraw > 0)
            await CardPileCmd.Draw(ctx, toDraw, Owner, fromHandDraw: false);

        // 下一张牌免费：先清再上（防叠加），本回合有效
        var existing = Owner.Creature.Powers?.OfType<RenWanFreePower>().FirstOrDefault();
        if (existing != null) await PowerCmd.Remove(existing);
        await PowerCmd.Apply<RenWanFreePower>(ctx, Owner.Creature, 1, Owner.Creature, null, false);
    }

    /// <summary>
    /// 回合结束：弃置所有手牌，每弃一张随机敌人受 5 点伤害。
    /// 必须用 BeforeSideTurnEnd（AfterSideTurnEnd 时手牌已自动冲入弃牌堆）。
    /// </summary>
    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (side != CombatSide.Player || Owner == null) return;
        // 只在自己回合末触发（多人：所有玩家共享玩家侧）
        if (!participants.Contains(Owner.Creature)) return;

        var handCards = PileTypeExtensions.GetPile(PileType.Hand, Owner)?.Cards.ToList() ?? [];
        if (handCards.Count == 0) return;
        var enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return;

        foreach (var c in handCards)
        {
            await CardCmd.Discard(choiceContext, c);
            var target = Owner.RunState.Rng.CombatCardSelection.NextItem(enemies);
            if (target != null)
                await CreatureCmd.Damage(choiceContext, target, 5m, ValueProp.Move, null, null);
        }
    }
}
