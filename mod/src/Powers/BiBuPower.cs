using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Yjcm;

/// <summary>
/// 庇部：每回合敌人第一次获得力量时，使其失去等量力量，你获得等量力量。
/// 通过 ModifyPowerAmountGivenAdditive 拦截，在 AfterPlayerTurnStart 重置。
/// </summary>
[RegisterPower]
public sealed class BiBuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/bi_bu.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/bi_bu.png";

    private bool _triggeredThisTurn;

    /// <summary>回合开始时重置</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature == Owner)
            _triggeredThisTurn = false;
    }

    private const int MaxSteal = 5;

    /// <summary>
    /// 拦截力量给予：当敌人即将获得力量时，取消并转给持有者，上限{MaxSteal}。
    /// </summary>
    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (_triggeredThisTurn || Owner == null || target == null) return 0;
        if (power is not StrengthPower || amount <= 0) return 0;
        if (target.IsPlayer) return 0;

        _triggeredThisTurn = true;
        var stolen = Math.Min(amount, MaxSteal);

        ApplyStolenStrength(stolen, cardSource);

        // 仅取消被偷取的部分，超出部分仍给敌人
        return -stolen;
    }

    /// <summary>在下一帧异步施加偷取的力量</summary>
    private async void ApplyStolenStrength(decimal amount, CardModel? cardSource)
    {
        if (Owner == null) return;
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner, amount, Owner, cardSource, false);
        VoicePlayer.PlayWav(TriggerVoicePath);
    }

    public static readonly string TriggerVoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(BiBuPower).Assembly.Location)!,
        "Voice", "Bank", "Assets", "bi_bu_buff.wav");
}
