using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

internal static class LampEnergyCases
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly HashSet<PowerModel> Active = [];

    public static async Task Run(Action<bool, string> check)
    {
        var owner = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var other = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var state = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        typeof(Player).GetProperty("PlayerCombatState")!.SetValue(owner, state);
        var creature = new Creature(owner, 7, 100);
        typeof(Player).GetField("<Creature>k__BackingField", Flags)!.SetValue(owner, creature);
        var reward = new EnergyNextTurnPower();
        typeof(AbstractModel).GetProperty("IsMutable", Flags)!.SetValue(reward, true);
        typeof(PowerModel).GetField("_owner", Flags)!.SetValue(reward, creature);
        typeof(PowerModel).GetField("_amount", Flags)!.SetValue(reward, 7);
        Active.Add(reward);
        var harmony = new Harmony("Yjcm.tests.lamp-energy");
        try
        {
            harmony.Patch(typeof(PlayerCmd).GetMethod("GainEnergy")!, prefix:
                new HarmonyMethod(typeof(LampEnergyCases).GetMethod(nameof(Gain), Flags)!));
            harmony.Patch(typeof(PowerCmd).GetMethod("Remove", [typeof(PowerModel)])!, prefix:
                new HarmonyMethod(typeof(LampEnergyCases).GetMethod(nameof(Remove), Flags)!));
            state.Energy = 0;
            await reward.AfterEnergyReset(other);
            check(state.Energy == 0 && Active.Contains(reward), "Teammate reset must not collect lamp energy");
            // Native reset happens before the native next-turn power. Only its reward,
            // not the previous turn's unrelated energy, survives into the new total.
            state.Energy = 3;
            foreach (var power in Active.ToArray()) await power.AfterEnergyReset(owner);
            check(state.Energy == 10, "Off-turn lamp must give seven in addition to normal turn energy");
            check(Active.Count == 0, "Lamp's native next-turn reward must remove itself");
            state.Energy = 3;
            foreach (var power in Active.ToArray()) await power.AfterEnergyReset(owner);
            check(state.Energy == 3, "Lamp must not grant energy on later turns");
        }
        finally { Active.Clear(); harmony.UnpatchAll(harmony.Id); }
    }

    private static bool Gain(decimal amount, Player player, ref Task __result)
    {
        player.PlayerCombatState!.GainEnergy(amount);
        __result = Task.CompletedTask;
        return false;
    }
    private static bool Remove(PowerModel? power, ref Task __result)
    {
        if (power != null) Active.Remove(power);
        __result = Task.CompletedTask;
        return false;
    }
}
