# LocaleFallbackFontLoader

ゲーム起動時や言語変更時に適切な FallbackFont を Addressables で読み込む Unity の TextMeshPro 用パッケージです。
必要な言語のテクスチャだけを読み込み、文字メッシュの動的生成数を減らせます。
全ての TMP_FontAsset や Font を Remote に置くことも実質可能です（多分）。

参考にさせていただいたブログ：[きゅぶろぐ　TextMeshProカテゴリ](https://blog.kyubuns.dev/archive/category/TextMeshPro)

# 使い方

## インポート

Package Manager の```Add package from git URL```に下記のURLを入力してください。
```
// TODO
```

また Dynamic な TMPro フォントはテクスチャが勝手に書き変わるため Git と相性が悪いです。
**下記のような対策パッケージのインポートを推奨します。**

https://github.com/STARasGAMES/tmpro-dynamic-data-cleaner.git#upm

## ファイルの生成と設定

Resources 上で```Create -> ScriptableObject -> LocaleFontLoader```を選択すると、下記の ScriptableObject が生成されます。

![image](https://github.com/yamara-mh/TMPro_LocaleFontLoader/assets/39893033/eb1ab188-0373-41fc-affd-8d288af5b7a4)

BaseFont に 下記のような TMP_FontAsset を用意して設定します。
- Atlas Population Mode : Static
- Character Set : Extended ASCII

どの言語でも利用する Dynamic な TMP_FontAsset を用意して```Dynamic Font Ref```に設定します。
- Atlas Population Mode : Dynamic
- Character Set : Unicode Range(Hex) & 何も指定しない
- Multi Atlas Textures : 有効
- Clear Dynamic Data On Build : 有効

文字種が多い言語の TMP_FontAsset を用意して```Locale Fonts```に設定します。
- Atlas Population Mode : Static
- Character Set : Unicode Range(Hex) & 頻出文字群 ([日本語の例](https://gist.github.com/oktopus1959/272c960ccfe03453bb975d1e994cb99d))


|項目|型|説明|
|-|-|-|
|BaseFont|TMP_FontAsset|全ての言語で共通して使う TMP_FontAssets を設定します。一般的には Extended ASCII のみを収録した TMP_FontAsset を設定します。動的フォントを Fallback に設定すると、編集中にテキストを表示する場合に便利です。ビルド時に Fallback は LocaleFontLoader_FallbackRemoverEditor によって一時的に空になります。そのためローカルと Addressable で二重に保存される心配はありません。|
|DynamicFontRef|AssetReferenceT<br><TMP_FontAsset>|多言語フォントを使った Dynamic な TMP_FontAsset を設定します。|
|LoadOnInit|bool|起動時(```RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)```)に Fallback を読み込む処理を追加します。特定の場面でのみ使うフォントなら無効でも良さそうです。|
|ReloadOnLocaleChanged|bool|言語切替時(```LocalizationSettings.SelectedLocaleChanged```)に Fallback を読み込む処理を追加します。特定の場面でのみ使うフォントなら無効でも良さそうです。|
|UpdateAllTextOnLoaded|bool|Fallback の読み込み完了時にシーン上の TextMeshPro を取得し、BaseFont が使われていたら ForceMeshUpdate() します。この処理は Localization Table が先に反映されてもフォントを更新可能にするためにあります。言語切替後に再起動する仕様等であれば無効でも良さそうです。|
|LocaleFonts|省略|文字種が多い言語の TMP_FontAssets を設定します。|

### 備考

Sampling Point Size と Padding の比率が違うほどアウトラインの太さに違いが出るので、収録文字数や各パラメータを調整して妥協点を見つけてください。

[Localization Tables で使われた文字の一覧を言語ごとに出力する事が可能です。](https://docs.unity.cn/Packages/com.unity.localization@1.0/manual/StringTables.html)


## 利用



