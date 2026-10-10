namespace Yjcm;

/// <summary>
/// 装备栏位类型：与三国杀 4 个装备栏一一对应。
/// 每个充能球栏位只接受对应类型的装备充能球。
/// </summary>
public enum EquipmentSlotType
{
    /// <summary>武器栏：可装备一把武器（如诸葛连弩、贯石斧）。</summary>
    Weapon = 0,

    /// <summary>防具栏：可装备一件防具（如八卦阵、仁王盾）。</summary>
    Armor = 1,

    /// <summary>防御马栏（+1马）：提高自身防御距离。</summary>
    DefensiveHorse = 2,

    /// <summary>进攻马栏（-1马）：降低进攻距离。</summary>
    OffensiveHorse = 3,

    /// <summary>宝物栏：可装备一件宝物；同时作为非装备充能球的收容栏位。</summary>
    Treasure = 4,
}
