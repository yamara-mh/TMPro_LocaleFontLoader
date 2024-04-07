#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Yamara.TMPro.Editor
{
    internal class TMPro_LocaleFontLoader_FallbackRemoverEditor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SavePath = "Temp/" + nameof(TMPro_LocaleFontLoader_FallbackRemoverEditor);
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) => Removes();
        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) => Adds();

        private static void Removes()
        {
            var storage = new FallbackFontTempStorage();
            var log = new StringBuilder();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(TMPro_LocaleFontLoader)}"))
            {
                var loader = AssetDatabase.LoadAssetAtPath<TMPro_LocaleFontLoader>(AssetDatabase.GUIDToAssetPath(guid));
                storage.Add(AssetDatabase.GetAssetPath(loader.BaseFont), loader.BaseFont);
                loader.BaseFont.fallbackFontAssetTable.Clear();
                EditorUtility.SetDirty(loader.BaseFont);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(storage));
            AssetDatabase.SaveAssets();

            log.AppendLine($"Emptied the fallbackFontAssetTable of {storage.Paths.Count} TMP_FontAssets at build time:");
            for (int i = 0; i < storage.Paths.Count; i++) log.AppendLine(storage.Paths[i]);
            Debug.Log($"[{nameof(TMPro_LocaleFontLoader_FallbackRemoverEditor)}] {log}");
        }
        private static void Adds()
        {
            var storage = JsonUtility.FromJson<FallbackFontTempStorage>(File.ReadAllText(SavePath));
            for (int i = 0; i < storage.Paths.Count; i++)
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(storage.Paths[i]);
                fontAsset.fallbackFontAssetTable = storage.GetFallbackList(i);
                EditorUtility.SetDirty(fontAsset);
            }
            AssetDatabase.SaveAssets();
            File.Delete(SavePath);
        }
    }

    [Serializable]
    class FallbackFontTempStorage
    {
        public List<string> Paths = new List<string>();
        public List<int> FallBacksCounts = new List<int>();
        public List<string> Fallbacks = new List<string>();
        internal void Add(string fontAssetPath, TMP_FontAsset fontAsset)
        {
            Paths.Add(fontAssetPath);
            FallBacksCounts.Add(fontAsset.fallbackFontAssetTable.Count());
            Fallbacks.AddRange(fontAsset.fallbackFontAssetTable.Select(f => AssetDatabase.GetAssetPath(f)).ToList());
        }
        internal List<TMP_FontAsset> GetFallbackList(int index)
            => Fallbacks
                .Skip(FallBacksCounts.Take(index).Sum())
                .Take(FallBacksCounts[index])
                .Select(path => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path))
                .ToList();
    }
}
#endif