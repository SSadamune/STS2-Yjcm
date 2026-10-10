using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Yjcm;

/// <summary>
/// 装备充能球专用命令：实现「对应类型装备只进对应栏位」的路由规则。
///
/// 与原版 <see cref="OrbCmd.Channel"/> 的区别：
/// 原版充能球是 FIFO 队列，格子满时激发最左（最旧）的球；
/// 装备栏则是固定栏位制——每类装备有且仅有一个栏位，同类装备替换、异类互不影响。
/// </summary>
public static class EquipCmd
{
    /// <summary>
    /// 将一件装备充能球放入玩家对应的装备栏位。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文（原版 Command 约定）。</param>
    /// <param name="player">装备所属玩家。</param>
    /// <param name="orb">要装备的充能球（调用方需先 <c>ModelDb.Orb&lt;T&gt;().ToMutable()</c> 取可变实例）。</param>
    /// <param name="playSfx">是否播放入栏音效（战斗开始时生成空槽球传 false 静默入队）。</param>
    public static async Task ChannelEquipment(PlayerChoiceContext choiceContext, Player player, EquipmentOrbModel orb, bool playSfx = true)
    {
        var queue = player.PlayerCombatState.OrbQueue;

        // 与原版 Channel 一致：设置所有者并校验可变实例
        orb.Owner = player;
        orb.AssertMutable();

        // 查找同类型的已装备充能球（每类有且仅有一个）
        int existingIndex = -1;
        for (int i = 0; i < queue.Orbs.Count; i++)
        {
            if (queue.Orbs[i] is EquipmentOrbModel e ? e.SlotType == orb.SlotType : orb.SlotType == EquipmentSlotType.Treasure)
            {
                existingIndex = i;
                break;
            }
        }

        // 被废除的栏位不能再装备任何装备：直接返回，不改变当前废除球
        if (existingIndex >= 0 && queue.Orbs[existingIndex] is AbolishSlotOrb && orb is not AbolishSlotOrb)
            return;

        if (existingIndex >= 0)
        {
            // 同类替换：移除旧球 → 在同位置插入新球 → 视觉原地替换（不触发激发）
            var existing = queue.Orbs[existingIndex];
            queue.Remove(existing);
            queue.Insert(existingIndex, orb);

            // 视觉：直接替换栏位中的球，旧球不播放激发动画
            NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, orb);

            if (playSfx) orb.PlayChannelSfx();
            CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, orb);
            await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, orb);
        }
        else if (playSfx)
        {
            // 无同类装备：按正常入队（新装备占一个空栏位）。
            // 栏位固定为 5 且最多 5 类装备，队列永远不会溢出，不会触发原版 FIFO 激发。
            await OrbCmd.Channel(choiceContext, orb, player);
        }
        else
        {
            // 静默入队：不播放入栏音效（空槽球战斗开始生成用）
            await queue.TryEnqueue(orb);
            NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddOrbAnim();
            CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, orb);
            await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, orb);
        }
    }

    /// <summary>
    /// 将非装备充能球放入宝物栏（收容栏位，同步版本）。
    /// 宝物栏内已有球（空宝物球或其他非装备球）会被原地替换，不触发激发。
    /// 此方法由 <c>OrbCmd.Channel</c> 的同步拦截补丁调用，也可直接调用。
    /// 注意：同步实现无法 await <c>Hook.AfterOrbChanneled</c>，故省略该通知钩子。
    /// </summary>
    /// <param name="player">所属玩家。</param>
    /// <param name="orb">非装备充能球（调用方需先取可变实例）。</param>
    public static void RouteToTreasure(Player player, OrbModel orb)
    {
        var queue = player.PlayerCombatState.OrbQueue;

        // 与原版 Channel 一致：设置所有者并校验可变实例
        orb.Owner = player;
        orb.AssertMutable();

        // 宝物栏收容位：非装备球，或装备球中的宝物类型（当前为空宝物球）
        int treasureIndex = -1;
        for (int i = 0; i < queue.Orbs.Count; i++)
        {
            if (IsTreasureSlotOrb(queue.Orbs[i]))
            {
                treasureIndex = i;
                break;
            }
        }

        // 宝物栏被废除后，非装备充能球也无法进入（保持废除状态）
        if (treasureIndex >= 0 && queue.Orbs[treasureIndex] is AbolishSlotOrb)
            return;

        if (treasureIndex >= 0)
        {
            // 替换宝物栏旧球
            var existing = queue.Orbs[treasureIndex];
            queue.Remove(existing);
            queue.Insert(treasureIndex, orb);
            NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, orb);
        }
        else
        {
            // 宝物栏空 → 追加到末尾（容量 5 且最多 5 球，不会溢出）
            queue.Insert(queue.Orbs.Count, orb);
            NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddOrbAnim();
        }

        orb.PlayChannelSfx();
        CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, orb);
    }

    /// <summary>
    /// 判断某个充能球是否占据宝物栏：非装备球，或宝物类型的装备球（空宝物球）。
    /// </summary>
    private static bool IsTreasureSlotOrb(OrbModel o)
        => o is not EquipmentOrbModel || (o is EquipmentOrbModel e && e.SlotType == EquipmentSlotType.Treasure);

    // ===== 废除 / 恢复 装备栏 =====

    /// <summary>
    /// 查找指定栏位在当前队列中的索引（-1 表示不存在）。
    /// </summary>
    private static int FindSlotIndex(Player player, EquipmentSlotType slotType)
    {
        var orbs = player.PlayerCombatState.OrbQueue.Orbs;
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] is EquipmentOrbModel e ? e.SlotType == slotType : slotType == EquipmentSlotType.Treasure)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 判断某个栏位是否已被废除（该位置的球是废除充能球）。
    /// </summary>
    public static bool IsSlotAbolished(Player player, EquipmentSlotType slotType)
    {
        int idx = FindSlotIndex(player, slotType);
        return idx >= 0 && player.PlayerCombatState.OrbQueue.Orbs[idx] is AbolishSlotOrb;
    }

    /// <summary>已废除的栏位数量。</summary>
    public static int CountAbolishedSlots(Player player)
        => player.PlayerCombatState.OrbQueue.Orbs.Count(o => o is AbolishSlotOrb);

    /// <summary>尚未废除的栏位列表（可按此让玩家选择废除目标）。</summary>
    public static List<EquipmentSlotType> GetAvailableSlots(Player player)
    {
        var result = new List<EquipmentSlotType>();
        foreach (var s in Enum.GetValues<EquipmentSlotType>())
        {
            if (!IsSlotAbolished(player, s))
                result.Add(s);
        }
        return result;
    }

    /// <summary>
    /// 废除指定装备栏：将该栏位当前充能球替换为废除球。
    /// 已被废除的栏位再次废除是空操作。
    /// </summary>
    public static async Task AbolishSlot(PlayerChoiceContext choiceContext, Player player, EquipmentSlotType slotType)
    {
        var queue = player.PlayerCombatState.OrbQueue;
        int idx = FindSlotIndex(player, slotType);
        if (idx < 0) return;
        var existing = queue.Orbs[idx];
        if (existing is AbolishSlotOrb) return; // 已废除

        var orb = (AbolishSlotOrb)ModelDb.Orb<AbolishSlotOrb>().ToMutable();
        orb.SetSlotType(slotType);
        orb.Owner = player;
        orb.AssertMutable();

        // 原地替换为废除球（不触发激发）
        queue.Remove(existing);
        queue.Insert(idx, orb);
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, orb);
        orb.PlayChannelSfx();
        CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, orb);
        await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, orb);
    }

    /// <summary>
    /// 恢复指定装备栏：将该栏位的废除球换回对应的空槽球。
    /// 未被废除的栏位无需恢复（空操作）。
    /// </summary>
    public static async Task RestoreSlot(PlayerChoiceContext choiceContext, Player player, EquipmentSlotType slotType)
    {
        var queue = player.PlayerCombatState.OrbQueue;
        int idx = FindSlotIndex(player, slotType);
        if (idx < 0) return;
        var existing = queue.Orbs[idx];
        if (existing is not AbolishSlotOrb) return; // 未废除

        var empty = CreateEmptySlotOrb(slotType, player);
        empty.Owner = player;
        empty.AssertMutable();

        // 原地替换回空槽球（不触发激发）
        queue.Remove(existing);
        queue.Insert(idx, empty);
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, empty);
        empty.PlayChannelSfx();
        CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, empty);
        await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, empty);
    }

    /// <summary>
    /// 恢复一个被废除的装备栏（按栏位顺序取第一个）。无废除栏位时为空操作。
    /// </summary>
    public static async Task RestoreOneSlot(PlayerChoiceContext choiceContext, Player player)
    {
        foreach (var s in Enum.GetValues<EquipmentSlotType>())
        {
            if (IsSlotAbolished(player, s))
            {
                await RestoreSlot(choiceContext, player, s);
                return;
            }
        }
    }

    /// <summary>
    /// 让玩家从给定栏位列表中选择一个栏位（用于废除选择）。
    /// </summary>
    public static async Task<EquipmentSlotType?> ChooseSlot(
        PlayerChoiceContext choiceContext, Player player, IReadOnlyList<EquipmentSlotType> slots)
    {
        var options = new List<CardModel>();
        foreach (var s in slots)
            options.Add(SlotOptionCard.Create(s, player));
        if (options.Count == 0) return null;

        // 允许玩家确认 0 张牌来取消本次废除；调用方会在空选择时直接跳过废除和摸牌。
        var chosenCard = await CardSelectCmd.FromSimpleGrid(
            choiceContext, options, player,
            new CardSelectorPrefs(new("cards", "xiankuang.select_abolish"), 0, 1));

        var chosen = chosenCard.FirstOrDefault();
        if (chosen is SlotOptionCard slotOption)
            return slotOption.SlotType;
        return null;
    }

    /// <summary>按栏位类型创建对应的空槽球（恢复用）。</summary>
#if GAME_BETA
#else
    public static async Task ClearSlot(PlayerChoiceContext choiceContext, Player player, EquipmentSlotType slotType)
    {
        var queue = player.PlayerCombatState.OrbQueue;
        int idx = FindSlotIndex(player, slotType);
        if (idx < 0 || queue.Orbs[idx] is AbolishSlotOrb) return;
        var existing = queue.Orbs[idx];
        var empty = CreateEmptySlotOrb(slotType, player);
        empty.AssertMutable();
        queue.Remove(existing);
        queue.Insert(idx, empty);
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, empty);
        CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, empty);
        await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, empty);
    }

#endif
    private static EquipmentOrbModel CreateEmptySlotOrb(EquipmentSlotType slotType, Player player)
    {
        EquipmentOrbModel orb = slotType switch
        {
            EquipmentSlotType.Weapon => (EquipmentOrbModel)ModelDb.Orb<EmptyWeaponSlotOrb>().ToMutable(),
            EquipmentSlotType.Armor => (EquipmentOrbModel)ModelDb.Orb<EmptyArmorSlotOrb>().ToMutable(),
            EquipmentSlotType.DefensiveHorse => (EquipmentOrbModel)ModelDb.Orb<EmptyDefensiveHorseSlotOrb>().ToMutable(),
            EquipmentSlotType.OffensiveHorse => (EquipmentOrbModel)ModelDb.Orb<EmptyOffensiveHorseSlotOrb>().ToMutable(),
            EquipmentSlotType.Treasure => (EquipmentOrbModel)ModelDb.Orb<EmptyTreasureSlotOrb>().ToMutable(),
            _ => throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null)
        };
        orb.Owner = player;
        return orb;
    }
#if GAME_BETA

    public static async Task ClearSlot(PlayerChoiceContext choiceContext, Player player, EquipmentSlotType slotType)
    {
        var queue = player.PlayerCombatState.OrbQueue;
        int idx = FindSlotIndex(player, slotType);
        if (idx < 0 || queue.Orbs[idx] is AbolishSlotOrb) return;
        var existing = queue.Orbs[idx];
        var empty = CreateEmptySlotOrb(slotType, player);
        empty.AssertMutable();
        queue.Remove(existing);
        queue.Insert(idx, empty);
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(existing, empty);
        CombatManager.Instance.History.OrbChanneled(player.Creature.CombatState, empty);
        await Hook.AfterOrbChanneled(player.Creature.CombatState, choiceContext, player, empty);
    }
#else
#endif
}
