using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop;

#nullable enable
namespace PartnerMod.Interop;

// Copy into Sunqian Universe (or another partner), registering its own assembly with RitsuLib.
// Gate calls with IsReady. These stubs must not throw or contain multiple returns.
[AssemblyInterop("Yjcm.Api.YjcmPublicApi, Yjcm")]
public static class YjcmGameplayInterop
{
    public static bool IsReady => false;
    public static int ApiVersion => 0;
    public static string[] GetCapabilities() => [];
    public static bool IsYjcmCharacter(CharacterModel? character) => false;
    public static bool IsHuoGongPower(PowerModel? power) => false;
    public static PowerModel? GetHuoGongPower(Creature? creature) => null;
    public static int GetHuoGongAmount(Creature? creature) => 0;
    public static IHoverTip CreateHuoGongHoverTip() => null!;
    public static Task<PowerModel?> ApplyHuoGong(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) => Task.FromResult<PowerModel?>(null);
    public static int[] GetEquipmentSlotTypes() => [];
    public static bool IsEquipmentOrb(OrbModel? orb) => false;
    public static int GetEquipmentSlotType(OrbModel? orb) => -1;
    public static bool IsEmptyEquipmentSlot(OrbModel? orb) => false;
    public static bool HasEquipmentBar(Player? player) => false;
    public static OrbModel? GetEquipmentInSlot(Player? player, int slotType) => null;
    public static bool IsEquipmentSlotAbolished(Player? player, int slotType) => false;
    public static int CountAbolishedEquipmentSlots(Player? player) => 0;
    public static int[] GetAvailableEquipmentSlots(Player? player) => [];
    public static OrbModel[] GetEquipmentCatalog() => [];
    public static Task<bool> ChannelEquipment(PlayerChoiceContext choiceContext, Player player,
        OrbModel orb, bool playSfx = true) => Task.FromResult(false);
    public static Task<bool> AbolishEquipmentSlot(PlayerChoiceContext choiceContext, Player player, int slotType) => Task.FromResult(false);
    public static Task<bool> RestoreEquipmentSlot(PlayerChoiceContext choiceContext, Player player, int slotType) => Task.FromResult(false);
    public static Task<bool> ClearEquipmentSlot(PlayerChoiceContext choiceContext, Player player, int slotType) => Task.FromResult(false);
}
