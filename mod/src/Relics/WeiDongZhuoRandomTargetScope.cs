using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Yjcm;

// ExecutionContext-local scopes keep simultaneous players and nested auto-plays
// separate. No process-wide "current player", extra random roll or per-turn cap.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class WeiDongZhuoRandomTargetScope
{
    internal sealed class Frame(CardModel card, PlayerChoiceContext context)
    {
        internal readonly CardModel Card = card;
        internal readonly PlayerChoiceContext Context = context;
        internal CardPlay? Play;
        internal Creature? Target;
        internal Rng? Rng;
    }
    private static readonly AsyncLocal<Frame?> Current = new();
    internal static PlayerChoiceContext? ContextFor(CardModel card) =>
        Current.Value?.Card == card ? Current.Value.Context : null;
    internal static Player PlayerOf(CardPlay play)
    {
#if GAME_BETA
        return play.Player;
#else
        return play.Card.Owner;
#endif
    }

    private static void Prefix(CardModel __instance, PlayerChoiceContext choiceContext, out Frame? __state)
    {
        __state = Current.Value;
        Current.Value = new Frame(__instance, choiceContext);
    }
    // Runs when the async method returns its Task, not when that Task completes.
    // The method's continuations retain their captured Frame; the caller is restored.
    private static void Finalizer(Frame? __state) => Current.Value = __state;

    internal static Creature? Reserve(CardPlay play)
    {
        var frame = Current.Value;
        if (frame == null || frame.Card != play.Card)
            throw new InvalidOperationException("GuangYong random card must run inside OnPlayWrapper");
        frame.Play = play;
        frame.Rng = PlayerOf(play).RunState.Rng.CombatTargets;
        frame.Target = frame.Rng.NextItem(PlayerOf(play).Creature.CombatState!.HittableEnemies);
        return frame.Target;
    }

    internal static Creature? NextTarget(Rng rng, IEnumerable<Creature> items, object source)
    {
        var frame = Current.Value;
        bool sameCard = source is CardModel card ? card == frame?.Card
            : source is AttackCommand attack && attack.ModelSource == frame?.Card;
#if GAME_BETA
        if (source is AttackCommand command && command.CardPlay != null && command.CardPlay != frame?.Play) sameCard = false;
#endif
        if (frame?.Target is { } reserved && frame.Rng == rng && sameCard)
        {
            frame.Target = null;
            var candidates = items.ToArray();
            if (candidates.Contains(reserved) && reserved.IsAlive) return reserved;
            // Target may have disappeared as a consequence of another effect.
            return rng.NextItem(candidates);
        }
        return rng.NextItem(items);
    }
}

// Replace only creature-selection call sites, NOT Rng's generic method body
// (the runtime shares generic bodies across reference types).
[HarmonyPatch]
internal static class WeiDongZhuoRandomTargetCalls
{
    private static bool IsTargetRoll(CodeInstruction instruction) =>
        instruction.operand is MethodInfo method && method.DeclaringType == typeof(Rng)
        && method.Name == nameof(Rng.NextItem) && method.IsGenericMethod
        && method.GetGenericArguments().SequenceEqual(new[] { typeof(Creature) });

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        var types = typeof(CardModel).Assembly.GetTypes().Concat(typeof(WeiDongZhuo).Assembly.GetTypes())
            .Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)).Append(typeof(AttackCommand));
        foreach (var type in types)
        {
            var entry = AccessTools.DeclaredMethod(type, type == typeof(AttackCommand) ? "Execute" : "OnPlay");
            if (entry == null) continue;
            var stateType = entry.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            var method = stateType == null ? entry : AccessTools.DeclaredMethod(stateType, "MoveNext");
            if (method?.GetMethodBody() == null) continue;
            if (PatchProcessor.GetCurrentInstructions(method).Any(IsTargetRoll)) yield return method;
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var selfField = AccessTools.Field(__originalMethod.DeclaringType, "<>4__this");
        var replacement = AccessTools.Method(typeof(WeiDongZhuoRandomTargetScope), nameof(WeiDongZhuoRandomTargetScope.NextTarget));
        foreach (var instruction in instructions)
        {
            if (!IsTargetRoll(instruction)) { yield return instruction; continue; }
            yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
            if (selfField != null) yield return new CodeInstruction(OpCodes.Ldfld, selfField);
            yield return new CodeInstruction(OpCodes.Call, replacement);
        }
    }
}
