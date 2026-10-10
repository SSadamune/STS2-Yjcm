using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

#nullable enable

namespace Yjcm;

/// <summary>
/// 天任：每打出一张武将牌，层数+1。当层数大于体力上限/7时清零，增加1点体力上限并恢复1点体力。
/// </summary>
public sealed class TianRenPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomIconPath => "res://Yjcm/Images/Powers/tian_ren.png";

    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/tian_ren.png";

    public static readonly string TriggerVoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(TianRenPower).Assembly.Location)!,
        "Voice", "Bank", "Assets", "tian_ren_buff.wav");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 只响应武将牌
        if (!cardPlay.Card.Tags.Contains(YjcmCardTags.WuJiang)) return;
        if (Owner == null) return;
        // 多人模式只统计持有者自己打出的武将牌，队友打出的牌不叠加
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        SetAmount(Amount + 1);

        var threshold = Owner.MaxHp / 9;
        if (Amount > threshold)
        {
            SetAmount(0);
            VoicePlayer.PlayWav(TriggerVoicePath);
            await CreatureCmd.GainMaxHp(Owner, 3);
        }
    }
}
