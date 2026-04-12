using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using REPOLib.Modules;
using UnityEngine;

namespace REPOLib.Patches;

[HarmonyPatch(typeof(MetaManager))]
internal static class MetaManagerPatch
{
    private static List<string> missingCosmeticUnlocks = [];
    private static List<string> missingCosmeticHistory = [];
    private static List<string> missingCosmeticEquipped = [];
    private static List<List<string>> missingCosmeticPresets = [];

    [HarmonyPatch(nameof(MetaManager.Awake))]
    [HarmonyPostfix]
    private static void AwakePatch(MetaManager __instance)
    {
        if(__instance != MetaManager.instance) return;
        if(BundleLoader.AllBundlesLoaded){
            Cosmetics.RegisterCosmetics();
        }else{
            BundleLoader.OnAllBundlesLoaded += Cosmetics.RegisterCosmetics;
        }
    }

    [HarmonyPatch(nameof(MetaManager.PresetsInitialize))]
    [HarmonyPostfix]
    private static void PresetsInitializePatch(MetaManager __instance)
    {
        missingCosmeticPresets.Clear();
        for(var index = 0; index < __instance.cosmeticPresets.Count; index++){
            missingCosmeticPresets.Add(new List<string>());
        }
    }

    [HarmonyPatch(nameof(MetaManager.Reset))]
    [HarmonyPrefix]
    private static void ResetPatch()
    {
        missingCosmeticUnlocks.Clear();
        missingCosmeticHistory.Clear();
        missingCosmeticEquipped.Clear();
    }

    [HarmonyPatch(nameof(MetaManager.CosmeticPresetSet))]
    [HarmonyPrefix]
    private static void CosmeticPresetSetPatch(int _index, List<int> _cosmeticEquipped, List<int> _colorsEquipped)
    {
        if(_index < 0 || _index >= missingCosmeticPresets.Count) return;
        missingCosmeticPresets[_index].Clear();
    }

    [HarmonyPatch(nameof(MetaManager.Save))]
    [HarmonyPrefix]
    private static bool SavePatch(MetaManager __instance)
    {
        bool IsValidCosmetic(int x) => x >= 0 && x < __instance.cosmeticAssets.Count && __instance.cosmeticAssets[x] != null;
        bool IsValidVanillaCosmetic(int x) => IsValidCosmetic(x) && !Cosmetics.RegisteredCosmetics.Contains(__instance.cosmeticAssets[x]);
        bool IsValidModdedCosmetic(int x) => IsValidCosmetic(x) && Cosmetics.RegisteredCosmetics.Contains(__instance.cosmeticAssets[x]);

        #region Vanilla
        var _saveSettings = new ES3Settings(ES3.Location.File);
        _saveSettings.encryptionType = ES3.EncryptionType.AES;
        _saveSettings.encryptionPassword = StatsManager.instance.totallyNormalString;
        _saveSettings.path = Application.isEditor ? __instance.savePathEditor : __instance.savePath;

        // Values that are in-sync with vanilla save file
        ES3.Save("cosmeticTokens", __instance.cosmeticTokens, _saveSettings);
        ES3.Save("cosmeticUnlocks", __instance.cosmeticUnlocks.Where(IsValidVanillaCosmetic).ToList(), _saveSettings);
        ES3.Save("cosmeticHistory", __instance.cosmeticHistory.Where(IsValidVanillaCosmetic).ToList(), _saveSettings);
        #endregion

        #region Modded
        var _saveSettingsModded = new ES3Settings(ES3.Location.Cache);
        _saveSettingsModded.encryptionType = _saveSettings.encryptionType;
        _saveSettingsModded.encryptionPassword = _saveSettings.encryptionPassword;
        _saveSettingsModded.path = "Modded" + _saveSettings.path;

        ES3.Save("cosmeticUnlocks", __instance.cosmeticUnlocks.Where(IsValidModdedCosmetic).Select(x => __instance.cosmeticAssets[x].assetId)
            .Concat(missingCosmeticUnlocks).ToList(), _saveSettingsModded);
        ES3.Save("cosmeticHistory", __instance.cosmeticHistory.Where(IsValidModdedCosmetic).Select(x => __instance.cosmeticAssets[x].assetId)
            .Concat(missingCosmeticHistory).ToList(), _saveSettingsModded);

        ES3.Save("cosmeticEquipped", __instance.cosmeticEquipped.Where(IsValidCosmetic).Select(x => __instance.cosmeticAssets[x].assetId)
            .Concat(missingCosmeticEquipped).ToList(), _saveSettingsModded);

        ES3.Save("cosmeticPresets", __instance.cosmeticPresets.Select((preset, i) => preset.Where(IsValidCosmetic).Select(x => __instance.cosmeticAssets[x].assetId)
            .Concat(missingCosmeticPresets[i]).ToList()
        ).ToList(), _saveSettingsModded);

        ES3.Save("colorPresets", __instance.colorPresets, _saveSettingsModded);
        ES3.Save("colorsEquipped", __instance.colorsEquipped, _saveSettingsModded);

        ES3.StoreCachedFile(_saveSettingsModded);
        #endregion

        return false;
    }

    [HarmonyPatch(nameof(MetaManager.Load))]
    [HarmonyPostfix]
    private static void LoadPatch(MetaManager __instance)
    {
        __instance.saveReady = false;
        LoadModded();
        __instance.saveReady = true;
    }

    public static void LoadModded()
    {
        if(!MetaManager.instance) return;

        bool IsValidCosmetic(string x) => MetaManager.instance.cosmeticAssets.FirstOrDefault(a => a.assetId == x);

        var _savePathModded = "Modded" + (Application.isEditor ? MetaManager.instance.savePathEditor : MetaManager.instance.savePath);
        try{
            ES3Settings _saveFileModded = new ES3Settings(_savePathModded, ES3.EncryptionType.AES, StatsManager.instance.totallyNormalString);
            if(ES3.FileExists(_saveFileModded)){
                Logger.LogInfo("Loading modded meta save");

                #region Combine
                if(ES3.KeyExists("cosmeticUnlocks", _saveFileModded)){
                    List<string> _cosmeticUnlocks = ES3.Load<List<string>>("cosmeticUnlocks", _saveFileModded);
                    MetaManager.instance.cosmeticUnlocks = MetaManager.instance.cosmeticUnlocks
                        .Concat(_cosmeticUnlocks.Where(IsValidCosmetic).Select(e => MetaManager.instance.cosmeticAssets.FindIndex(a => a.assetId == e)))
                        .Distinct().ToList();
                    missingCosmeticUnlocks = _cosmeticUnlocks.Where(x => !IsValidCosmetic(x)).ToList();
                }
                if(ES3.KeyExists("cosmeticHistory", _saveFileModded)){
                    List<string> _cosmeticHistory = ES3.Load<List<string>>("cosmeticHistory", _saveFileModded);
                    MetaManager.instance.cosmeticHistory = MetaManager.instance.cosmeticHistory
                        .Concat(_cosmeticHistory.Where(IsValidCosmetic).Select(e => MetaManager.instance.cosmeticAssets.FindIndex(a => a.assetId == e)))
                        .Distinct().ToList();
                    missingCosmeticHistory = _cosmeticHistory.Where(x => !IsValidCosmetic(x)).ToList();
                }
                #endregion

                #region Overwrite
                if(ES3.KeyExists("cosmeticEquipped", _saveFileModded)){
                    List<string> _cosmeticEquipped = ES3.Load<List<string>>("cosmeticEquipped", _saveFileModded);
                    MetaManager.instance.cosmeticEquipped = _cosmeticEquipped.Where(IsValidCosmetic).Select(e => MetaManager.instance.cosmeticAssets.FindIndex(a => a.assetId == e)).ToList();
                    missingCosmeticEquipped = _cosmeticEquipped.Where(x => !IsValidCosmetic(x)).ToList();
                }

                if(ES3.KeyExists("cosmeticPresets", _saveFileModded)){
                    var _cosmeticPresets = ES3.Load<List<List<string>>>("cosmeticPresets", _saveFileModded);
                    if(_cosmeticPresets != null){
                        int _minLength = Mathf.Min(MetaManager.instance.cosmeticPresets.Count, _cosmeticPresets.Count);
                        for(int i = 0; i < _minLength; i++){
                            MetaManager.instance.cosmeticPresets[i] = _cosmeticPresets[i].Where(IsValidCosmetic).Select(e => MetaManager.instance.cosmeticAssets.FindIndex(a => a.assetId == e)).ToList();
                            missingCosmeticPresets[i] = _cosmeticPresets[i].Where(x => !IsValidCosmetic(x)).ToList();
                        }
                    }
                }

                if(ES3.KeyExists("colorPresets", _saveFileModded)){
                    var _colorPresets = ES3.Load<List<List<int>>>("colorPresets", _saveFileModded);
                    if(_colorPresets != null){
                        int _minLength = Mathf.Min(MetaManager.instance.colorPresets.Count, _colorPresets.Count);
                        for(int i = 0; i < _minLength; i++){
                            MetaManager.instance.colorPresets[i] = _colorPresets[i];
                        }
                    }
                }

                if(ES3.KeyExists("colorsEquipped", _saveFileModded)){
                    var _colorsEquipped = ES3.Load<int[]>("colorsEquipped", _saveFileModded);
                    if(_colorsEquipped != null){
                        int _minLength = Mathf.Min(MetaManager.instance.colorsEquipped.Length, _colorsEquipped.Length);
                        for(int i = 0; i < _minLength; i++){
                            MetaManager.instance.colorsEquipped[i] = _colorsEquipped[i];
                        }
                    }
                }
                #endregion
            }else{
                MetaManager.instance.Save();
            }
        }catch(System.Exception ex){
            Logger.LogError("Failed to load modded meta save: " + ex.Message);
            ES3.DeleteFile(_savePathModded);
            MetaManager.instance.Save();
        }
    }
}