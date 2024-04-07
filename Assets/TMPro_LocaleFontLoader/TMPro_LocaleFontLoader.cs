using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using System.Threading;


namespace Yamara.TMPro
{
    /// <summary>
    /// This class loads the TMP_FontAsset set from Addressable by LocalizationSettings.SelectedLocaleChanged.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(TMPro_LocaleFontLoader), menuName = nameof(ScriptableObject) + "/" + nameof(TMPro_LocaleFontLoader))]
    public class TMPro_LocaleFontLoader : ScriptableObject
    {
        [Header("Place this inside Resources")]
        [SerializeField, Tooltip(
            "Specify a Static TMP_FontAsset to be used in all languages. Example: Extended ASCII\n" +
            "Setting the dynamic font to fallback is useful for displaying text while editing. " +
            "When building, Fallback will be temporarily emptied by LocaleFontLoader_FallbackRemoverEditor, " +
            "so there is no need to worry about it being saved twice locally and in Addressable.")]
        public TMP_FontAsset BaseFont;
        [SerializeField, Tooltip("Specify a Dynamic TMP_FontAsset that uses multilingual fonts.")]
        public AssetReferenceT<TMP_FontAsset> DynamicFontRef;

        [SerializeField, Tooltip("Add Fallback load processing to RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)")]
        public bool LoadOnInit = true;
        [SerializeField, Tooltip("Add Fallback reload processing to LocalizationSettings.SelectedLocaleChanged")]
        public bool ReloadOnLocaleChanged = true;
        [SerializeField, Tooltip("Get all TMPros and update the necessary meshes when loading is complete")]
        public bool UpdateAllTextOnLoaded = true;

        [SerializeField]
        public List<LocaleFontData> LocaleFonts;
        [Serializable]
        public class LocaleFontData
        {
            public LocaleIdentifier Identifier;
            public AssetReferenceT<TMP_FontAsset> FontRef;
            public AssetReferenceT<TMP_FontAsset> OverrideDynamicFontRef;
        }


        private List<AssetReferenceT<TMP_FontAsset>> currentLocaleFontRefs = new();
        public bool IsLoading { get; private set; }
        public bool IsLoaded { get; private set; }
        public Action OnLoaded;

        public static event Action<Locale> OnLoadedAllFonts;
        public static bool IsLoadedFonts { get; private set; }


        private static TMPro_LocaleFontLoader[] localeFontLoaders;


#if UNITY_EDITOR
        private static bool isQuitting = false;
        private static Dictionary<TMPro_LocaleFontLoader, List<TMP_FontAsset>> fallbackFontsEditor = new();
#endif


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void EnterPlay()
        {
#if UNITY_EDITOR
            InitEditor();
            Application.quitting += ApplicationQuittingEditor;
#endif
            LocalizationSettings.SelectedLocaleChanged += UpdateAllFont;
            await LocalizationSettings.InitializationOperation;
            UpdateAllFont(true);
        }
#if UNITY_EDITOR
        private static void InitEditor()
        {
            localeFontLoaders = Resources.LoadAll<TMPro_LocaleFontLoader>("");
            foreach (var loader in localeFontLoaders)
            {
                fallbackFontsEditor.Add(loader, new(loader.BaseFont.fallbackFontAssetTable));
                loader.BaseFont.fallbackFontAssetTable.Clear();
            }
            if (localeFontLoaders.Length > 0)
            {
                var log = new System.Text.StringBuilder($"Emptied the fallbackFontAssetTable of {localeFontLoaders.Length} TMP_FontAssets at play time:\n");
                for (int i = 0; i < localeFontLoaders.Length; i++) log.AppendLine(localeFontLoaders[i].BaseFont.name);
                Debug.Log($"[{nameof(TMPro_LocaleFontLoader)}] {log}");
            }
        }
        private static void ApplicationQuittingEditor()
        {
            isQuitting = true;
            OnLoadedAllFonts = null;
            IsLoadedFonts = false;
            if (fallbackFontsEditor.Count() > 0)
            {
                foreach (var loader in localeFontLoaders) loader.BaseFont.fallbackFontAssetTable = fallbackFontsEditor[loader];
            }
            fallbackFontsEditor.Clear();
            localeFontLoaders = null;
            Application.quitting -= ApplicationQuittingEditor;
        }
#endif

        public static void UpdateAllFont(Locale locale = null) => UpdateAllFont(false, locale);
        public static async void UpdateAllFont(bool isInit, Locale locale = null, CancellationToken cancellationToken = default)
        {
            IsLoadedFonts = false;

            var updates = new List<TMP_FontAsset>();
            localeFontLoaders ??= Resources.LoadAll<TMPro_LocaleFontLoader>("");
            foreach (var loader in localeFontLoaders)
            {
                if (isInit && loader.LoadOnInit == false) continue;
                if (loader.ReloadOnLocaleChanged)
                {
                    await loader.LoadFontsAsync(locale);
                    if (cancellationToken.IsCancellationRequested) return;
                    if (loader.UpdateAllTextOnLoaded) updates.Add(loader.BaseFont);
                }
            }
            if (updates.Count > 0) UpdateAllText(updates);
            IsLoadedFonts = true;
            OnLoadedAllFonts?.Invoke(LocalizationSettings.SelectedLocale);
        }
        public static void UpdateAllText(IEnumerable<TMP_FontAsset> fonts)
        {
            var fontHashSet = new HashSet<TMP_FontAsset>();
            foreach (var font in fonts) fontHashSet.Add(font);

            foreach (TMP_Text tmpText in FindObjectsOfType(typeof(TMP_Text)))
            {
                if (fontHashSet.Contains(tmpText.font)) tmpText.ForceMeshUpdate();
            }
        }

        public async UniTask LoadFontsAsync(Locale locale = null, CancellationToken cancellationToken = default)
        {
            if (IsLoading) return;
            IsLoading = true;
            IsLoaded = false;

            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            if (cancellationToken.IsCancellationRequested) return;
            BaseFont.fallbackFontAssetTable.Clear();

            var overrideDynamicFontFlag = false;
            var nextLocaleFontRefs = new List<AssetReferenceT<TMP_FontAsset>>();
            foreach (var item in LocaleFonts.Where(l => l.Identifier == locale.Identifier))
            {
                if (item.FontRef != null && item.FontRef.RuntimeKeyIsValid())
                {
                    if (item.FontRef.Asset == null) await item.FontRef.LoadAssetAsync();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnloadAllFallback();
                        return;
                    }
                    if (item.FontRef.Asset != null)
                    {
                        BaseFont.fallbackFontAssetTable.Add(item.FontRef.Asset as TMP_FontAsset);
                        nextLocaleFontRefs.Add(item.FontRef);
                    }
                }

                if (item.OverrideDynamicFontRef != null && item.OverrideDynamicFontRef.RuntimeKeyIsValid())
                {
                    if (item.OverrideDynamicFontRef.Asset == null) await item.OverrideDynamicFontRef.LoadAssetAsync();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnloadAllFallback();
                        return;
                    }
                    if (item.OverrideDynamicFontRef.Asset != null)
                    {
                        overrideDynamicFontFlag = true;
                        BaseFont.fallbackFontAssetTable.Add(item.OverrideDynamicFontRef.Asset as TMP_FontAsset);
                        nextLocaleFontRefs.Add(item.OverrideDynamicFontRef);
                    }
                }
            }

            if (overrideDynamicFontFlag == false)
            {
                if (DynamicFontRef != null && DynamicFontRef.RuntimeKeyIsValid())
                {
                    if (DynamicFontRef.Asset == null) await DynamicFontRef.LoadAssetAsync();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnloadAllFallback();
                        return;
                    }
                    if (DynamicFontRef.Asset != null)
                    {
                        BaseFont.fallbackFontAssetTable.Add(DynamicFontRef.Asset as TMP_FontAsset);
                        nextLocaleFontRefs.Add(DynamicFontRef);
                    }
                }
            }

            foreach (var releaseFontRef in currentLocaleFontRefs.Except(nextLocaleFontRefs)) if (releaseFontRef.Asset != null) releaseFontRef.ReleaseAsset();
            currentLocaleFontRefs = nextLocaleFontRefs;

            IsLoading = false;
            IsLoaded = true;
            OnLoaded?.Invoke();
        }
        public void UpdateTexts()
        {
            foreach (TMP_Text tmpText in FindObjectsOfType(typeof(TMP_FontAsset)))
            {
                if (tmpText.font == BaseFont) tmpText.ForceMeshUpdate();
            }
        }
        public void UnloadAllFallback()
        {
            IsLoaded = false;
#if UNITY_EDITOR
            if (isQuitting) return;
#endif
            foreach (var fontRef in currentLocaleFontRefs.Where(r => r.Asset != null)) fontRef.ReleaseAsset();
            currentLocaleFontRefs.Clear();

            BaseFont.fallbackFontAssetTable.Clear();
        }
    }
}