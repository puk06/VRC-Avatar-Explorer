# Avalonia Style / Template 入門ガイド

[← ドキュメント一覧に戻る](../README.md)

このドキュメントは、AvatarExplorerのUI（`AvatarExplorer.UI`）を読み書きするときに避けて通れない
**Avaloniaの Style と Template** を、このリポジトリの実コードを例にしながら整理した学習用メモです。

- 対象: C#は分かるが、AvaloniaのXAMLは雰囲気で読んでいる人
- 前提: Avalonia 12.x（このプロジェクトは 12.1.4）
- 方針: 「用語の整理」→「仕組みの理解」→「実コードの解説」→「練習問題」の順で進みます

> コード例の行番号は執筆時点のものです。実際に読むときは `x:Name` や `Selector` の文字列で検索してください。

---

## 目次

1. [30秒で全体像](#1-30秒で全体像)
2. [Styleの基本](#2-styleの基本)
3. [セレクタ（対象の選び方）](#3-セレクタ対象の選び方)
4. [優先順位とスコープ](#4-優先順位とスコープ)
5. [疑似クラスとClasses](#5-疑似クラスとclasses)
6. [色の指定（DynamicResource）](#6-色の指定dynamicresource)
7. [コントロールとテンプレートの2層構造](#7-コントロールとテンプレートの2層構造)
8. [ControlTemplateの読み方](#8-controltemplateの読み方)
9. [/template/ セレクタ](#9-template-セレクタ)
10. [PART_ 名前規約](#10-part_-名前規約)
11. [3種類の「テンプレート」](#11-3種類のテンプレート)
12. [ContentPresenter と ItemsPresenter](#12-contentpresenter-と-itemspresenter)
13. [TemplateBinding と Binding](#13-templatebinding-と-binding)
14. [実例解説: サイドパネルのタブ](#14-実例解説-サイドパネルのタブ)
15. [よくある誤解 Q&A](#15-よくある誤解-qa)
16. [Styleが効かないときのチェックリスト](#16-styleが効かないときのチェックリスト)
17. [デバッグ方法](#17-デバッグ方法)
18. [このプロジェクトのUIルール](#18-このプロジェクトのuiルール)
19. [用語集](#19-用語集)
20. [練習問題](#20-練習問題)
21. [チートシート](#21-チートシート)

---

## 1. 30秒で全体像

Avaloniaの見た目は、**2つのレイヤー**に分かれています。

```
┌───────────────────────────────────────────────┐
│ コントロール（例: Button）                      │
│  「何ができるか」＝ 機能とプロパティ              │
│   Content, Command, IsEnabled, Background ... │
│                                               │
│  ┌─────────────────────────────────────────┐  │
│  │ テンプレート（ControlTemplate）           │  │
│  │  「どう描くか」＝ 見た目の設計図           │  │
│  │   Border / Grid / ContentPresenter ...  │  │
│  └─────────────────────────────────────────┘  │
└───────────────────────────────────────────────┘
        ↑ 上から下へ「設定」するのが Style の Setter
```

- **Style** = 「条件に合うコントロールに、プロパティをまとめて設定する」仕組み（CSSに近い）
- **ControlTemplate** = 「そのコントロールの中身をどう組み立てるか」の設計図
- **ContentPresenter / ItemsPresenter** = テンプレート内に開ける「中身を表示する穴」
- **TemplateBinding** = テンプレートの中から「持ち主のプロパティ」を参照する記法

この4つが分かれば、このプロジェクトのXAMLはほぼ読めます。

---

## 2. Styleの基本

### 2.1 構造は2つだけ

```xml
<Style Selector="Border.panelbackground">          <!-- ① どれに -->
    <Setter Property="Background" Value="{DynamicResource Background.Window}"/>  <!-- ② 何を -->
    <Setter Property="CornerRadius" Value="15"/>
</Style>
```

- **Selector** … 対象を選ぶ条件
- **Setter** … 設定するプロパティと値。1つのStyleに複数書ける

実物は `AvatarExplorer.UI/App.axaml:411` にあります。使う側はクラスを付けるだけです。

```xml
<Border Classes="panelbackground"> ... </Border>
```

### 2.2 CSSとの違い

| | CSS | Avalonia Style |
|---|---|---|
| 設定できるもの | CSSプロパティ（固定） | **そのコントロールのCLRプロパティ**（`Background` 等） |
| 値の型 | 文字列 | **型でチェックされる**（Brush、double など） |
| 継承 | 多くのプロパティが継承 | 一部のみ（`Foreground`、`FontSize` など） |
| 擬似クラス | `:hover` | `:pointerover` など |
| テンプレート | なし | **あり（ここが最大の違い）** |

`Value` に型が合わないものを書くと、実行時ではなく**バインディング/設定時**にエラーになります。

### 2.3 Styleを書ける場所

| 場所 | 例 | 影響範囲 |
|---|---|---|
| `Application.Styles` | `App.axaml:388` 以降 | アプリ全体 |
| `Window.Styles` / `UserControl.Styles` | `MainView.axaml:17` | その画面の子孫 |
| 任意のコントロールの `.Styles` | `ErrorLogOverlay.axaml:28` の `DataGrid.Styles` | そのコントロールの子孫 |

```xml
<!-- 画面ローカルの上書き例: AdvancedSearch.axaml:11 -->
<UserControl.Styles>
    <Style Selector="Border.negationtag">
        <Setter Property="Background" Value="{DynamicResource NegationTag.Background}"/>
    </Style>
</UserControl.Styles>
```

**同じクラス名でも、スコープが狭い方が優先されます。** だから「アプリ共通の見た目」は `App.axaml`、「この画面だけの例外」は各画面の `UserControl.Styles` に書く、という使い分けになります。

### 2.4 Setterで指定できるもの

`Setter` はプロパティだけではありません。

```xml
<!-- 添付プロパティ（ToolTip.ShowDelay など）は括弧で囲む: App.axaml:552 -->
<Setter Property="(ToolTip.ShowDelay)" Value="1500"/>
```

```xml
<!-- アニメーション: App.axaml:439 -->
<Style Selector="Button.appear">
    <Style.Animations>
        <Animation Duration="0:0:0.4" Easing="CubicEaseOut" FillMode="Both">
            <KeyFrame Cue="0%"><Setter Property="TranslateTransform.Y" Value="12"/></KeyFrame>
            <KeyFrame Cue="100%"><Setter Property="TranslateTransform.Y" Value="0"/></KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

```xml
<!-- Templateの差し替え: MainView.axaml:42 -->
<Setter Property="Template"> ... </Setter>
```

---

## 3. セレクタ（対象の選び方）

### 3.1 一覧

| 書き方 | 意味 | このリポジトリの例 |
|---|---|---|
| `Button` | 型で選ぶ | `App.axaml:551` |
| `Button.button` | 型＋クラス | `App.axaml:429` |
| `Button#okButton` | 型＋`x:Name` | — |
| `Button:pointerover` | 状態（疑似クラス） | `App.axaml:452` |
| `Button:disabled` | 無効状態 | `App.axaml:435` |
| `A B`（スペース） | **子孫**（Aの中のどこかにあるB） | `MainView.axaml:55` |
| `A > B` | **直接の子**だけ | — |
| `A, B` | どちらか（OR） | — |
| `A /template/ B` | **テンプレート内部**のB | `App.axaml:457` |
| `:not(:pointerover)` | 否定 | — |
| `A.b.c` | クラス2つ（AND） | `App.axaml:570` |

### 3.2 クラスは複数付けられる

```xml
<TextBlock Classes="settingslabel title"/>
```

```xml
<Style Selector="TextBlock.settingslabel"> ... </Style>          <!-- 両方に効く -->
<Style Selector="TextBlock.settingslabel.title"> ... </Style>    <!-- title付きだけ上書き -->
```

`App.axaml:570` と `:577` がこのパターンです。CSSの `.settingslabel.title` と同じ。

### 3.3 子孫セレクタは「条件分岐」として使える

```xml
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected">
```

日本語に訳すと：

> 「`expanded` クラスが付いた TabControl **の中にある**、`sidepaneltab` クラスを持ち、**選択中**の TabItem」

`expanded` は `Classes.expanded="{Binding IsSidePanelVisible}"`（`MainView.axaml:472`）で動的に付くので、
**「パネルが開いているときだけ選択タブを強調する」** という条件付きスタイルになります。

### 3.4 読み方練習

```xml
<Style Selector="Button.button:pointerover /template/ ContentPresenter">
```

分解すると：

```
Button                          … 型がButton
  .button                       … buttonクラスを持つ
  :pointerover                  … かつマウスが乗っている
  /template/                    … そのテンプレートの中へ入って
  ContentPresenter              … ContentPresenterに
```

→ **「マウスが乗っている `button` クラスのButtonの、テンプレート内のContentPresenter」**

---

## 4. 優先順位とスコープ

### 4.1 値の優先順位

同じプロパティに複数の値が来た場合、強い方が勝ちます。

```
強い
  ↑  1. XAMLに直接書いた値             <Border Background="Red"/>
     2. アニメーション
     3. 疑似クラス付きStyle            Button.button:pointerover
     4. 通常のStyle                    Button.button
     5. テーマの既定値（FluentTheme）
  ↓
弱い
```

**最重要ポイント:** XAMLに直接書いた値は最強です。

```xml
<!-- ❌ こう書くと、Styleで何をしても背景色は変わらない -->
<Button Classes="button" Background="Red"/>
```

（厳密には、テンプレート内の `TemplateBinding` 由来の値が通常Styleより少し強い、などもう少し細かい段階があります。
まずは上の5段階だけ覚えれば実用上は困りません。）

「Styleが効かない」と思ったら、まず**そのコントロールに直接値が書かれていないか**を確認してください。
`MainView.axaml` のTabItemが全部Setter経由で色を指定しているのは、この理由です。

### 4.2 同じ優先度なら？

- **後から適用された方が勝つ**
- 適用順は「外側（Application）→ 内側（UserControl）」なので、**よりローカルなStyleが後に来て勝つ**
- 疑似クラス付きStyleは「後から」ではなく**優先度自体が一段上**（4.1の3番目）

なので「共通の見た目をApp.axamlに書き、画面ごとに上書きする」が自然に成立します。

---

## 5. 疑似クラスとClasses

### 5.1 疑似クラス（Avaloniaが自動で付ける状態）

| 疑似クラス | いつ付くか |
|---|---|
| `:pointerover` | マウスが上にある |
| `:pressed` | 押されている |
| `:selected` | 選択中（TabItem、ListBoxItemなど） |
| `:disabled` | `IsEnabled="False"` |
| `:focus` / `:focus-visible` | フォーカスがある |
| `:empty` | 中身が空（ContentPresenterなど） |

### 5.2 自分で状態を作る `Classes.xxx`

ViewModelのbool値を「見た目のバリエーション」に変換する標準イディオムです。

```xml
<TabControl Classes.expanded="{Binding IsSidePanelVisible}">
```

```xml
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected"> ... </Style>
```

この仕組みのおかげで、コードビハインドに `if` を書かずに見た目を切り替えられます。

---

## 6. 色の指定（DynamicResource）

```xml
<Setter Property="Background" Value="{DynamicResource Button.Background}"/>
```

| 記法 | 動作 | テーマ切替への追従 |
|---|---|---|
| `{DynamicResource Key}` | 実行時に値を解決し、変更も監視 | **する** |
| `{StaticResource Key}` | 読み込み時に1回だけ解決 | しない |
| 直値 `"Red"` | 固定 | しない |

このアプリはテーマが多い（Light / Dark / Sakura / Mint / Lavender / Ocean / Sunset / Forest / Mocha / Slate）ので、
**色は必ず `DynamicResource`** になっています（`App.axaml:19` 以降の `ThemeDictionaries`）。

```xml
<ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
        <SolidColorBrush x:Key="Text.Primary" Color="rgba(26,26,26,1.0)"/>
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
        <SolidColorBrush x:Key="Text.Primary" Color="rgba(242,242,242,1.0)"/>
    </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

**ルール:** 色＝`DynamicResource`、サイズ・レイアウト＝直値。これがこのプロジェクトの統一ルールです。

---

## 7. コントロールとテンプレートの2層構造

ここがAvalonia最大のつまずきポイントです。

### 7.1 「Backgroundを設定したのに色が変わらない」理由

Buttonの `Background` は、**Buttonが自分で塗る色ではありません**。
標準テンプレートでは、中の `ContentPresenter` が `Background` を受け取って描画しています。

```
Button（Background = 青）
   └ ControlTemplate
        └ Border / ContentPresenter（ここが実際に青を塗る）
```

なので、`Button.Background` を変えても
**テンプレート内の要素がその値を使っていなければ見た目は変わりません。**

### 7.2 だから `/template/` が必要

`App.axaml:457` はこの問題への対処です。

```xml
<!-- Button自身に設定 -->
<Style Selector="Button.button">
    <Setter Property="Background" Value="{DynamicResource Button.Background}"/>
</Style>

<!-- テンプレート内部のContentPresenterにも設定 -->
<Style Selector="Button.button /template/ ContentPresenter">
    <Setter Property="Background" Value="{DynamicResource Button.Background}"/>
</Style>
```

> このプロジェクトでは `Button.button` / `accentbutton` / `implemented` / `notimplemented` の4系統すべてで
> 「本体＋`/template/ ContentPresenter`」のペアを書いています。これはおまじないではなく**必須**です。

### 7.3 テンプレートを差し替えた場合は別

`MainView.axaml:19` のように `Template` を丸ごと自作した場合、
「どの要素がどのプロパティを使うか」を自分で決めるので、この問題は起きません（自分で `TemplateBinding` を書く）。

---

## 8. ControlTemplateの読み方

### 8.1 テンプレートは「そのコントロール専用の画面」

```xml
<Setter Property="Template">
    <ControlTemplate>
        <Grid ColumnDefinitions="*,Auto">        <!-- 土台（普通のレイアウトパネル） -->
            <ContentPresenter .../>              <!-- 中身を出す穴 -->
            <ItemsPresenter .../>                <!-- 項目を並べる穴 -->
        </Grid>
    </ControlTemplate>
</Setter>
```

読み方のコツ：

1. **ルートは普通のレイアウトパネル**（Grid、Border、DockPanel、Panelなど）
2. そこに **`ContentPresenter` / `ItemsPresenter` という「穴」を開ける**
3. 穴に何を流すかを `TemplateBinding` で決める

つまり「ControlTemplate = コントロールのための小さなUserControl」と思うと理解しやすいです。

### 8.2 標準テンプレートを確認する方法

Avaloniaの標準テンプレートはGitHubで読めます。

- `src/Avalonia.Themes.Fluent/Controls/Button.xaml`
- `src/Avalonia.Themes.Fluent/Controls/TabItem.xaml`
- `src/Avalonia.Themes.Fluent/Controls/TabControl.xaml`
- https://github.com/AvaloniaUI/Avalonia

実行中なら **DevTools（F12）** の Visual Tree / Styles タブで、実際に適用されているテンプレートとStyleを確認できます。

### 8.3 テンプレートを差し替えるときの注意

- 元テンプレートにあった **`PART_` 要素は残す**（次章）
- 標準テンプレートがやっている **`TemplateBinding` は同じように書く**（機能を壊さないため）
- 状態（`:pointerover` / `:selected`）ごとの見た目は **自分でStyleを書く必要がある**

---

## 9. /template/ セレクタ

`/template/` は「**そのコントロールのテンプレートの中に入る**」という特別なセレクタです。

```xml
<!-- ScrollBarのテンプレート内にある、TrackRect という名前のRectangle: App.axaml:542 -->
<Style Selector="ScrollBar /template/ Rectangle#TrackRect">
    <Setter Property="IsVisible" Value="False"/>
</Style>
```

なぜ必要か：

- 通常の子孫セレクタ（スペース）は **論理ツリー** をたどる
- テンプレートの中身は通常の子として見えない（テンプレートは別の世界）
- だから `/template/` で「テンプレート内部」と明示する

```
ScrollBar
 └ /template/ で入れる → テンプレートの中
      ├ Rectangle#TrackRect   ← これ
      └ Thumb ...
```

---

## 10. PART_ 名前規約

`PART_` で始まる名前は**飾りではなく契約**です。

```xml
<ContentPresenter x:Name="PART_SelectedContentHost" .../>   <!-- MainView.axaml:23 -->
<ItemsPresenter   x:Name="PART_ItemsPresenter" .../>        <!-- MainView.axaml:28 -->
```

コントロール本体（C#側）は、テンプレートの中からこの名前の要素を探して使います。

| コントロール | 主なPART |
|---|---|
| TabControl | `PART_SelectedContentHost`, `PART_ItemsPresenter` |
| ScrollViewer | `PART_ScrollContentPresenter`, `PART_VerticalScrollBar` ... |
| TextBox | `PART_TextPresenter`, `PART_ScrollViewer` |
| ScrollBar | `PART_Track`, `PART_Thumb` |

**ルール:**

- 名前を変える → 機能が壊れる（タブの内容が出ない等）
- 消したい場合は **削除せず `IsVisible="False"`** にする
- 自分でテンプレートを書くときも、標準テンプレートのPART名をそのまま使う

---

## 11. 3種類の「テンプレート」

「テンプレート」という言葉が3種類出てくるので整理します。

| 名前 | 型 | 役割 | 定義する場所 |
|---|---|---|---|
| **ControlTemplate** | `ControlTemplate` | コントロールの見た目の骨組み | `ControlTheme` / `Style` の `Setter` |
| **DataTemplate** | `DataTemplate` | データ → UI の変換レシピ | `ItemTemplate`、`ContentTemplate` など |
| **ItemsPanelTemplate** | `ItemsPanelTemplate` | 項目を並べるPanelの作り方 | `ItemsPanel` |

### 11.1 プロパティ名に「Template」が付くもの

| プロパティ | 型 | 何のレシピか |
|---|---|---|
| `Template` | `ControlTemplate` | コントロール自体の見た目 |
| `ContentTemplate` | `DataTemplate` | コンテンツのデータ表示 |
| `ItemTemplate` | `DataTemplate` | 一覧の1項目の表示 |
| `HeaderTemplate` | `DataTemplate` | ヘッダーのデータ表示 |
| `IconTemplate` | `DataTemplate` | アイコンのデータ表示 |
| `ItemsPanel` | `ItemsPanelTemplate` | 項目を並べるPanel |
| `ItemContainerTheme` | `ControlTheme` | 各項目のコンテナ（ListBoxItem等）の見た目 |

### 11.2 具体例：DataTemplate

```xml
<!-- MainView.axaml:93 -->
<DataTemplate x:DataType="component:PathSegment">
    <Panel> ... </Panel>
</DataTemplate>
```

これは「`PathSegment` という**データ**を、どう画面に出すか」のレシピです。
`x:DataType` は**コンパイル時バインディング**の型指定で、間違ったプロパティ名を書くとビルド時にエラーになります（便利）。

`ItemTemplate` は `ItemsControl` が「項目1つにつき1回」このレシピを適用します。

### 11.3 `Content=` にControlを入れた場合と、データを入れた場合

`ContentPresenter` は次の順で中身を決めます（Avaloniaのソース `CreateChild` より）。

1. `Content` が**コントロール**で、`ContentTemplate` が **null** → そのコントロールをそのまま表示
2. `ContentTemplate` が**ある** → テンプレートを適用（コントロールでもテンプレートが優先）
3. どちらでもない → **型に合うDataTemplateをツリーから探す**
4. 見つからない → 既定変換（文字列ならTextBlock、それ以外は `ToString()`）

```xml
<!-- ① コントロールをそのまま -->
<TabItem.Icon>
    <materialIcons:MaterialIcon Kind="TextSearch"/>
</TabItem.Icon>

<!-- ② データ＋テンプレート -->
<TabItem Icon="{Binding UnreadCount}">
    <TabItem.IconTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" Foreground="Red"/>
        </DataTemplate>
    </TabItem.IconTemplate>
</TabItem>
```

**カスタムテンプレートを作るときは、`XxxTemplate` を `ContentPresenter` に中継するのを忘れない**
（`ContentTemplate="{TemplateBinding IconTemplate}"` のように）。忘れると、外から渡されたレシピが無視されます。

---

## 12. ContentPresenter と ItemsPresenter

### 12.1 違いは「1個」か「複数」か

| | ContentPresenter | ItemsPresenter |
|---|---|---|
| 表示するもの | **1つ**の内容 | **複数**の項目 |
| 主なプロパティ | `Content`, `ContentTemplate`, `Child`, `Padding`, `Background`, `CornerRadius` ... | `ItemsPanel`, `Panel`, `ItemsControl` のみ |
| 中身を作るのは | 自分 | **ItemsControl**（自分は置き場所を提供するだけ） |
| 項目ごとのコンテナ | 作らない | ItemsControlが作る（TabItem, ListBoxItem等） |
| 単体で使えるか | 使える | ItemsControlのテンプレート内でしか意味がない |

公式ドキュメントの説明：

- `ContentPresenter` … *"Presents a single item of data inside a TemplatedControl template."*
- `ItemsPresenter` … *"Presents items inside an ItemsControl."*

### 12.2 内部で起きること

**ContentPresenter（1個）**

```
Content
  ↓
ContentPresenter.UpdateChild()
  ├ コントロール → そのまま Child に
  └ データ       → DataTemplate を適用して Child に
  ↓
Child は常に1つ
```

**ItemsPresenter（複数）**

```
ItemsControl
  ├ ItemsSource を読む
  ├ ItemsPanel テンプレートから Panel を生成
  ├ ItemsPresenter.Panel にその Panel をセット
  └ 各項目のコンテナ（TabItem等）を生成して Panel.Children に追加
        ↑ PanelContainerGenerator の仕事
```

つまり **ItemsPresenterは項目を1つも知りません。** 「ここにPanelを置いて」という空き地です。

```xml
<!-- MainView.axaml:28 -->
<ItemsPresenter ItemsPanel="{TemplateBinding ItemsPanel}" Width="35"/>
```

これは「TabControlが持っている `ItemsPanel`（縦のStackPanel）をここに置いてください」の意味。

```xml
<!-- MainView.axaml:473 -->
<TabControl.ItemsPanel>
    <ItemsPanelTemplate>
        <StackPanel Spacing="10"/>
    </ItemsPanelTemplate>
</TabControl.ItemsPanel>
```

**並べ方（Panel）を決めるのは ItemsPanel、1項目の見た目を決めるのは ItemTemplate、項目一覧を持つのは ItemsControl。**
ItemsPresenterはそのどれでもありません。

### 12.3 なぜTabControlは両方使うのか

TabControlは「**N個の選択肢から1つを選ぶ**」コントロールです。

```
TabControl
├ 見出し（N個）      → ItemsPresenter
└ 本体（選択中の1個）  → ContentPresenter
```

```xml
<!-- MainView.axaml:22-28 -->
<ContentPresenter x:Name="PART_SelectedContentHost"
                  Content="{TemplateBinding SelectedContent}" .../>
<ItemsPresenter x:Name="PART_ItemsPresenter" .../>
```

`SelectedContent` は「選択中TabItemの `Content`」をTabControlがコピーしてくるプロパティです。
だからContentPresenter側は、選択が変われば自動で中身が入れ替わります。

同じ構図は `ListBox`（一覧＋選択項目の詳細）や `ComboBox`（一覧＋選択値）にもあります。

### 12.4 よくある間違い

```xml
<!-- ❌ ItemsPresenter は ItemsSource を持っていない -->
<ItemsPresenter ItemsSource="{Binding Items}" ItemTemplate="{Binding ...}"/>
```

`ItemsSource` と `ItemTemplate` は **ItemsControl側**（`TabControl` や `ListBox` タグ自身）に書きます。

---

## 13. TemplateBinding と Binding

```xml
Content="{TemplateBinding SelectedContent}"   <!-- ① -->
IsVisible="{Binding IsSidePanelVisible}"      <!-- ② -->
```

| | 何を参照するか | 方向 |
|---|---|---|
| `{TemplateBinding X}` | **テンプレートの持ち主（このコントロール自身）のプロパティX** | 一方向・軽量 |
| `{Binding X}` | **その要素のDataContext**（＝多くの場合ViewModel） | 双方向可・変換器等が使える |

- ①は「TabControlの `SelectedContent` を、テンプレートの外から受け取る」
- ②は「DataContext（MainViewModel）の `IsSidePanelVisible` を見る」

`{TemplateBinding X}` は `{Binding X, RelativeSource={RelativeSource TemplatedParent}}` の短縮形です。

### 13.1 使い分けの指針

| やりたいこと | 使うもの |
|---|---|
| コントロール自身のプロパティをテンプレート内に流す | `TemplateBinding` |
| ViewModelのデータを表示する | `Binding` |
| テンプレート内の要素に名前で触る | `x:Name`（C#側から `GetTemplateChild`） |

### 13.2 TemplateBindingは一方向

`TemplateBinding` は**片方向**です。テンプレート内で値を書き換えても、持ち主には反映されません。
双方向が必要な場面（TextBoxなど）は、標準テンプレート側で `Binding ... Mode=TwoWay` を使っています。

---

## 14. 実例解説: サイドパネルのタブ

`MainView.axaml:17-68` のStyle群を、上から順に意味を書きます。

### 14.1 `TabControl#SidePanelTabControl`（18-32行目）

```xml
<Style Selector="TabControl#SidePanelTabControl">
    <Setter Property="Template">
        <ControlTemplate>
            <Grid ColumnDefinitions="*,Auto">
                <ContentPresenter x:Name="PART_SelectedContentHost" .../>
                <ItemsPresenter x:Name="PART_ItemsPresenter" Grid.Column="1" Width="35" .../>
            </Grid>
        </ControlTemplate>
    </Setter>
</Style>
```

| 部分 | 意味 |
|---|---|
| `#SidePanelTabControl` | `x:Name` でこの1個だけを対象に |
| `Grid ColumnDefinitions="*,Auto"` | 左＝伸びる、右＝35px固定 |
| `PART_SelectedContentHost` | パネル本体（選択中の1つ） |
| `PART_ItemsPresenter` | タブ見出し（複数）を右端に縦並び |
| `IsVisible="{Binding IsSidePanelVisible}"` | 閉じているときは**非表示にするだけ**（破棄しない）→ 入力内容が保持される |
| `Margin="0,0,10,0"` | 本体とタブ帯の間の10px |

### 14.2 `TabItem.sidepaneltab`（33-51行目）

```xml
<Style Selector="TabItem.sidepaneltab">
    <Setter Property="Width" Value="35"/>
    <Setter Property="Height" Value="35"/>
    <Setter Property="MinWidth" Value="0"/>
    <Setter Property="MinHeight" Value="0"/>
    <Setter Property="Padding" Value="0"/>
    <Setter Property="Template">
        <ControlTemplate>
            <ContentPresenter
                Content="{TemplateBinding Icon}" ContentTemplate="{TemplateBinding IconTemplate}"
                Background="{TemplateBinding Background}" Foreground="{TemplateBinding Foreground}"
                BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" CornerRadius="8"
                HorizontalContentAlignment="Center" VerticalContentAlignment="Center"/>
        </ControlTemplate>
    </Setter>
</Style>
```

- 35×35の正方形を強制（既定のTabItemは文字幅でバラつく）
- テンプレートを「ContentPresenter1個」に置き換え → **アイコンだけの角丸ボタン**になる
- Iconは `TemplateBinding` で受け取る（Avalonia 12の `TabItem.Icon` プロパティ）

### 14.3 状態ごとのStyle（52-61行目）

```xml
<!-- ホバー -->
<Style Selector="TabItem.sidepaneltab:pointerover"> ... </Style>

<!-- 開いている時だけ選択中を強調 -->
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected"> ... </Style>

<!-- 選択中＋ホバー（より条件が厳しいので後に書く） -->
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected:pointerover"> ... </Style>
```

**「展開中」という状態を `Classes.expanded` で表現している**のがポイントです。

### 14.4 クリックで開閉する仕組み（コード側）

TabItemの見た目をボタンにしただけでは「クリック＝タブ選択」しか起きません。
このアプリでは `MainView.axaml.cs:228` で、各TabItemに**トンネリング**の `PointerPressed` を登録し、
`SidePanelManager.OnButtonPressed` で

- 閉じている → 開く
- 開いていて別のタブ → 切り替え
- 開いていて同じタブ → 閉じる

を実現しています（`SidePanelManager.cs:31`）。

---

## 15. よくある誤解 Q&A

**Q1. Styleを書いたのに見た目が変わらない**

A. 次の順で疑ってください。

1. そのコントロールに**直値**が書かれていないか（最強）
2. **テンプレート内部**の要素に設定すべきプロパティではないか → `/template/`
3. Selectorが合っているか（型・クラス名・`x:Name`の綴り）
4. Styleのスコープが届いているか（別ファイルの子孫ではないか）
5. 疑似クラスの付け忘れ（`:pointerover` など）

**Q2. `Button.Background` を変えたのに色が変わらない**

A. Buttonは自分で塗っていません。標準テンプレート内の `ContentPresenter` が塗っています。
`App.axaml:457` のように `/template/ ContentPresenter` も設定してください。

**Q3. `ItemsPresenter` に `ItemsSource` を書いたが何も出ない**

A. `ItemsPresenter` に `ItemsSource` はありません。`ItemsSource` / `ItemTemplate` は `ItemsControl` 側に書き、
`ItemsPresenter` には `ItemsPanel` だけを渡します。

**Q4. `IconTemplate` は「標準テンプレート」のこと？**

A. 違います。`IconTemplate` は **DataTemplate**（データ→UIのレシピ）で、
`Template`（ControlTemplate）とは別物です。

**Q5. TemplateBindingとBindingはどっちを使えばいい？**

A. テンプレート内から**コントロール自身のプロパティ**を使うなら `TemplateBinding`、
**ViewModelのデータ**を出すなら `Binding` です。

**Q6. PART_ って消しちゃダメ？**

A. ダメです。コントロール本体が名前で探しています。不要なら `IsVisible="False"` で隠します。

**Q7. 色を直値で書いてもいい？**

A. このプロジェクトではNGです。テーマ切替に追従しなくなるので、色は `DynamicResource` を使います。

**Q8. `Classes.expanded="{Binding ...}"` は何をしている？**

A. bool値に応じて `expanded` クラスを付け外ししています。Selector側で
`TabControl.expanded ...` と書くことで、ViewModelの状態をスタイルの条件にできます。

---

## 16. Styleが効かないときのチェックリスト

上から順に確認すると早いです。

- [ ] その要素に**直接値**を書いていないか
- [ ] Selectorの**型名**は合っているか（`Button` と `ToggleButton` など）
- [ ] **クラス名**の綴りは合っているか（`Classes="button"` と `.Button` など）
- [ ] `x:Name` を使うなら `#` を付けているか
- [ ] そのプロパティは**テンプレート内部**が描画していないか → `/template/`
- [ ] Styleの**スコープ**は届いているか（`UserControl.Styles` はその画面の子孫だけ）
- [ ] **疑似クラス**の状態になっているか
- [ ] テンプレートを差し替えていないか（差し替え後は標準のSetterが効かないことがある）
- [ ] `DynamicResource` の**キー名**は存在するか（タイプミスすると無言で無効）

---

## 17. デバッグ方法

### 17.1 DevTools（F12）

アプリ実行中に **F12** でAvalonia DevToolsが開きます（Debugビルド時）。

- **Visual Tree**: 実際の要素の階層。テンプレート内部も見える
- **Properties**: どのプロパティにどんな値が入っているか
- **Styles**: どのStyleが適用されているか、優先順位つきで確認できる

「この色はどこから来ているのか」を調べるのに最適です。

### 17.2 コード検索

クラス名や `x:Name` は文字列なので、grepで一発です。

```bash
# クラスを使っている箇所を探す
rg 'Classes="[^"]*panelbackground'

# 名前で探す
rg 'SidePanelTabControl'

# リソースキーの定義を探す
rg 'x:Key="Button.AccentBackground"'
```

### 17.3 ビルドでXAMLを検証

AvaloniaはXAMLをビルド時にコンパイルするので、**存在しないプロパティや型の不一致はビルドエラー**になります。

```bash
dotnet build AvatarExplorer.UI/AvatarExplorer.UI.csproj
```

`x:DataType` を指定した `{Binding}` は、**存在しないプロパティ名もコンパイル時に検出**されます。

### 17.4 標準テンプレートを読む

- https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Themes.Fluent/Controls
- `Button.xaml`、`TabItem.xaml`、`TabControl.xaml`、`ScrollBar.xaml` など

自作テンプレートを書くときは、まず標準を読んで `PART_` と `TemplateBinding` を確認するのが最短です。

---

## 18. このプロジェクトのUIルール

`App.axaml` と各画面を読んで読み取れる、暗黙のコーディング規約です。

| 項目 | ルール | 例 |
|---|---|---|
| 色 | 必ず `DynamicResource`（`App.axaml` の ThemeDictionaries） | `{DynamicResource Text.Primary}` |
| サイズ・余白 | 直値 | `Width="35"` |
| 共通の見た目 | `App.axaml` の `Application.Styles` にクラスStyleとして定義 | `Border.panelbackground` |
| 画面固有の例外 | 各画面の `UserControl.Styles` | `AdvancedSearch.axaml:11` |
| クラス名 | 小文字始まり（`panelbackground`, `accentbutton`, `sidepaneltab`） | — |
| アイコンボタン | `materialIcons:MaterialIcon` ＋ `Classes="button"` | `MainView.axaml:80` |
| パネル | `Border Classes="panelbackground"` | `MainView.axaml:73` |
| ボタンの色 | 本体Setter＋`/template/ ContentPresenter` のペア | `App.axaml:429-465` |
| 状態による見た目 | `Classes.xxx="{Binding ...}"` ＋ 子孫セレクタ | `MainView.axaml:472` |
| タブのアイコン | `TabItem.Icon`（Avalonia 12） | `MainView.axaml:479` |

---

## 19. 用語集

| 用語 | 意味 |
|---|---|
| **Style** | セレクタ＋Setterの組。条件に合うコントロールへプロパティを一括設定する |
| **Selector** | どのコントロールに適用するかの条件 |
| **Setter** | プロパティに値を設定する1行 |
| **疑似クラス** | `:pointerover` などの状態。コントロールが自動でON/OFFする |
| **Classes** | `Classes="xxx"` で付ける目印。複数可 |
| **ControlTemplate** | コントロールの見た目の骨組み |
| **ControlTheme** | コントロール型ごとの既定スタイル一式（Fluentテーマが提供） |
| **DataTemplate** | データ→UIの変換レシピ。`x:DataType` とセットで使うことが多い |
| **ItemsPanelTemplate** | 項目を並べるPanelの作り方 |
| **ContentPresenter** | 1つの内容を表示する穴 |
| **ItemsPresenter** | 複数項目を並べるPanelの置き場所 |
| **TemplateBinding** | テンプレート内から「持ち主のプロパティ」を参照する |
| **TemplatedParent** | テンプレートの持ち主（＝そのコントロール自身） |
| **PART_** | コントロール本体が名前で探すテンプレート内の要素 |
| **/template/** | テンプレート内部を指すセレクタ |
| **論理ツリー / 視覚ツリー** | 要素の親子関係。テンプレートは論理ツリーには現れない |
| **x:DataType** | コンパイル時バインディングの型指定 |
| **DynamicResource** | 実行時に解決し、変更にも追従するリソース参照 |

---

## 20. 練習問題

### 問題1

次のSelectorを日本語に訳してください。

```xml
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected:pointerover">
```

<details>
<summary>解答</summary>

「`expanded` クラスが付いた TabControl の中にある、`sidepaneltab` クラスを持ち、**選択中かつマウスが乗っている** TabItem」

`MainView.axaml:59` の実物です。「選択中のタブにホバーしても、ホバー色に負けずアクセント色を維持する」ためのStyleです。

</details>

### 問題2

`Button.Background` をSetterで設定したのに色が変わらないとき、何を追加すればよいですか？

<details>
<summary>解答</summary>

```xml
<Style Selector="Button.button /template/ ContentPresenter">
    <Setter Property="Background" Value="{DynamicResource Button.Background}"/>
</Style>
```

標準のButtonテンプレートでは、実際に背景を塗っているのが内側の `ContentPresenter` だからです。
`App.axaml:457` に実物があります。

</details>

### 問題3

`ItemsPresenter` に `ItemsSource` を書いても動かないのはなぜですか？

<details>
<summary>解答</summary>

`ItemsPresenter` は `ItemsSource` を持っていないからです。持っているのは `ItemsPanel` / `Panel` / `ItemsControl` だけ。
項目一覧の管理は `ItemsControl` の仕事で、`ItemsPresenter` は「項目を並べるPanelの置き場所」を提供するだけです。

</details>

### 問題4

`{TemplateBinding IconTemplate}` は何をしている？

<details>
<summary>解答</summary>

「テンプレートの持ち主（TabItem）の `IconTemplate` プロパティの値を、`ContentPresenter.ContentTemplate` にそのまま渡している」。
`IconTemplate` は DataTemplate（データ→UIのレシピ）で、ControlTemplateではありません。

</details>

---

## 21. チートシート

### Styleの骨格

```xml
<Style Selector="型.クラス:疑似クラス /template/ 要素#名前">
    <Setter Property="プロパティ" Value="値"/>
</Style>
```

### セレクタ

```
型            Button
クラス        Button.button          （複数可: Button.a.b）
名前          Button#okButton
疑似クラス    Button:pointerover
子孫          A B
直接の子      A > B
テンプレート  A /template/ B
OR            A, B
否定          :not(:pointerover)
```

### 優先順位（強い順）

```
XAML直値 > アニメーション > 疑似クラスStyle > 通常Style > テーマ既定
```

### テンプレートの種類

```
ControlTemplate      … 見た目の骨組み（Template）
DataTemplate         … データ→UI（ItemTemplate, ContentTemplate, IconTemplate ...）
ItemsPanelTemplate   … 並べるPanel（ItemsPanel）
```

### Presenter

```
ContentPresenter … 1つ。Content / ContentTemplate を持つ
ItemsPresenter   … 複数。ItemsPanel だけを持つ。Panelの置き場所
```

### テンプレート内の記法

```xml
Content="{TemplateBinding Content}"       <!-- 持ち主のプロパティを流す -->
ContentTemplate="{TemplateBinding ContentTemplate}"
ItemsPanel="{TemplateBinding ItemsPanel}"
IsVisible="{Binding IsSomething}"          <!-- DataContext（ViewModel）を見る -->
x:Name="PART_SelectedContentHost"          <!-- 本体が探す名前 -->
```

### 色

```xml
Value="{DynamicResource Text.Primary}"   <!-- テーマ追従: これを使う -->
Value="{StaticResource Text.Primary}"    <!-- 追従しない -->
```

### 状態をスタイルの条件にする

```xml
<TabControl Classes.expanded="{Binding IsSidePanelVisible}"/>
```
```xml
<Style Selector="TabControl.expanded TabItem.sidepaneltab:selected"> ... </Style>
```

---

## 参考リンク

- Avalonia 公式ドキュメント: https://docs.avaloniaui.net/docs/basics/user-interface/styling
- Avalonia リポジトリ（標準テンプレート）: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Themes.Fluent/Controls
- このリポジトリの実装例
  - `AvatarExplorer.UI/App.axaml` … 共通Styleとテーマ定義
  - `AvatarExplorer.UI/Views/MainView.axaml` … TabControlのテンプレート差し替え
  - `AvatarExplorer.UI/Views/MainView.axaml.cs` … SidePanelのクリック処理
  - `AvatarExplorer.UI/ViewModels/Managers/SidePanelManager.cs` … 開閉状態の管理
