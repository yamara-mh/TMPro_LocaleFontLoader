using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Linq;

namespace Yamara.TMPro
{
    public class TMPro_LocaleFontLoader_TemporaryLoader : MonoBehaviour
    {
        public enum ScopeMode
        {
            Manual = 0,
            Instance = 1,
            Active = 2,
        }
        [SerializeField] private ScopeMode scopeMode = ScopeMode.Instance;

        [SerializeField, Tooltip("Get all TMPros and update the necessary meshes when loading is complete")]
        private bool updateAllTextOnLoad = true;
        [SerializeField] private List<TMPro_LocaleFontLoader> fontLoaders;

        public bool IsLoaded { get; private set; }
        public event Action OnLoaded;
        public event Action OnUnloaded;

        private CancellationTokenSource loadCancellationTokenSource;

        private async void Start()
        {
            if (scopeMode == ScopeMode.Instance)
            {
                loadCancellationTokenSource = new();
                await Activate();
            }
        }
        private void OnDestroy()
        {
            if (scopeMode == ScopeMode.Instance)
            {
                loadCancellationTokenSource?.Cancel();
                Deactivate();
            }
        }

        private async void OnEnable()
        {
            if (scopeMode == ScopeMode.Active)
            {
                loadCancellationTokenSource = new();
                await Activate();
            }
        }
        private void OnDisable()
        {
            if (scopeMode == ScopeMode.Active)
            {
                loadCancellationTokenSource?.Cancel();
                Deactivate();
            }
        }

        public async UniTask Activate()
        {
            LocalizationSettings.SelectedLocaleChanged += LoadFonts;
            await LoadFontsAsync(await LocalizationSettings.SelectedLocaleAsync);
        }
        public void Deactivate()
        {
            LocalizationSettings.SelectedLocaleChanged -= LoadFonts;
            UnloadFonts();
        }

        private async void LoadFonts(Locale locale) => await LoadFontsAsync(locale);
        private async UniTask LoadFontsAsync(Locale locale = null)
        {
            loadCancellationTokenSource?.Cancel();
            loadCancellationTokenSource = new();

            IsLoaded = false;
            foreach (var loader in fontLoaders) await loader.LoadFontsAsync(locale, loadCancellationTokenSource.Token);
            IsLoaded = true;

            OnLoaded?.Invoke();
            if (updateAllTextOnLoad) TMPro_LocaleFontLoader.UpdateAllText(fontLoaders.Select(l => l.BaseFont));
        }

        private void UnloadFonts()
        {
            IsLoaded = false;
            foreach (var loader in fontLoaders) loader.UnloadAllFallback();
            OnUnloaded?.Invoke();
        }
    }
}