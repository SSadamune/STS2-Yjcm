using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using Yjcm.Extensions;

#nullable enable

namespace Yjcm;

/// <summary>
/// 栏位选项卡基类：作为卡牌选择载体，用于选择要废除/恢复哪个装备栏。
/// 所有选项卡共享贤贶的卡面，打出时无实际效果（仅用于 <see cref="CardSelectCmd.FromSimpleGrid"/> 选择）。
/// </summary>
public abstract class SlotOptionCard : CardModel
{
    /// <summary>该选项卡对应的装备栏位。</summary>
    public abstract EquipmentSlotType SlotType { get; }

    public override string PortraitPath => "wei_zhang_xing_cai_xian_kuang.png".CardImagePath();

    public SlotOptionCard()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }

    public override bool CanBeGeneratedInCombat => false;

    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }

    /// <summary>按栏位类型创建对应的选项卡卡牌。</summary>
    public static CardModel Create(EquipmentSlotType slotType, Player player)
    {
        var combatState = player.Creature.CombatState;
        return slotType switch
        {
            EquipmentSlotType.Weapon => combatState.CreateCard<SlotOptionWeapon>(player),
            EquipmentSlotType.Armor => combatState.CreateCard<SlotOptionArmor>(player),
            EquipmentSlotType.DefensiveHorse => combatState.CreateCard<SlotOptionDefensiveHorse>(player),
            EquipmentSlotType.OffensiveHorse => combatState.CreateCard<SlotOptionOffensiveHorse>(player),
            EquipmentSlotType.Treasure => combatState.CreateCard<SlotOptionTreasure>(player),
            _ => throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null)
        };
    }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class SlotOptionWeapon : SlotOptionCard
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Weapon;
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class SlotOptionArmor : SlotOptionCard
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Armor;
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class SlotOptionDefensiveHorse : SlotOptionCard
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.DefensiveHorse;
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class SlotOptionOffensiveHorse : SlotOptionCard
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.OffensiveHorse;
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class SlotOptionTreasure : SlotOptionCard
{
    public override EquipmentSlotType SlotType => EquipmentSlotType.Treasure;
}
