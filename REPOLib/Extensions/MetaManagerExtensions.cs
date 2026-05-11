using System.Collections.Generic;
using System.Linq;

namespace REPOLib.Extensions;

internal static class MetaManagerExtensions
{
    public static bool HasCosmetic(this MetaManager metaManager, CosmeticAsset item)
    {
        if (item == null)
        {
            return false;
        }

        return metaManager.cosmeticAssets.Contains(item);
    }

    internal static bool AddCosmetic(this MetaManager metaManager, CosmeticAsset item)
    {
        if (metaManager.cosmeticAssets.Contains(item))
        {
            return false;
        }

        metaManager.cosmeticAssets.Add(item);
        return true;
    }

    public static List<CosmeticAsset> GetCosmetics(this MetaManager metaManager)
    {
        return metaManager.cosmeticAssets.ToList();
    }
}
