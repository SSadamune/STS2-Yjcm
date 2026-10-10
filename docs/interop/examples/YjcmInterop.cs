using System;
using System.Threading.Tasks;
using STS2RitsuLib.Interop;

namespace PartnerMod.Interop;

// Copy this file into the partner's project. Do NOT add a reference to Yjcm.dll.
// Register that project's own assembly with ModTypeDiscoveryHub, as in the tutorial.
[AssemblyInterop("Yjcm.Api.PublicApi, Yjcm")]
public static class YjcmInterop
{
    public static bool IsReady => false;
    public static int ApiVersion => 0;
    public static string GetModId() => "";
    public static string[] GetCharacterIds() => [];
    public static string[] GetCapabilities() => [];
    public static string[] GetPartnerIds() => [];
    public static bool HasPartner(string modId) => false;
    public static string[] GetPartnerCapabilities(string modId) => [];
    public static bool RegisterPartner(string modId, int apiVersion, string[] capabilities,
        Func<string, string, Task<string>> receiver) => false;
    public static bool UnregisterPartner(string modId) => false;
    public static Task<string> InvokeAsync(string operation, string payloadJson) =>
        Task.FromResult("{\"ok\":false,\"error\":\"partner_missing\",\"data\":null}");
    public static Task<string> CallPartnerAsync(string modId, string operation, string payloadJson) =>
        Task.FromResult("{\"ok\":false,\"error\":\"partner_missing\",\"data\":null}");
}
