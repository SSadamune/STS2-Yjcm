using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 赤兔（进攻马）：每回合第一张攻击牌造成的伤害变为2倍。
/// 继承进攻马被动：获得力量时额外获得一点。
/// </summary>
[RegisterOrb]
public sealed class ChiTuOrb : OffensiveHorseOrb
{
    public override Color DarkenedColor => new("B34A6A");

    private const string IconRes = "res://Yjcm/Images/Powers/chi_tu.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes, 54f);

    // 本回合第一张攻击牌是否已打出
    private bool _firstAttackDone;

    // 当前攻击牌是否应翻倍
    private bool _doubleNext;

    // 玩家回合开始时重置
    public override Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        _firstAttackDone = false;
        _doubleNext = false;
        return Task.CompletedTask;
    }

    // 打出第一张攻击牌时，标记本次伤害翻倍
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (_firstAttackDone) return;

        _firstAttackDone = true;
        _doubleNext = true;
    }

    // 牌结算后清除翻倍标记
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        _doubleNext = false;
    }

    public override decimal ModifyDamageMultiplicative(
#if GAME_BETA
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (dealer != Owner.Creature) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (!_doubleNext) return 1m;
        return 2m;
    }
}
