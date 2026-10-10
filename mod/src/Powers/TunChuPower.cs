using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

#nullable enable

namespace Yjcm;

/// <summary>
/// 囤储：能量不重置，无法获得能量，每回合开始失去3点能量。
/// </summary>
public sealed class TunChuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/tun_chu.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/tun_chu.png";

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        if (player.Creature == Owner)
            return false;
        return base.ShouldPlayerResetEnergy(player);
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player.Creature == Owner)
            return 0;
        return base.ModifyEnergyGain(player, amount);
    }

    // 每回合开始失去3点能量
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature == Owner)
            await PlayerCmd.LoseEnergy(3, player);
    }
}
