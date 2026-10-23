using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

// Mock only the external API and initialization side effects; call the real integrated code.
internal static class SunqianCases
{
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int _calls;
    private static object?[]? _received;

    public static async Task Run(Assembly content, string stagedPath, Action<bool, string> check)
    {
        var hover = content.GetType("Yjcm.Interop.SunqianLinkHover", true)!;
        var decide = hover.GetMethod("SelectMode", Flags)!;
        for (int bits = 0; bits < 32; bits++)
        {
            bool ready = (bits & 1) != 0, run = (bits & 2) != 0, combat = (bits & 4) != 0;
            bool party = (bits & 8) != 0, local = (bits & 16) != 0;
            int expected = !ready ? 0 : !run ? 1 : !party ? 0 : combat && local ? 2 : 1;
            check((int)decide.Invoke(null, [ready, run, combat, party, local])! == expected,
                $"Mutually exclusive hover/keyword mode: {bits}");
        }

        var api = content.GetType("Yjcm.Interop.SunqianUniverseInterop", true)!;
        check(!(bool)api.GetProperty("IsReady")!.GetValue(null)!, "Absent partner must remain unavailable");
        foreach (string helperName in new[] { "YiZhenQingMengInterop", "ZhuQueYuShanInterop" })
        {
            var helper = content.GetType("Yjcm.Interop." + helperName, true)!;
            check(!(bool)helper.GetMethod("Affects", Flags)!.Invoke(null, [null])!,
                "Absent partner must preserve vanilla fire branch");
        }
        check(!await (Task<bool>)api.GetMethod("TriggerBurning")!.Invoke(null, [null, null, null])!,
            "Unbound trigger must safely return false");

        var harmony = new Harmony("Yjcm.tests.sunqian");
        try
        {
            harmony.Patch(api.GetMethod("ApplyBurning")!, prefix: new HarmonyMethod(
                typeof(SunqianCases).GetMethod(nameof(Apply), Flags)!));
            var target = new Creature(null!, 100, 100);
            foreach (string helperName in new[] { "YiZhenQingMengInterop", "ZhuQueYuShanInterop" })
            {
                _calls = 0;
                var helper = content.GetType("Yjcm.Interop." + helperName, true)!;
                await (Task)helper.GetMethod("ApplyEffect", Flags)!.Invoke(null,
                    [null, target, 7m, target, null, true])!;
                check(_calls == 1, "Burning application must call partner once");
                check(ReferenceEquals(_received![1], target) && (decimal)_received[2]! == 7m &&
                    ReferenceEquals(_received[3], target) && _received[4] == null && _received[5]!.Equals(false),
                    "Partner call must preserve target, amount, source and silent flag");
            }

            // Exercise the real loader guard without booting the game. MainFile's static Logger
            // needs Godot native initialization, so its guard is checked in check_assembly instead.
            foreach (var (assembly, name) in new[] {
                (Assembly.LoadFrom(Path.Combine(stagedPath, "Yjcm.dll")), "Yjcm.Loader.LoaderBootstrap") })
            {
                var type = assembly.GetType(name, true)!;
                _calls = 0;
                harmony.Patch(type.GetMethod("InitializeCore", Flags)!, prefix: new HarmonyMethod(
                    typeof(SunqianCases).GetMethod(nameof(LoaderCore), Flags)!));
                var initialize = type.GetMethod("Initialize", Flags)!;
                await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => initialize.Invoke(null, null))));
                check(_calls == 1, name + " must initialize exactly once");
                check((bool)type.GetField("_initialized", Flags)!.GetValue(null)!, name + " must record readiness");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static bool Apply(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        Creature? applier, CardModel? cardSource, bool silent, ref Task<PowerModel?> __result)
    {
        _calls++;
        _received = [choiceContext, target, amount, applier, cardSource, silent];
        __result = Task.FromResult<PowerModel?>(null);
        return false;
    }

    private static bool LoaderCore(ref bool __result)
    {
        Interlocked.Increment(ref _calls);
        __result = true;
        return false;
    }
}
