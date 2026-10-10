using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 的卢（防御马）：每回合受到的第一次突破格挡的伤害时，无效溢出的部分。
/// 继承防御马被动：获得敏捷时额外获得一点。
/// </summary>
[RegisterOrb]
public sealed class DiLuOrb : DefensiveHorseOrb
{
    public override Color DarkenedColor => new("6B4A9E");

    private const string IconRes = "res://Yjcm/Images/Powers/di_lu.png";

    public override OrbAssetProfile AssetProfile => new(IconPath: IconRes, VisualsScenePath: null);

    protected override Node2D? TryCreateOrbSprite() => EquipmentOrbSprite.CreateNormalized(IconRes, 54f);

    // 本回合是否已触发过「无效溢出」
    public bool UsedThisTurn { get; private set; }

    // 本次受击前玩家是否有格挡（ModifyHpLost 在格挡消耗后才执行，需提前记录）
    private bool _hadBlock;

    // 玩家回合开始时重置
    public override Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        UsedThisTurn = false;
        return Task.CompletedTask;
    }

    // 记录本次受击前是否有格挡
    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource);
        // 只记录持有者（武诸葛）自己受击时的格挡状态，队友受击不污染标记
        if (target != Owner.Creature) return;
        _hadBlock = target.Block > 0;
    }

    // 只有「有格挡且伤害突破格挡」时才无效溢出；无格挡不生效
    public override decimal ModifyHpLostBeforeOsty(
        Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return base.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource);
        if (UsedThisTurn) return base.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource);
        if (!_hadBlock) return base.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource); // 无格挡不生效
        if (amount <= 0) return base.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource);

        UsedThisTurn = true;
        _hadBlock = false;
        return 0m;
    }

    // 备份：ModifyHpLost 未触发时，仅当「有格挡且突破」才回补溢出（无格挡同样不生效）
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target != Owner.Creature) return;
        if (UsedThisTurn) return;
        if (result.BlockedDamage <= 0 || result.OverkillDamage <= 0) return; // 必须有格挡且突破

        UsedThisTurn = true;
        await CreatureCmd.Heal(Owner.Creature, result.OverkillDamage);
    }
}
