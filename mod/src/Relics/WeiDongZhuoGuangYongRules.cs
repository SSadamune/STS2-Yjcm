namespace Yjcm;

// Pure decisions shared by the live effect and deterministic regression tests.
internal static class WeiDongZhuoGuangYongRules
{
    internal readonly record struct Transfer(int Enemy, int Player, bool Success);

    internal static Transfer Calculate(int enemy, int player, bool upgraded, bool cap, bool seen)
    {
        if (enemy <= 0) return new(enemy, player, false);
        if (!cap)
        {
            int amount = upgraded ? enemy : 1;
            return new(enemy - amount, player + amount, true);
        }
        if (upgraded) return new(0, 1, true);
        if (!seen && player <= 0) return new(enemy, enemy, true);
        if (player <= 1) return new(0, 1, true);
        return new(enemy - 1, player - 1, true);
    }

    internal static bool PayMaxHp(int hp, int maxHp, bool success) => success && hp < maxHp && maxHp > 1;
}
