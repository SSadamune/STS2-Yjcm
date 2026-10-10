using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using PartnerMod.Interop;

// Reflective consumer in a separate assembly: no compile-time reference to Yjcm or its models.
// Resolve the beta game's module initializer dependencies from the same pinned snapshot.
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string path = Path.Combine(Path.GetFullPath(args[1]), name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var api = assembly.GetType("Yjcm.Api.PublicApi", throwOnError: true)!;
const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
int checks = 0;
void Check(bool value, string description)
{
    if (!value) throw new InvalidOperationException(description);
    checks++;
}
object? Call(string name, params object?[] values) => api.GetMethod(name, Flags)!.Invoke(null, values);
bool Register(string id, int version, string[] caps, Func<string, string, Task<string>> receiver) =>
    (bool)Call("RegisterPartner", id, version, caps, receiver)!;
async Task<JsonElement> Response(string method, params object?[] values)
{
    using var json = JsonDocument.Parse(await (Task<string>)Call(method, values)!);
    return json.RootElement.Clone();
}
void Error(JsonElement response, string error) => Check(!response.GetProperty("ok").GetBoolean() &&
    response.GetProperty("error").GetString() == error, $"Expected {error}: {response}");

Check(!YjcmInterop.IsReady && YjcmInterop.ApiVersion == 0, "Unbound tutorial proxy must remain unavailable");
Check(!YjcmInterop.RegisterPartner("Missing", 1, [], (_, _) => Task.FromResult("{}")), "Fallback registration must not succeed");
Check(!api.GetProperty("IsReady")!.GetValue(null)!.Equals(true), "API must not expose readiness before initialization");
Error(await Response("InvokeAsync", "api.info", "{}"), "not_ready");
Check(Register("FixtureEarly", 1, ["echo"], (_, payload) => Task.FromResult(payload)), "Early callback registration");
Error(await Response("CallPartnerAsync", "FixtureEarly", "echo", "{}"), "not_ready");
Call("Initialize", (object)new[] { "YJCM_CHARACTER_WU_ZHUGELIANG", "YJCM_CHARACTER_WEI_DONG_ZHUO" });
Check((bool)api.GetProperty("IsReady")!.GetValue(null)!, "Readiness after initialization");
Check((int)api.GetProperty("ApiVersion")!.GetValue(null)! == 1, "Protocol version");
Check((string)Call("GetModId")! == "Yjcm", "Exact mod ID");
var info = await Response("InvokeAsync", "api.info", "{}");
Check(info.GetProperty("ok").GetBoolean() && info.GetProperty("data").GetProperty("modId").GetString() == "Yjcm", "Partner calls Yjcm");
Check(info.GetProperty("data").GetProperty("characterIds").GetArrayLength() == 2, "Character catalog");
var characters = await Response("InvokeAsync", "content.characters", "{}");
Check(characters.GetProperty("ok").GetBoolean() && characters.GetProperty("data").GetArrayLength() == 2, "Built-in content query");
Error(await Response("InvokeAsync", "unknown", "{}"), "unsupported_operation");
Error(await Response("InvokeAsync", "bad operation", "{}"), "invalid_operation");
Error(await Response("InvokeAsync", "api.info", "not json"), "invalid_payload");
Error(await Response("InvokeAsync", "api.info", new string(' ', 65_537)), "invalid_payload");
Error(await Response("InvokeAsync", "api.info", null), "invalid_payload");
Error(await Response("CallPartnerAsync", "Missing", "echo", "{}"), "partner_missing");
Check(!Register("YJCM", 1, [], (_, _) => Task.FromResult("{}")), "Self partner rejected");
Check(!Register("WrongVersion", 2, [], (_, _) => Task.FromResult("{}")), "Version mismatch rejected");
Check(!Register("bad id", 1, [], (_, _) => Task.FromResult("{}")), "Bad ID rejected");
Check(!Register("BadCapability", 1, ["bad operation"], (_, _) => Task.FromResult("{}")), "Bad capability rejected");
Check(!Register("TooMany", 1, Enumerable.Repeat("echo", 129).ToArray(), (_, _) => Task.FromResult("{}")), "Capability limit");
var capabilities = new[] { "echo", "reverse" };
int callbackCount = 0;
Check(Register("Fixture", 1, capabilities, async (operation, payload) =>
{
    callbackCount++;
    if (operation == "echo") return payload;
    // Reverse callback calls back into Yjcm after an asynchronous boundary.
    await Task.Yield();
    var nested = await Response("InvokeAsync", "content.characters", "{}");
    Check(nested.GetProperty("ok").GetBoolean(), "Callback can call Yjcm without registry-lock deadlock");
    return nested.GetProperty("data").GetRawText();
}), "Partner registers reverse direction");
capabilities[0] = "changed";
Check((bool)Call("HasPartner", "Fixture")!, "Registered partner is visible");
Check(!Register("Fixture", 1, ["echo"], (_, _) => Task.FromResult("{}")), "Duplicate must not replace callback");
var echo = await Response("CallPartnerAsync", "Fixture", "echo", "{\"value\":42}");
Check(echo.GetProperty("ok").GetBoolean() && echo.GetProperty("data").GetProperty("value").GetInt32() == 42, "Yjcm calls partner");
Check(callbackCount == 1, "Dispatch must invoke exactly once");
Error(await Response("CallPartnerAsync", "Fixture", "changed", "{}"), "unsupported_operation");
Error(await Response("CallPartnerAsync", "fixture", "echo", "{}"), "partner_missing");
Error(await Response("CallPartnerAsync", "Fixture", "echo", "bad json"), "invalid_payload");
var reverse = await Response("CallPartnerAsync", "Fixture", "reverse", "{}");
Check(reverse.GetProperty("ok").GetBoolean() && reverse.GetProperty("data").GetArrayLength() == 2, "Bidirectional async callback");
var snapshot = (string[])Call("GetPartnerCapabilities", "Fixture")!;
snapshot[0] = "mutated";
Check(((string[])Call("GetPartnerCapabilities", "Fixture")!).Contains("echo"), "Capability snapshot isolation");
var catalog = (string[])Call("GetCharacterIds")!;
catalog[0] = "mutated";
Check(!((string[])Call("GetCharacterIds")!).Contains("mutated"), "Character snapshot isolation");
var partners = (string[])Call("GetPartnerIds")!;
partners[0] = "mutated";
Check(!((string[])Call("GetPartnerIds")!).Contains("mutated"), "Partner snapshot isolation");

Check(Register("Throw", 1, ["run"], (_, _) => throw new Exception("fixture")), "Throwing partner registration");
Error(await Response("CallPartnerAsync", "Throw", "run", "{}"), "handler_failed");
Check(Register("Cancelled", 1, ["run"], (_, _) => Task.FromCanceled<string>(new CancellationToken(true))), "Cancelled callback registration");
Error(await Response("CallPartnerAsync", "Cancelled", "run", "{}"), "cancelled");
Check(Register("BadJson", 1, ["run"], (_, _) => Task.FromResult("not json")), "Bad JSON callback registration");
Error(await Response("CallPartnerAsync", "BadJson", "run", "{}"), "invalid_response");
Check(Register("NullResponse", 1, ["run"], (_, _) => Task.FromResult<string>(null!)), "Null response callback registration");
Error(await Response("CallPartnerAsync", "NullResponse", "run", "{}"), "invalid_response");
Check(Register("BigResponse", 1, ["run"], (_, _) => Task.FromResult(new string(' ', 65_537))), "Oversized response callback registration");
Error(await Response("CallPartnerAsync", "BigResponse", "run", "{}"), "invalid_response");
Check(Register("Loop", 1, ["run"], async (_, _) =>
{
    var nested = await Response("CallPartnerAsync", "Loop", "run", "{}");
    Error(nested, "recursive_call");
    return "{\"loopStopped\":true}";
}), "Loop callback registration");
var loop = await Response("CallPartnerAsync", "Loop", "run", "{}");
Check(loop.GetProperty("data").GetProperty("loopStopped").GetBoolean(), "Recursive loop is stopped");
Check((bool)Call("RegisterLocalEndpoint", "test.extension", (Func<string, Task<string>>)(p => Task.FromResult(p)))!, "Local explicit endpoint registration");
Check(!(bool)Call("RegisterLocalEndpoint", "api.info", (Func<string, Task<string>>)(_ => Task.FromResult("{}")))!, "Built-in endpoint must not be overwritten");
Check(((string[])Call("GetCapabilities")!).Contains("test.extension"), "Capability discovery includes local extension");
var extension = await Response("InvokeAsync", "test.extension", "[1,2]");
Check(extension.GetProperty("ok").GetBoolean() && extension.GetProperty("data").GetArrayLength() == 2, "Explicit local dispatch");
Check((bool)Call("UnregisterPartner", "Fixture")!, "Partner unregister");
Check(!(bool)Call("UnregisterPartner", "Fixture")!, "Second unregister is a normal failure");
Error(await Response("CallPartnerAsync", "Fixture", "echo", "{}"), "partner_missing");
// Failure/recursion must not leak into a later, independent request.
Check((await Response("InvokeAsync", "api.info", "{}")).GetProperty("ok").GetBoolean(), "Subsequent call remains healthy");
Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "InteropTests"), "Runtime must not depend on test partner");
var gameplayApi = assembly.GetType("Yjcm.Api.YjcmPublicApi", true)!;
foreach (var stub in typeof(YjcmGameplayInterop).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
{
    var target = gameplayApi.GetMethod(stub.Name, stub.GetParameters().Select(p => p.ParameterType).ToArray());
    Check(target != null && target.ReturnType == stub.ReturnType, $"Gameplay proxy mismatch: {stub.Name}");
    Check(stub.GetParameters().All(p => p.ParameterType.Assembly != assembly) && stub.ReturnType.Assembly != assembly,
        $"No private Yjcm type in proxy: {stub.Name}");
}
Check(!YjcmGameplayInterop.IsReady && YjcmGameplayInterop.ApiVersion == 0, "Missing gameplay partner fallback");
Check(YjcmGameplayInterop.GetEquipmentInSlot(null, 0) == null, "Missing equipment fallback");
Check((int)gameplayApi.GetProperty("ApiVersion")!.GetValue(null)! == 1, "Gameplay version");
Check((bool)gameplayApi.GetProperty("IsReady")!.GetValue(null)!, "Shared initialization readiness");
Check(((int[])gameplayApi.GetMethod("GetEquipmentSlotTypes")!.Invoke(null, null)!).SequenceEqual([0,1,2,3,4]), "Stable equipment slot numbers");
foreach (var name in new[] { "IsHuoGongPower", "IsEquipmentOrb", "IsEmptyEquipmentSlot", "HasEquipmentBar", "IsYjcmCharacter" })
    Check(!(bool)gameplayApi.GetMethod(name)!.Invoke(null, [null])!, $"Null safe query: {name}");
Check((int)gameplayApi.GetMethod("GetEquipmentSlotType")!.Invoke(null, [null])! == -1, "Non-equipment sentinel");
Check((int)gameplayApi.GetMethod("GetHuoGongAmount")!.Invoke(null, [null])! == 0, "Absent fire attack zero");
Check(((int[])gameplayApi.GetMethod("GetAvailableEquipmentSlots")!.Invoke(null, [null])!).Length == 0, "No phantom slots outside combat");
foreach (var slot in new[] { -1, 5 })
{
    try { gameplayApi.GetMethod("GetEquipmentInSlot")!.Invoke(null, [null, slot]); Check(false, "Invalid slot accepted"); }
    catch (TargetInvocationException ex) { Check(ex.InnerException is ArgumentOutOfRangeException, "Slot validation"); }
}
var reverseApi = assembly.GetType("Yjcm.Interop.SunqianUniverseInterop", true)!;
Check(!(bool)reverseApi.GetProperty("IsReady")!.GetValue(null)!, "Absent Sunqian is optional");
Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "sunqian-universe"), "No hard reference to Sunqian");
var data = Activator.CreateInstance(assembly.GetType("Yjcm.WuZhugeliangSettingsData", true)!)!;
Check((bool)data.GetType().GetProperty("EnableEffects")!.GetValue(data)!, "VFX default on including old files with missing property");
Console.WriteLine($"YJCM_INTEROP_OK dll={Path.GetFileName(args[0])} assertions={checks}");
