# 注意
このパッケージは十分に検証できていません。

# TMPro_LocaleFontLoader
ゲーム起動時や言語変更時に適切な FallbackFont を Addressables で読み込む Unity の TextMeshPro 用パッケージです。
必要な言語のテクスチャだけを読み込み、処理負荷が高い文字テクスチャの動的生成頻度を減らせます。

参考にさせていただいたブログ：[きゅぶろぐ　TextMeshProカテゴリ](https://blog.kyubuns.dev/archive/category/TextMeshPro)

# 使い方

## インポート
Package Manager の```Add package from git URL```に下記のURLを入力してください。
```
https://github.com/yamara-mh/TMPro_LocaleFontLoader/tree/feature.git?path=Assets/TMPro_LocaleFontLoader
```

また Dynamic な TMPro フォントはテクスチャが勝手に書き変わるため Git と相性が悪いです。
**下記のような対策パッケージのインポートを推奨します。**

https://github.com/STARasGAMES/tmpro-dynamic-data-cleaner.git#upm

## ファイルの生成と設定
![image](https://github.com/yamara-mh/TMPro_LocaleFontLoader/assets/39893033/67cb7318-7886-4101-a9ab-8598a5489626)

Resources 上で```Create -> ScriptableObject -> LocaleFontLoader```を選択すると、上記の ScriptableObject が生成されるので、下記の表を参考に設定します。

|項目|型|説明|
|-|-|-|
|BaseFont|TMP_FontAsset|全ての言語で共通して使う TMP_FontAssets を設定します。一般的には Extended ASCII のみを収録した TMP_FontAsset を設定します。動的フォントを Fallback に設定すると、編集中にテキストを表示する場合に便利です。ビルド時に Fallback は LocaleFontLoader_FallbackRemoverEditor によって一時的に空になります。そのためローカルと Addressable で二重に保存される心配はありません。|
|DynamicFontRef|AssetReferenceT<br><TMP_FontAsset>|多言語フォントを使った Dynamic な TMP_FontAsset を設定します。|
|LoadOnInit|bool|起動時(```RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)```)に Fallback を読み込む処理を追加します。特定の場面でのみ使うフォントなら無効でも良さそうです。|
|ReloadOnLocaleChanged|bool|言語切替時(```LocalizationSettings.SelectedLocaleChanged```)に Fallback を読み込む処理を追加します。特定の場面でのみ使うフォントなら無効でも良さそうです。|
|UpdateAllTextOnLoaded|bool|Fallback の読み込み完了時にシーン上の TextMeshPro を取得し、BaseFont が使われていたら ForceMeshUpdate() します。この処理は Localization Table が先に反映されてもフォントを更新可能にするためにあります。言語切替後に再起動する仕様等であれば無効でも良さそうです。|
|LocaleFonts|省略|文字種が多い言語の TMP_FontAssets を設定します。|

そして TextMeshPro の FontAsset に BaseFont を指定すると、ゲーム起動時や言語変更時に適切な FallbackFont を扱えるようになります。

TMP Settings ファイルの Default Font Asset に BaseFont を指定すると、TextMeshPro 生成時 FontAsset に自動で BaseFont が代入されて便利です。

### 各フォントの設定例
BaseFont
- Atlas Population Mode : Static
- Character Set : Extended ASCII

Dynamic Font
- Atlas Population Mode : Dynamic
- Character Set : Custom Characters & 何も指定しない
- Multi Atlas Textures : 有効
- Clear Dynamic Data On Build : 有効

Locale Fonts
- Atlas Population Mode : Static
- Character Set : Custom Characters & 頻出文字群

#### Locale Font の日本語で収録する文字に関して

通常は[常用漢字を突っ込むのはもうやめ！Adobeが定めた良い感じの日本語文字セットをTextMeshProで使う](https://blog.kyubuns.dev/entry/2021/01/20/090740)を参考に収録すると良さそうです。

上記の日本語文字セットに収録されていない文字を使っていないか不安であれば、[Localization Table から出力した文字セット](https://docs.unity.cn/Packages/com.unity.localization@1.0/manual/StringTables.html#Preloading:~:text=with%20the%20data.-,Character%20Sets,-Sometimes%20we%20need)も加えると確実です。

WegGL のようにメモリの節約が重要なケースでは、Adobeの文字セットの代わりに[文化庁資料「漢字出現頻度数調査について」から、漢字のみを出現頻度順に並べたもの](https://gist.github.com/oktopus1959/272c960ccfe03453bb975d1e994cb99d9)を収録すると良さそうです。

## TemporaryLoader
![image](https://github.com/yamara-mh/TMPro_LocaleFontLoader/assets/39893033/e53203c3-a07b-447a-8cb5-28994ca2a51b)

特定の場面のみ使うフォントを読み込むコンポーネントを用意しました。
|項目|説明|
|-|-|
|ScopeMode|フォントの読み込み、破棄のタイミングを決める設定です。<br>Instance : このコンポーネントの Start() と OnDestroy() のタイミングで読込、破棄が行われます。<br>Active : このコンポーネントの OnEnable() と OnDisable() のタイミングで読込、破棄が行われます。<br>Manual : 手動で関数を呼んで読込、破棄を行います。|
|UpdateAllTextOnLoaded|Fallback の読み込み完了時にシーン上の TextMeshPro を取得し、BaseFont が使われていたら ForceMeshUpdate() します。|
|FontLoaders|対象となる LocaleFontLoader を設定します。|
