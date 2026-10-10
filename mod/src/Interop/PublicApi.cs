using System.Text.Json;

#nullable enable

namespace Yjcm.Api;

/// <summary>
/// Optional cross-mod API. Its public contract uses only CLR types, not Yjcm/game model types.
/// Partners access this type through RitsuLib AssemblyInterop and register their callback for the reverse direction.
/// This is an in-process bridge, not a multiplayer transport or a gameplay command scheduler.
/// </summary>
public static class PublicApi
{
    private const int ProtocolVersion = 1;
    private const int MaxPayloadLength = 65_536;
    private const int MaxCapabilities = 128;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Partner> Partners = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Func<string, Task<string>>> Endpoints = new(StringComparer.Ordinal)
    {
        ["api.info"] = _ => Task.FromResult(JsonSerializer.Serialize(new
        {
            modId = GetModId(), apiVersion = ApiVersion,
            capabilities = GetCapabilities(), characterIds = GetCharacterIds()
        })),
        ["content.characters"] = _ => Task.FromResult(JsonSerializer.Serialize(GetCharacterIds()))
    };
    private static readonly AsyncLocal<string[]?> CallPath = new();
    private static string[] _characterIds = [];
    private static int _ready;

    private sealed record Partner(HashSet<string> Capabilities, Func<string, string, Task<string>> Receiver);

    public static int ApiVersion => ProtocolVersion;
    public static bool IsReady => Volatile.Read(ref _ready) == 1;
    public static string GetModId() => "Yjcm";

    public static string[] GetCharacterIds()
    {
        lock (Gate) return (string[])_characterIds.Clone();
    }

    public static string[] GetCapabilities()
    {
        lock (Gate) return Endpoints.Keys.Order(StringComparer.Ordinal).ToArray();
    }

    public static string[] GetPartnerIds()
    {
        lock (Gate) return Partners.Keys.Order(StringComparer.Ordinal).ToArray();
    }

    public static bool HasPartner(string modId)
    {
        if (!ValidName(modId)) return false;
        lock (Gate) return Partners.ContainsKey(modId);
    }

    public static string[] GetPartnerCapabilities(string modId)
    {
        if (!ValidName(modId)) return [];
        lock (Gate)
            return Partners.TryGetValue(modId, out var partner)
                ? partner.Capabilities.Order(StringComparer.Ordinal).ToArray() : [];
    }

    /// <summary>
    /// Registers the reverse direction: Yjcm can invoke the partner's advertised capabilities.
    /// Receiver returns raw JSON data, not an API response envelope. Duplicate registration does not replace it.
    /// Registration may precede readiness; calls cannot. Mod IDs and operation names are case-sensitive.
    /// </summary>
    public static bool RegisterPartner(string modId, int apiVersion, string[] capabilities,
        Func<string, string, Task<string>> receiver)
    {
        if (!ValidName(modId) || string.Equals(modId, GetModId(), StringComparison.OrdinalIgnoreCase) ||
            apiVersion != ProtocolVersion || capabilities == null || capabilities.Length > MaxCapabilities ||
            capabilities.Any(name => !ValidName(name)) || receiver == null)
            return false;
        var partner = new Partner(new HashSet<string>(capabilities, StringComparer.Ordinal), receiver);
        lock (Gate) return Partners.TryAdd(modId, partner);
    }

    /// <summary>Releases the callback on partner shutdown/reload. Does not cancel already running calls.</summary>
    public static bool UnregisterPartner(string modId)
    {
        if (!ValidName(modId)) return false;
        lock (Gate) return Partners.Remove(modId);
    }

    /// <summary>Partner → Yjcm. Always returns JSON { ok, error, data } for handled failures.</summary>
    public static Task<string> InvokeAsync(string operation, string payloadJson)
    {
        if (!IsReady) return Task.FromResult(Failure("not_ready"));
        if (!ValidName(operation)) return Task.FromResult(Failure("invalid_operation"));
        Func<string, Task<string>>? endpoint;
        lock (Gate) Endpoints.TryGetValue(operation, out endpoint);
        if (endpoint == null) return Task.FromResult(Failure("unsupported_operation"));
        return ExecuteAsync($"Yjcm/{operation}", payloadJson, endpoint);
    }

    /// <summary>Yjcm → partner. Missing/unsupported partners are normal failures, never hard dependencies.</summary>
    public static Task<string> CallPartnerAsync(string modId, string operation, string payloadJson)
    {
        if (!IsReady) return Task.FromResult(Failure("not_ready"));
        if (!ValidName(modId) || !ValidName(operation)) return Task.FromResult(Failure("invalid_operation"));
        Partner? partner;
        lock (Gate) Partners.TryGetValue(modId, out partner);
        if (partner == null) return Task.FromResult(Failure("partner_missing"));
        if (!partner.Capabilities.Contains(operation)) return Task.FromResult(Failure("unsupported_operation"));
        return ExecuteAsync($"{modId}/{operation}", payloadJson, payload => partner.Receiver(operation, payload));
    }

    // Local integrations add explicit capabilities here, not automatic reflection over gameplay methods.
    internal static bool RegisterLocalEndpoint(string operation, Func<string, Task<string>> handler)
    {
        if (!ValidName(operation) || handler == null) return false;
        lock (Gate) return Endpoints.TryAdd(operation, handler);
    }

    internal static void Initialize(string[] characterIds)
    {
        lock (Gate) _characterIds = characterIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Volatile.Write(ref _ready, 1);
    }

    private static bool ValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 128 &&
        name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-' or '/' or ':');

    private static string Failure(string error) => JsonSerializer.Serialize(new { ok = false, error, data = (object?)null });

    private static async Task<string> ExecuteAsync(string route, string payloadJson, Func<string, Task<string>> handler)
    {
        if (payloadJson == null || payloadJson.Length > MaxPayloadLength) return Failure("invalid_payload");
        try { using var payload = JsonDocument.Parse(payloadJson); }
        catch (JsonException) { return Failure("invalid_payload"); }

        var previousPath = CallPath.Value ?? [];
        if (previousPath.Length >= 16 || previousPath.Contains(route, StringComparer.Ordinal))
            return Failure("recursive_call");
        CallPath.Value = [.. previousPath, route];
        try
        {
            // Never call a partner while holding the registry lock, and never Task.Run game-facing callbacks.
            string result = await handler(payloadJson);
            if (result == null || result.Length > MaxPayloadLength) return Failure("invalid_response");
            try
            {
                using var data = JsonDocument.Parse(result);
                return JsonSerializer.Serialize(new { ok = true, error = (string?)null, data = data.RootElement });
            }
            catch (JsonException) { return Failure("invalid_response"); }
        }
        catch (OperationCanceledException) { return Failure("cancelled"); }
        catch (Exception) { return Failure("handler_failed"); }
        finally { CallPath.Value = previousPath; }
    }
}
