using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Yjcm.Interop;

/// <summary>Optional event rewards. Never loads a disabled assembly or scans unrelated card pools.</summary>
internal static class ShangFangGuPartners
{
    internal const string SunqianModId = "sunqian-universe";
    internal const string NewsanguoModId = "newsanguo";
    internal const string SunqianCardType = "Squ.Cards.ShangfangguSigh";
    internal const string NewsanguoCardType = "newsanguo.Scripts.Cards.BetterThanYilingFlames";

    internal static CardModel? SunqianCard => Resolve(SunqianModId, SunqianCardType);
    internal static CardModel? NewsanguoCard => Resolve(NewsanguoModId, NewsanguoCardType);

    private static CardModel? Resolve(string modId, string typeName) =>
        Resolve(modId, typeName,
            id => ModManager.GetLoadedMods().Any(mod => mod.manifest?.id == id),
            (assemblyName, name) =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(candidate => candidate.GetName().Name == assemblyName);
                var type = assembly?.GetType(name, throwOnError: false);
                return type != null && typeof(CardModel).IsAssignableFrom(type)
                    ? ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(type))
                    : null;
            });

    // Dependency injection keeps the four installation combinations testable without partner DLLs.
    internal static CardModel? Resolve(string modId, string typeName,
        Func<string, bool> isLoaded, Func<string, string, CardModel?> findRegisteredCard)
    {
        if (!isLoaded(modId)) return null;
        return findRegisteredCard(modId, typeName);
    }
}
