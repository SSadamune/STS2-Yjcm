using STS2RitsuLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>
/// 负面Buff「连环」：受伤害时传导等量伤害给其他有连环的敌人，全员减一层
/// </summary>
public sealed class LianHuanPower : ModPowerTemplate
{
    private static bool _isChaining; // 传导中标志：防止传导伤害再次触发传导（兜底）

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/lian_huan.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/lian_huan.png";

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult _, ValueProp props, Creature? dealer, CardModel? __)
    {
        await base.AfterDamageReceived(choiceContext, target, _, props, dealer, __);

        if (base.Amount <= 0 || !props.IsPoweredAttack())
            return;
        // 关键：只响应"持有者本人"受到的伤害。Hook.AfterDamageReceived 会把该钩子
        // 派发给场上所有连环持有者（不只是被击中的那个），若不对比 target，
        // 每次受击每个连环敌人都会各自传导一次 → 多目标级联（历史上 5/18 标记的
        // "连环Buff递归触发问题"就是它）
        if (target != Owner) return;
        // 兜底：传导伤害本身（Unblockable|Unpowered，非攻击）不会被 IsPoweredAttack
        // 判定，正常不会再次触发传导；此标志再挡一道，杜绝任何路径的递归
        if (_isChaining) return;

        var combatState = target.CombatState;
        if (combatState == null) return;

        var linked = combatState.HittableEnemies
            .Where(e => e.Powers?.OfType<LianHuanPower>().Any(p => p.Amount > 0) == true)
            .ToList();
        if (linked.Count <= 1) return;

        // 用真实伤害传导（走 CreatureCmd.Damage 正常管线）。
        // 之前用反射直接改 _currentHp 绕过死亡流程：被传导致死的敌人不触发死亡处理，
        // 尸体残留原地，且悬停尸体时 Creature.get_HoverTips 对空 powers 抛
        // NullReferenceException。改走 Damage 后死亡处理正常，尸体被移除。
        // 传导伤害不可格挡、不吃力量/易伤修正（相当于体力流失）；
        // dealer 传 null，避免误触发伤害来源相关钩子。
        _isChaining = true;
        try
        {
            foreach (var enemy in linked)
            {
                if (enemy == target || enemy.IsDead) continue;
                var power = enemy.Powers?.OfType<LianHuanPower>().FirstOrDefault();
                if (power == null || power.Amount <= 0) continue;
                var loss = Math.Min(power.Amount, enemy.CurrentHp);
                if (loss <= 0) continue;
                await CreatureCmd.Damage(choiceContext, enemy, loss,
                    ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            }
        }
        finally
        {
            _isChaining = false;
        }

        // 全员减一层，减到0时移除buff
        foreach (var enemy in linked)
        {
            if (enemy.IsDead) continue; // 被传导打死的敌人跳过（powers 可能已被清理）
            var power = enemy.Powers?.OfType<LianHuanPower>().FirstOrDefault();
            if (power == null) continue;
            var newAmount = power.Amount - 1;
            if (newAmount <= 0)
                await PowerCmd.Remove(power);
            else
                power.SetAmount(newAmount, silent: true);
        }
    }
}
