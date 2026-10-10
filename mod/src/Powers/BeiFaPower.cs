using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Yjcm;

/// <summary>
/// 北伐：下回合开始时获得能量、抽牌、力量、敏捷，下回合结束时死亡。
/// 数值由卡牌端在 Apply 后设置。
/// </summary>
public sealed class BeiFaPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/bei_fa.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/bei_fa.png";

    /// <summary>下回合获得的能量</summary>
    public int EnergyAmount { get; set; } = 7;
    /// <summary>下回合抽牌数量</summary>
    public int CardDrawAmount { get; set; } = 7;
    /// <summary>下回合获得的力量</summary>
    public int StrengthAmount { get; set; } = 77;
    /// <summary>下回合获得的敏捷</summary>
    public int DexterityAmount { get; set; } = 77;

    private bool _buffsApplied;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (_buffsApplied || player.Creature != Owner) return;
        _buffsApplied = true;

        // 获得能量
        await PlayerCmd.GainEnergy(EnergyAmount, player);
        // 抽牌
        await CardPileCmd.Draw(choiceContext, CardDrawAmount, player, false);
        // 获得力量
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner!, StrengthAmount, Owner, null, false);
        // 获得敏捷
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner!, DexterityAmount, Owner, null, false);
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (!_buffsApplied || side != CombatSide.Player || Owner == null) return;

        await CreatureCmd.Kill(Owner, force: true);
    }
}
