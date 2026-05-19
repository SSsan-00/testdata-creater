# HashMap Maker

`HashMap Maker` は、クエリ結果セットのテストデータを GUI で編集し、C# の `HashMap` 初期化コードとして出力する WinForms アプリです。

1 行の結果セットは `new HashMap`、複数行の結果セットは `new HashMap<HashMap>` として出力します。複数行の場合、外側の key は現在の表示順に従って `0, 1, 2...` に振り直されます。

## 動作環境

- .NET 9 SDK
- Windows 環境
- WinForms UI の起動は Windows が必要です

macOS / Linux でも Windows ターゲットの restore / build は可能ですが、画面の起動確認は Windows 上で行ってください。

## 導入手順

GitHub から取得できる場合:

```bash
git clone https://github.com/SSsan-00/testdata-creater.git
cd testdata-creater
dotnet build TestDataCreater.sln
```

リポジトリをダウンロードできない場合:

1. 空のフォルダを作成します。
2. [TestDataCreater.Bootstrap.csproj](TestDataCreater.Bootstrap.csproj) だけをそのフォルダへコピーします。
3. 次のコマンドを実行します。

```bash
dotnet build TestDataCreater.Bootstrap.csproj
dotnet build TestDataCreater.sln
```

bootstrap は solution、Core プロジェクト、WinForms プロジェクト、ソースファイルを現在のフォルダへ展開します。MSTest のテストソースは展開しません。

既存ファイルがある場合、bootstrap は上書きしません。再生成したい場合は次を実行してください。

```bash
dotnet build TestDataCreater.Bootstrap.csproj /p:BootstrapOverwrite=true
```

## 起動方法

Windows 上で次のコマンドを実行します。

```bash
dotnet run --project src/TestDataCreater/TestDataCreater.csproj
```

Visual Studio を使う場合は `TestDataCreater.sln` を開き、`TestDataCreater` プロジェクトを起動プロジェクトに設定して実行してください。

## 使い方

1. 左側の `ワークスペース` で結果セットを選択します。
2. `追加` で結果セットを追加します。
3. ワークスペース名を変更する場合は一覧の項目をダブルクリックします。
4. グリッド左上の固定操作エリアにある `+ 行` で行を追加します。
5. 各行の左端にある `- 行` で行を削除します。
6. `+ 列` の下にある `コピー` で、その行を複製して一つ下へ挿入します。
7. グリッド左上の固定操作エリアにある `+ 列` で列を追加します。
8. グリッド最上段の各列にある `- 列` で列を削除します。
9. グリッドのセルに値を入力します。
10. 列ヘッダーをクリックしてフィールド名を直接変更します。
11. 画面上部の `列名` でも選択列の名前を変更できます。
12. 画面上部の `型` で選択セルの値型を指定します。型名は C# に近い表記で表示されます。
13. `CSVインポート` でCSVファイルを選択すると、1行目を列名、2行目以降をデータとして表へ取り込みます。
14. 下部の `プレビュー` で C# 出力内容を確認します。グリッドとプレビューの境界を上下にドラッグすると表示領域を変更できます。
15. `コピー` でプレビュー内容をクリップボードへコピーします。

行をドラッグすると表示順を変更できます。列はヘッダーをドラッグして並び替えできます。

プレビューとコピーは常に現在の全レコードを出力します。セル入力中もプレビューは入力内容に追従します。

`型` の候補は入力値に応じて絞り込まれます。たとえば `01` のような文字列は数値型として選択できません。

CSVインポート時は、既存入力の有無に関係なく現在の表をCSVの内容で上書きします。セルの型は基本 `string` として扱い、`null` は `null`、C# の `int` として成立する値は `int` として取り込みます。

## 出力形式

1 行のみの場合:

```csharp
var data = new HashMap
{
    { "USER_ID", 1 },
    { "USER_NAME", "Alice" },
};
```

複数行の場合:

```csharp
var data = new HashMap<HashMap>
{
    { 0, new HashMap
        {
            { "USER_ID", 1 },
            { "USER_NAME", "Alice" },
        }
    },
    { 1, new HashMap
        {
            { "USER_ID", 2 },
            { "USER_NAME", "Bob" },
        }
    },
};
```

複数行出力時の外側 key は、GUI 上の表示順に従って `0` から連番で再採番します。

## 値型

セルごとに次の型を選択できます。

- `string`
- `int`
- `long`
- `decimal`
- `double`
- `bool`
- `DateTime`
- `Guid`
- `null`
- `DBNull.Value`
- `C# expression`

`C# expression` は任意の C# 式をそのまま出力するための型です。たとえば `OrderStatus.Completed` や `new Money(1200, "JPY")` のような、プロジェクト固有の型や enum を使う場合に指定します。

## データ保存

入力した workspace はアプリ終了時や編集操作時に自動保存されます。保存先はユーザーのアプリケーションデータフォルダ配下の `TestDataCreater/workspace.json` です。

アプリを再起動すると、前回の workspace、行、列、セル値、値型が復元されます。

## 開発者向け

通常ビルド:

```bash
dotnet build TestDataCreater.sln
```

テスト実行:

```bash
dotnet test TestDataCreater.sln
```

MSTest のテストは `tests/` 配下にあります。bootstrap で展開されるソースにはテストプロジェクトを含めていません。
