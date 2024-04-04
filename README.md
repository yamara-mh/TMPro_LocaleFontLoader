# LocaleFallbackFontLoader

ゲーム起動時や言語変更時に適切な FallbackFont を Addressables で読み込む Unity の TextMeshPro 用パッケージです。
必要な言語のテクスチャだけを読み込み、文字メッシュの動的生成数を減らせます。
全ての TMP_FontAsset や Font を Remote に置くことも実質可能です（多分）。

参考ブログ：[本当に使える！TextMeshProでの「日本語」「多言語」対応方法](https://blog.kyubuns.dev/entry/2021/02/06/001609)

# 使い方

## インポート

Package Manager の```Add package from git URL```に下記のURLを入力してください。
```
// TODO
```

また Dynamic な TMPro フォントはテクスチャが勝手に書き変わり Git と相性が悪いです。
**下記のような対策できるパッケージもインポートしてください。**

https://github.com/STARasGAMES/tmpro-dynamic-data-cleaner.git#upm

## ファイルの生成と設定

Resources 上で```Create -> ScriptableObject -> LocaleFontLoader```を選択します。

![image](https://github.com/yamara-mh/FallbackFontLoader/assets/39893033/d8e5eea1-b082-45d3-b71f-a1fa0784edad)

RootEmptyFont に 下記のような TMP_FontAsset を用意して設定します。
- Source Font File : None
- Atlas Resolution : 8 x 8
- Character Set : Unicode Range(Hex) & 何も指定しない

どの言語でも利用する Static な TMP_FontAsset を用意して```Common Static Font Refs```に設定します。
- Atlas Population Mode : Static
- Character Set : Extended ASCII or ASCII

どの言語でも利用する Dynamic な TMP_FontAsset を用意して```Common Dynamic Font Refs```に設定します。
- Atlas Population Mode : Dynamic
- Character Set : Unicode Range(Hex) & 何も指定しない

対応言語ごとに TMP_FontAsset を用意して```Locale Fonts```に設定します。
- Atlas Population Mode : Static
- Character Set : Unicode Range(Hex) & 頻出文字群 ([日本語の例](https://gist.github.com/kyubuns/b06b84106a6b6791f6f4b194c98e42fd))

### 備考

```()_``` が含まれない TMP_FontAsset を使うと次の警告が出ます。
```The character used for Underline is not available in font asset ...```
この警告は```TMP Settings```の```Dynamic Font System Settings -> Disable warnings```を有効にして抑制できます。

Sampling Point Size と Padding の比率が違うほどアウトラインの太さに違いが出るので、各パラメータを調整して妥協点を見つけてください。

Extended ASCII には英数字と基本的な記号に加え、アクセント記号や特殊記号が含まれています。ゲームによっては ASCII を選択したり一部文字を減らしても良さそうです。

## 利用



