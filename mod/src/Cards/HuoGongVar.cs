using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Yjcm.Interop;

namespace Yjcm;

/// <summary>Display the same giving hooks as PowerCmd without changing the saved base value.</summary>
public sealed class HuoGongVar(string name, decimal amount, bool supportsBurning = false)
    : PowerVar<HuoGongPower>(name, amount)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
        Creature? target, bool runGlobalHooks)
    {
        PreviewValue = BaseValue;
        if (!runGlobalHooks || !card.IsMutable || card.Owner?.Creature is not { } giver ||
            card.CombatState is not { } combat) return;

        var power = supportsBurning && YiZhenQingMengInterop.Affects(card.Owner)
            ? YiZhenQingMengInterop.CanonicalBurningPower()
            : ModelDb.Power<HuoGongPower>();
        // Never pass a live power to a preview: partners use canonical instances to
        // distinguish display-only queries from actual, consumable applications.
        if (power == null) return;
        PreviewValue = Hook.ModifyPowerAmountGiven(combat, power, giver, BaseValue,
            target, card, out _);
    }
}
