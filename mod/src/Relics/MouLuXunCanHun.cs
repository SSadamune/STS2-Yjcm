using STS2RitsuLib.Interactions.RightClick;
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

#nullable enable

namespace Yjcm;

/// <summary>
/// 先古之民遗物「谋陆逊残魂」：Ancient 稀有度。
/// 使用牌指定自己时，摸一张牌；指定敌人时，若为攻击牌使其获得三层火攻，若为非攻击牌使其获得三层铁索连环（不限次数）。
/// 右键此遗物（每场战斗限一次）：使所有敌人获得等同于当前手牌数的火攻，
/// 然后依次弃置所有手牌（每弃一张触发一次火攻伤害），再摸等量的牌。
/// </summary>
[RegisterRelic(typeof(SharedRelicPool))]
public sealed class MouLuXunCanHun : ModRelicTemplate, IModRightClickableRelic
{
    /// <summary>右键每场战斗限一次标记（与出牌效果完全独立）</summary>
    private bool _rightClickUsedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override string PackedIconPath =>
        "res://Yjcm/Images/Relics/mou_lu_xun_can_hun.png";
    protected override string PackedIconOutlinePath =>
        "res://Yjcm/Images/Relics/mou_lu_xun_can_hun.png";
    protected override string BigIconPath =>
        "res://Yjcm/Images/Relics/Big/mou_lu_xun_can_hun.png";

    public override Task BeforeCombatStart()
    {
        _rightClickUsedThisCombat = false;
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 打出牌时（不限次数，不读任何限次标记）：
    /// 指定自己 → 摸一张牌；指定敌人 → 攻击牌上三层火攻 / 非攻击牌上三层铁索连环。
    /// 注：TargetType.Self 的牌打出时 cardPlay.Target 为 null（无目标选择），
    /// 必须用 card.TargetType 判断，不能用 Target 引用。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (Owner == null) return;
        var card = cardPlay.Card;
        if (card?.Owner != Owner) return; // 多人：只触发自己打出的牌

        // 指定自己（TargetType.Self）：摸一张牌
        if (card.TargetType == TargetType.Self)
        {
            MainFile.Logger.Info($"[谋陆逊] 打出牌指定自己(Self): {card.Id.Entry} → 摸1张");
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
            return;
        }

        // 指定敌人（任意/全体/随机敌人）：攻击牌 → 三层火攻；非攻击牌 → 三层铁索连环
        if (card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy)
        {
            if (card.Type == CardType.Attack)
            {
                MainFile.Logger.Info($"[谋陆逊] 打出攻击牌 {card.Id.Entry} → 敌人 3 层火攻");
                await PowerCmd.Apply<HuoGongPower>(choiceContext, cardPlay.Target!, 3, Owner.Creature, card, false);
            }
            else
            {
                MainFile.Logger.Info($"[谋陆逊] 打出非攻击牌 {card.Id.Entry} → 敌人 3 层连环");
                await PowerCmd.Apply<LianHuanPower>(choiceContext, cardPlay.Target!, 3, Owner.Creature, card, false);
            }
        }
    }

    /// <summary>右键（每场战斗限一次）：全体敌人获得=手牌数的火攻，弃置全部手牌（触发火攻）并摸等量牌</summary>
    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (_rightClickUsedThisCombat) return; // 每场战斗限一次（仅右键）
        var player = context.Player;
        if (player == null || player.Creature.CombatState == null) return; // 仅战斗内可用
        var ctx = context.PlayerChoiceContext;
        if (ctx == null) return;

        var hand = PileTypeExtensions.GetPile(PileType.Hand, player)?.Cards.ToList() ?? [];
        if (hand.Count == 0) return;

        _rightClickUsedThisCombat = true; // 触发后本场战斗不可再右键
        MainFile.Logger.Info($"[谋陆逊] 右键触发（本场限一次）：手牌 {hand.Count} 张 → 全体火攻 {hand.Count} 层");

        // 所有敌人获得与当前手牌数等量的火攻（source 传 null：遗物非卡牌来源）
        foreach (var enemy in player.Creature.CombatState.HittableEnemies)
            await PowerCmd.Apply<HuoGongPower>(ctx, enemy, hand.Count, player.Creature, null, false);

        // 依次弃置所有手牌（每弃一张触发一次火攻伤害），副本遍历避免集合被修改
        foreach (var c in hand)
            await CardCmd.Discard(ctx, c);

        // 摸等量的牌
        await CardPileCmd.Draw(ctx, hand.Count, player, false);
    }
}
