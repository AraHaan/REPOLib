using HarmonyLib;
using REPOLib.Modules;
using UnityEngine;

namespace REPOLib.Patches;

[HarmonyPatch(typeof(MultiplayerPool))]
internal static class MultiplayerPoolPatch
{
    [HarmonyPatch(nameof(MultiplayerPool.Instantiate))]
    [HarmonyPrefix]
    private static bool InstantiatePatch(string prefabId, Vector3 position, Quaternion rotation, ref GameObject __result)
    {
        if (!NetworkPrefabs.TryGetNetworkPrefab(prefabId, out GameObject? prefab))
        {
            return true;
        }

        bool activeSelf = prefab.activeSelf;

        if (activeSelf)
        {
            prefab.SetActive(false);
        }

        __result = Object.Instantiate(prefab, position, rotation);

        if (activeSelf)
        {
            prefab.SetActive(true);
        }

        return false;
    }
}
