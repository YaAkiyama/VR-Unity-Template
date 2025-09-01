# VR-Unity-Template

Meta Quest 3向けVRファイルエクスプローラー

## 🎯 プロジェクト仕様

### アプリケーション概要
- **タイプ**: VRファイルエクスプローラー
- **プラットフォーム**: Meta Quest 3
- **Unity バージョン**: 2022.3.62f1 LTS
- **開発ツール**: ClaudeCode + Unity MCP + GitHub MCP
- **開発ステータス**: 基本機能完成、拡張機能開発準備完了

## ✅ 実装済み機能

### 1. VRパネルシステム
- **3パネル構成**: 左・中央・右パネルの配置
- **カメラ追従**: 常にユーザーの正面を向くUI
- **柔軟な設計**: パネル数の変更に対応
- **中央パネル**: ファイルエクスプローラー専用

### 2. ファイルエクスプローラー（中央パネル）
- **実際のフォルダ内容表示**: プラットフォーム対応
  - Unity Editor: `Assets/StreamingAssets`を表示
  - Meta Quest実機: `Application.persistentDataPath`を使用（サンプル構造を自動作成）
- **4列グリッドレイアウト**: パーセンテージベースの動的サイズ調整
- **垂直スクロール**: 15個以上のアイテムに対応
- **VRコントローラー対応**: 右スティックでスクロール操作

### 3. ナビゲーション機能
- **フォルダクリック**: 黄色いフォルダアイコンクリックで移動
- **上位フォルダ遷移**: 
  - **Bボタン**（右・左コントローラー）で`cd ..`相当の操作
  - **左上「↑」アイコン**クリックで同様の操作
- **パスバー**: 現在のフォルダパスをリアルタイム表示

### 4. UI/UXの詳細調整
- **ファイル・フォルダ識別**: 
  - フォルダ: 黄色背景 (#FFC837)
  - ファイル: 白色背景
  - 上位フォルダ: 黄色背景 + 大きめの「↑」テキスト
- **テキストレイアウト**: 左寄せ、適切なマージン設定
- **スクロールバー**: 右側配置、細めのハンドル
- **VR最適化**: 適切なフォントサイズと配色

### 5. Android 14ファイルアクセス対応
- **ContentProvider API**: MediaStore経由でメディアファイル検出
- **MediaScanner統合**: ファイル強制スキャン機能
- **MANAGE_EXTERNAL_STORAGE権限**: 全ファイルアクセス対応
- **拡張子判定**: JPG、MP4、PNG等のメディアファイル自動検出

### 6. メディアビューアーシステム（NEW）
- **常設メディアパネル**: 右パネルに統合されたメディア表示エリア
- **パノラマメディア対応**: 
  - ファイル名に"360"、"panorama"、"pano"等のキーワード含む場合
  - Skybox/PanoramicシェーダーによるSkybox背景表示（L1/L2ボタンと同様の体験）
  - パノラマ画像・動画両対応
- **通常メディア表示**: 
  - 一般的な画像・動画を右パネルの常設ビューアに表示
  - 動画再生機能、画像表示機能
  - ファイル名タイトルバー表示

## 🎮 操作方法

### VRコントローラー
- **右スティック上下**: ファイルリストのスクロール
- **Bボタン（右/左）**: 上位フォルダへ移動（`cd ..`）
- **グリップボタン（右/左）**: ファイルリスト強制更新（MediaScanner実行）
- **トリガー**: ファイル・フォルダのクリック

### UI操作
- **黄色フォルダアイコン**: クリックでフォルダ内に移動
- **左上「↑」アイコン**: 上位フォルダに移動（Bボタンと同等）
- **白色ファイルアイコン**: クリックでメディアファイル表示
  - パノラマメディア: Skybox背景として360度表示
  - 通常メディア: 右パネルの常設ビューアに表示

## 📋 ClaudeCode開発ルール

### 🔴 必須遵守事項

```
【重要】ClaudeCodeは以下のルールを必ず守ること：

1. **日本語優先**
   - すべての説明は日本語で行う
   - コードコメントも日本語
   - エラーメッセージの説明も日本語
   - コミットメッセージも日本語

2. **MCP優先原則**
   - Unity MCPで実行可能な操作は必ずMCPを使用
   - GitHub MCPでファイル操作とコミットを実行
   - 手動操作は最終手段

3. **エラーゼロ原則**
   - Unityコンソールにエラーを残さない
   - ビルド可能な状態を常に維持
   - エラー発生時は即座に修正

4. **手動操作の詳細化**
   手動操作が必要な場合：
   - 具体的なメニューパス（例: Edit → Project Settings → XR）
   - 設定値の具体的な数値
   - スクリーンショットの説明
   - 確認すべきポイント

5. **コミット規則**
   - 機能単位で小さくコミット
   - プレフィックス使用:
     - feat: 新機能
     - fix: バグ修正
     - docs: ドキュメント
     - refactor: リファクタリング
```

### 開発開始時の手順

```
1. GitHub MCPでこのREADME.mdの最新版を取得
2. 仕様変更がないか確認
3. Unity Editorの起動状態を確認
4. 前回の作業状態を確認
5. 開発を開始
```

## 🏗️ プロジェクト構造

```
VR-Unity-Template/
├── Assets/
│   ├── Scripts/
│   │   ├── UISetup.cs               # メインのUI制御スクリプト
│   │   ├── MediaViewer.cs           # メディアビューアーシステム
│   │   ├── AndroidFileAccess.cs     # Androidファイルアクセス
│   │   ├── PermissionRequester.cs   # Android権限管理
│   │   └── VRScrollController.cs    # VRスクロール制御
│   ├── StreamingAssets/             # エディタ用サンプルデータ
│   │   ├── Images/
│   │   ├── Videos/
│   │   ├── Documents/
│   │   └── [各種サンプルファイル]
│   └── Samples/
│       └── Img/                     # 開発中のスクリーンショット
├── Packages/
├── ProjectSettings/
└── .claude/
    └── mcp.json                    # MCP設定
```

## 💻 重要なクラス

### UISetup.cs
メインのUI制御クラス（1400+行）。以下の機能を提供：

```csharp
// ファイルエクスプローラー用の状態管理
private string currentFolderPath = "";      // 現在のフォルダパス（相対）
private string baseFolderPath = "";         // ベースフォルダ（絶対）
private TextMeshProUGUI pathBarText;        // パスバー参照
private Transform fileButtonsContainer;     // ファイルボタンコンテナ

// VRコントローラー入力の状態管理
private bool previousBButtonPressed = false;
private bool previousLeftBButtonPressed = false;
private bool previousGripPressed = false;
private bool previousLeftGripPressed = false;
```

**主要メソッド:**
- `CreateTestFileButtons()`: 実際のフォルダ内容を読み込んでUI生成
- `NavigateToFolder(string folderName)`: フォルダ遷移処理
- `NavigateToParentFolder()`: 上位フォルダ遷移（`cd ..`相当）
- `CheckNavigationInput()`: VRコントローラーのBボタン監視
- `CheckRefreshInput()`: グリップボタンでファイルリスト更新
- `RefreshFileList()`: ファイルリストの動的更新

### AndroidFileAccess.cs
Android/Meta Quest向けファイルアクセス機能：

```csharp
// MediaStore API経由でファイル取得
public static List<string> GetMediaStoreFiles(string folderPath)

// MediaScannerでファイル登録
public static void ScanFile(string filePath)
public static void ScanFolder(string folderPath)

// ContentProvider経由でファイル一覧取得
static List<string> GetFilesViaContentProvider(string folderPath)

// サポート拡張子の定義
public static class SupportedExtensions
{
    public static readonly string[] Images = { ".jpg", ".jpeg", ".png", ... };
    public static readonly string[] Videos = { ".mp4", ".mov", ".avi", ... };
}
```

### MediaViewer.cs
統合メディアビューアーシステム（新規追加）：

```csharp
// シングルトンパターンでメディア表示を統一管理
public static MediaViewer Instance { get; private set; }

// メディアファイル判定とルーティング
public void OpenMediaFile(string filePath)
{
    // パノラマコンテンツ判定: ファイル名にキーワード含む場合
    if (IsPanoramaContent(fileName)) {
        // Skyboxとして表示（L1/L2ボタンと同様）
        DisplayPanoramaImage/Video(filePath);
    } else {
        // 右パネルの常設ビューアに表示
        DisplayRegularImage/Video(filePath);
    }
}

// パノラマ判定ロジック
private bool IsPanoramaContent(string fileName)
{
    string[] keywords = {"360", "panorama", "pano", "spherical", "equirectangular"};
    return keywords.Any(keyword => fileName.ToLower().Contains(keyword));
}

// Skybox設定（画像・動画対応）
private void SetPanoramaSkybox(Texture2D/RenderTexture texture)
{
    // Skybox/Panoramicシェーダー使用
    // Latitude Longitude Layout設定
    // RenderSettings.skybox更新
}
```

### PermissionRequester.cs
Android実行時パーミッション要求：

```csharp
// MANAGE_EXTERNAL_STORAGE権限チェック
static bool CheckAllFilesAccess()

// ストレージ権限の状態確認
public static bool HasStoragePermission()
```

### VRScrollController.cs
VRコントローラーによるスクロール制御：

```csharp
[SerializeField] private float scrollSpeed = 2f;
[SerializeField] private float scrollDeadZone = 0.1f;
[SerializeField] private bool useRightController = true;
```

### プラットフォーム対応

```csharp
#if UNITY_EDITOR
    // エディタ: Assets/StreamingAssetsを使用
    baseFolderPath = Path.Combine(Application.dataPath, "StreamingAssets");
#else
    // 実機: persistentDataPathを使用（読み書き可能）
    baseFolderPath = Application.persistentDataPath;
    CreateSampleFolderStructure(baseFolderPath);  // サンプル構造作成
#endif
```

## 🔧 技術仕様

### 必須パッケージ
- XR Interaction Toolkit (3.0.1以上)
- XR Plugin Management (4.4.0以上)
- Oculus XR Plugin (4.1.2以上)
- TextMeshPro (3.0.6以上)

### レイアウト調整可能パラメータ
```csharp
[Header("ファイルエクスプローラーレイアウト設定")]
[SerializeField] private float explorerPaddingPercent = 0.02f;  // パディング: 2%
[SerializeField] private float explorerMarginPercent = 0.02f;   // マージン: 2%
[SerializeField] private int explorerColumnsCount = 4;          // 列数: 4個
```

### スクロール設定
```csharp
[Header("スクロール設定")]
[SerializeField] private float scrollSpeed = 2f;
[SerializeField] private float scrollDeadZone = 0.1f;
```

### Unity設定
```yaml
Build Settings:
  Platform: Android
  Texture Compression: ASTC
  
Player Settings:
  Company Name: YaAkiyama
  Product Name: VR-Unity-Template
  Package Name: com.yaakiyama.vrtemplate
  Minimum API Level: 29 (Android 10)
  Target API Level: Automatic

XR Plugin Management:
  Android:
    - Oculus: ✓
  
Oculus Settings:
  Stereo Rendering Mode: Multiview
  Target Devices: Quest 3
```

### スクリプト設計方針
- **シングルトン**: Manager系クラスで使用
- **イベントシステム**: UnityEventとAction使用
- **null安全**: null条件演算子使用
- **非同期処理**: async/await使用

## 📊 パフォーマンス目標
- **フレームレート**: 72fps維持
- **解像度**: 2064×2208 per eye
- **レイテンシ**: 20ms以下
- **メモリ使用**: 2GB以下

## 🐛 トラブルシューティング

### Android 14でファイルが表示されない問題（解決済み）
Meta Quest 3でJPG/MP4ファイルが表示されない場合の解決方法：

1. **MANAGE_EXTERNAL_STORAGE権限の手動付与が必要**
   - Meta Questのホーム画面で「設定」を開く
   - 「アプリ」→「アプリ管理」を選択
   - 「My Application」（VRアプリ）を選択
   - 「権限」→「ファイルとメディア」を「許可」に変更
   - 「すべてのファイルへのアクセス」も許可する

2. **グリップボタンでファイルリスト強制更新**
   - 右または左コントローラーのグリップボタンを押す
   - MediaScannerでフォルダを再スキャン
   - ファイルリストが更新される

### Unity MCPエラー
```bash
# Unity Editorを再起動
# MCPサーバーを再起動
```

### ビルドエラー
- Android Build Supportを確認
- SDK/NDK/JDKパスを確認
- XR Pluginの設定を確認

### Quest 3で動作しない
- 開発者モードが有効か確認
- USBデバッグが許可されているか確認
- adb devicesでデバイス認識を確認

## 🔍 デバッグ情報

### ログ出力例
```
[UISetup] ベースフォルダパス設定: /storage/emulated/0/Android/data/com.example.vrfileexplorer/files
[UISetup] 検出されたアイテム数: 8 (フォルダ: 5, ファイル: 3)
[UISetup] 上位フォルダアイコンを追加
[UISetup] フォルダ移動: Images
[UISetup] 上位フォルダへ移動: '' (ルートに戻る)
```

## 🚀 ビルドと実行

### Unity Editor
1. プロジェクトを開く
2. `Assets/StreamingAssets`に表示したいファイル・フォルダを配置
3. Play ボタンでエディタ内テスト実行

### Meta Quest 3実機
1. **Build Settings**:
   - Platform: Android
   - Texture Compression: ASTC
2. **XR Plug-in Management**: OpenXR設定
3. **Player Settings**: 
   - Package Name設定
   - Minimum API Level: Android 7.0以上
4. Build & Run または APKファイル作成

## 🛠️ 今後の拡張予定

### 短期的な改善
- [ ] ファイル形式別アイコン表示
- [ ] ファイル詳細情報表示（サイズ、更新日時）
- [ ] 並び替え機能（名前、日時、サイズ）
- [ ] メディアビューアの操作性向上（再生/一時停止、音量調整等）

### 中長期的な機能追加
- [ ] ファイル操作機能（コピー、移動、削除）
- [ ] 検索・フィルタリング機能
- [ ] 外部ストレージアクセス
- [ ] クラウドストレージ連携
- [ ] より多くのメディア形式対応
- [ ] プレイリスト機能

## 📝 開発ログ

### 2025-09-01
- **メディアビューアーシステム実装完了** 🆕
  - 統合メディア表示システム（MediaViewer.cs）実装
  - パノラマメディア自動判定機能（ファイル名キーワード検出）
  - パノラマ画像・動画のSkybox背景表示（L1/L2ボタンと同様）
  - 通常メディアの右パネル常設ビューア表示
  - Skybox/PanoramicシェーダーによるVR360度表示対応
- **常設メディアパネルシステム実装**
  - 右パネルに統合されたメディア表示エリア
  - ファイルクリック時の自動メディア判定・表示
  - パノラマ・通常メディアの適切なルーティング
- **Android 14ファイルアクセス問題を解決**
  - ContentProvider/MediaStore APIでメディアファイル検出を実装
  - MANAGE_EXTERNAL_STORAGE権限の手動付与でJPG/MP4ファイル表示成功
  - MediaScannerによるファイル強制スキャン機能追加
  - VRコントローラーのグリップボタンでファイルリスト更新機能実装

### 2025-08-30
- **VRファイルエクスプローラー基本機能完成**
- UIパネルシステム実装（3パネル構成、カメラ追従）
- ファイル・フォルダ表示システム実装（プラットフォーム対応）
- 4列グリッドレイアウト + 垂直スクロール
- VRコントローラー操作対応（右スティックスクロール、Bボタンナビゲーション）
- 上位フォルダ遷移機能（Bボタン + 「↑」アイコン）
- パスバー + 動的ファイルリスト更新
- Meta Quest 3実機対応完了

### 2025-08-21
- プロジェクト初期設定
- README.md作成（開発ルール・仕様記載）

## 🚀 クイックスタート

### 開発者向け
```bash
# リポジトリをクローン
git clone https://github.com/YaAkiyama/VR-Unity-Template.git
cd VR-Unity-Template

# Unity Hubでプロジェクトを開く
# Unity 2022.3.62f1 LTS

# ClaudeCodeを起動
npx @anthropic-ai/claude-code
```

### ClaudeCodeへの継承指示
```
GitHub MCPでREADME.mdを取得し、VRファイルエクスプローラーの現在の実装状況を確認してください。

現在の状態：
✅ 基本機能完成済み + メディアビューアー実装完了
- 3パネルVR UI（左・中央・右パネル）
  - 左パネル: パノラマメディア用（L1/L2ボタン）
  - 中央パネル: ファイルエクスプローラー
  - 右パネル: 常設メディアビューアー
- 実際のフォルダ内容表示（エディタ・実機対応）
- 4列グリッドレイアウト + 垂直スクロール
- VRコントローラー操作（スクロール・ナビゲーション）
- 上位フォルダ遷移（Bボタン + 「↑」アイコン）
- Android 14ファイルアクセス対応（MediaStore/ContentProvider）
- グリップボタンでファイルリスト強制更新

🆕 NEW: メディアビューアーシステム
- ファイルクリックで自動メディア判定・表示
- パノラマメディア: Skybox背景表示（ファイル名キーワード検出）
- 通常メディア: 右パネル常設ビューアで表示
- 画像・動画両対応、VideoPlayer統合

重要な権限設定：
⚠️ Meta Quest 3でメディアファイル表示にはMANAGE_EXTERNAL_STORAGE権限の手動付与が必要
詳細は「トラブルシューティング」セクション参照

主要ファイル：
- Assets/Scripts/UISetup.cs (1400+行) - UI制御システム
- Assets/Scripts/MediaViewer.cs (680+行) - メディア表示システム 🆕
- Assets/Scripts/AndroidFileAccess.cs - ファイルアクセス処理
- Assets/Scripts/PermissionRequester.cs - 権限管理
- Assets/Scripts/VRScrollController.cs - VRスクロール

Unity MCPとGitHub MCPを使用して、さらなる拡張機能の開発や改善を継続してください。
```

## 📄 ライセンス

MIT License

## 👤 作成者

YaAkiyama

---

**最終更新**: 2025-09-01 (メディアビューアーシステム実装完了)