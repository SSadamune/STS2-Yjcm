using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Yjcm;

/// <summary>Event-only, once per run. The native death-prevention flow owns the trigger.</summary>
[RegisterRelic(typeof(SharedRelicPool))]
public sealed class QiXingDeng : ModRelicTemplate
{
    private bool _wasUsed;
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsUsedUp => WasUsed;
    public override string PackedIconPath => "res://Yjcm/Images/Relics/qi_xing_deng.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => "res://Yjcm/Images/Relics/Big/qi_xing_deng.png";

    [SavedProperty]
    public bool WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (value) Status = RelicStatus.Disabled;
        }
    }

    public override bool ShouldDieLate(Creature creature) =>
        !CanPreventDeath(WasUsed, creature == Owner.Creature);

    internal static bool CanPreventDeath(bool wasUsed, bool isOwner) => !wasUsed && isOwner;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner.Creature || WasUsed) return;
        // Consume first: nested HP/draw hooks must not reuse the lamp.
        WasUsed = true;
        Flash();
        // Native death prevention enters with 0 HP. Heal reactivates player hooks
        // and fires Revived as well as restoring HP; SetCurrentHp alone does not.
        await CreatureCmd.Heal(creature, 7 - creature.CurrentHp);
        if (creature.CombatState == null || CombatManager.Instance.IsOverOrEnding) return;

        // Reuse the running action for choices triggered by drawn cards. When there is
        // no action (e.g. an enemy-turn hook), queue only if a choice actually occurs.
        var action = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
        if (action != null)
        {
            await GrantCombatRewards(new GameActionPlayerChoiceContext(action));
            return;
        }
        if (LocalContext.NetId is not { } localId) return;
#if GAME_BETA
        var context = new HookPlayerChoiceContext(this, Owner, localId, GameActionType.Combat);
#else
        var context = new HookPlayerChoiceContext(this, localId, creature.CombatState, GameActionType.Combat);
#endif
        await context.AssignTaskAndWaitForPauseOrCompletion(GrantCombatRewards(context));
    }

    internal static bool ShouldDeferEnergy(CombatSide side, PlayerTurnPhase phase) =>
        side != CombatSide.Player || phase is PlayerTurnPhase.None or
            PlayerTurnPhase.AutoPostPlay or PlayerTurnPhase.End;

    private async Task GrantCombatRewards(PlayerChoiceContext context)
    {
        if (ShouldDeferEnergy(Owner.Creature.CombatState!.CurrentSide, Owner.PlayerCombatState!.Phase))
            // Native next-turn energy is granted AFTER the reset, is saved/synchronized,
            // and removes itself. Do not preserve unrelated leftover energy or grant twice.
            await PowerCmd.Apply<EnergyNextTurnPower>(context, Owner.Creature, 7, Owner.Creature, null);
        else
            await PlayerCmd.GainEnergy(7, Owner);
        await CardPileCmd.Draw(context, 7, Owner, false);
    }
}
