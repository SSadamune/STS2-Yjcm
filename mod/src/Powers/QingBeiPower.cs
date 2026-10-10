using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm;

/// <summary>
/// 擎北：回合开始选择一种卡牌类型，本回合只能使用该类型，每用一张获1能量。
/// </summary>
public sealed class QingBeiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/qing_bei.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/qing_bei.png";

    private CardType? _lockedType;

    public void SetLockedType(CardType? type) => _lockedType = type;

    // 阻止打出错类型的牌
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        // 多人模式下只限制持有者自己的牌，队友的牌不受陈式影响。
        if (card.Owner?.Creature != Owner)
            return true;
        if (_lockedType == null) return base.ShouldPlay(card, autoPlayType);
        return card.Type == _lockedType.Value;
    }

    // 每打出一张牌获得1能量
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        // 只响应持有者自己打出的牌，队友打出的牌不获得能量
        if (_lockedType != null && Owner?.Player != null
            && cardPlay.Card?.Owner?.Creature == Owner)
            await PlayerCmd.GainEnergy(1, Owner.Player);
    }

    // 回合开始：观星+选择类型
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature != Owner) return;

        var options = new List<CardModel>
        {
            Owner.CombatState.CreateCard<QingBeiAttack>(Owner.Player),
            Owner.CombatState.CreateCard<QingBeiSkill>(Owner.Player),
            Owner.CombatState.CreateCard<QingBeiPowerOption>(Owner.Player),
        };

        var chosen = await CardSelectCmd.FromSimpleGrid(choiceContext, options, player,
            new CardSelectorPrefs(
                new MegaCrit.Sts2.Core.Localization.LocString("cards", "qingbei.select_type"),
                0, 1));

        var picked = chosen.FirstOrDefault();
        if (picked is QingBeiAttack) _lockedType = CardType.Attack;
        else if (picked is QingBeiSkill) _lockedType = CardType.Skill;
        else if (picked is QingBeiPowerOption) _lockedType = CardType.Power;
        else _lockedType = null; // skip
    }
}
