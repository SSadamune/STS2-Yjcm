using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Yjcm;

/// <summary>威董卓在队伍中时，以角色专属台词替换先古之民的商店对白。</summary>
[HarmonyPatch(typeof(MerchantRoom), "EnterInternal")]
internal static class WeiDongZhuoMerchantDialogue
{
    private const string Domain = "characters";
    private static readonly Dictionary<string, List<LocString>> VanillaLines = new();

    private static void Prefix(IRunState runState)
    {
        CaptureVanillaLines();
        if (!runState.Players.Any(player => player.Character is WeiDongZhuo))
        {
            RestoreVanillaLines();
            return;
        }
        Replace(MerchantRoom.Dialogue, "_welcomeLines", "merchant.welcome.1", "merchant.welcome.2");
        Replace(MerchantRoom.Dialogue, "_openInventoryLines", "merchant.open.1", "merchant.open.2");
        Replace(MerchantRoom.Dialogue, "_purchaseSuccessLines", "merchant.buy.1", "merchant.buy.2");
        Replace(MerchantRoom.Dialogue, "_purchaseFailureGoldLines", "merchant.no_gold.1");
        Replace(MerchantRoom.Dialogue, "_purchaseFailureSpaceLines", "merchant.no_space.1");
        Replace(MerchantRoom.Dialogue, "_purchaseFailureForbiddenLines", "merchant.forbidden.1");
        Replace(MerchantRoom.Dialogue, "_foulPotionLines", "merchant.potion.1");
        Replace(MerchantRoom.Dialogue, "_playerDeadLines", "merchant.dead.1");
    }

    private static void Replace(MerchantDialogueSet dialogue, string fieldName, params string[] suffixes)
    {
        var lines = AccessTools.Field(typeof(MerchantDialogueSet), fieldName)
            .GetValue(dialogue) as List<LocString>;
        if (lines == null) return;
        lines.Clear();
        lines.AddRange(suffixes.Select(suffix => new LocString(Domain,
            $"YJCM_CHARACTER_WEI_DONG_ZHUO.{suffix}")));
    }

    private static void CaptureVanillaLines()
    {
        foreach (var fieldName in FieldNames)
        {
            if (VanillaLines.ContainsKey(fieldName)) continue;
            var lines = GetLines(fieldName);
            if (lines != null) VanillaLines[fieldName] = [.. lines];
        }
    }

    private static void RestoreVanillaLines()
    {
        foreach (var (fieldName, original) in VanillaLines)
        {
            var lines = GetLines(fieldName);
            if (lines == null) continue;
            lines.Clear();
            lines.AddRange(original);
        }
    }

    private static List<LocString>? GetLines(string fieldName) =>
        AccessTools.Field(typeof(MerchantDialogueSet), fieldName)
            .GetValue(MerchantRoom.Dialogue) as List<LocString>;

    private static readonly string[] FieldNames =
    [
        "_welcomeLines", "_openInventoryLines", "_purchaseSuccessLines",
        "_purchaseFailureGoldLines", "_purchaseFailureSpaceLines",
        "_purchaseFailureForbiddenLines", "_foulPotionLines", "_playerDeadLines"
    ];
}
