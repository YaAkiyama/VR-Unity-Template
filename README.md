# VR-Unity-Template

MetaQuest3向けVRパノラマビューアーテンプレート

## 🎯 プロジェクト仕様

### アプリケーション概要
- **タイプ**: VRパノラマビューアー
- **プラットフォーム**: Meta Quest 3
- **Unity バージョン**: 2022.3.62f1 LTS
- **開発ツール**: ClaudeCode + Unity MCP + GitHub MCP

### 機能仕様

#### 1. プレイヤーシステム
- **位置**: 固定位置（移動不可）
- **回転**: 360度自由回転（頭の動きに追従）
- **高さ**: 床から1.6m（調整可能）

#### 2. インタラクションシステム
- **コントローラー**: Quest 3両手コントローラー使用
- **レーザーポインター**: 
  - 両手から直線状に表示
  - 色: 通常時は白、ホバー時は青
  - 長さ: 10m
- **UIインタラクション**:
  - パネルとの交差点にドット表示
  - ドットサイズ: 直径5cm
  - トリガーボタンでクリック操作

#### 3. UIパネルシステム
- **配置**: ワールド空間に固定
- **デフォルト位置**: プレイヤー前方3m、高さ1.5m
- **サイズ**: 3m × 2m
- **構成**:
  ```
  ┌─────────────────────────────────────┐
  │  [メニュー]    [コンテンツ]    [情報]  │
  │  ┌──────┐    ┌──────────┐    ┌────┐ │
  │  │リスト │    │グリッド   │    │詳細│ │
  │  └──────┘    └──────────┘    └────┘ │
  └─────────────────────────────────────┘
  ```

#### 4. メディア再生機能
- **パノラマ画像**:
  - 対応形式: JPG, PNG
  - 解像度: 最大8K
  - 投影方式: Equirectangular
- **パノラマ動画**:
  - 対応形式: MP4
  - 解像度: 最大4K
  - フレームレート: 30/60fps
- **切り替え**: フェードイン/アウト（0.5秒）

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
│   │   ├── Player/           # プレイヤー制御
│   │   │   └── VRPlayerController.cs
│   │   ├── UI/              # UIシステム
│   │   │   ├── LaserPointerController.cs
│   │   │   ├── UIPanel.cs
│   │   │   └── UIInteraction.cs
│   │   ├── Media/           # メディア再生
│   │   │   ├── PanoramaManager.cs
│   │   │   └── MediaLoader.cs
│   │   └── Interaction/     # インタラクション
│   │       └── InteractionManager.cs
│   ├── Prefabs/
│   │   ├── Player/
│   │   │   └── XROrigin_Fixed.prefab
│   │   ├── UI/
│   │   │   ├── MediaPanel.prefab
│   │   │   └── LaserPointer.prefab
│   │   └── Media/
│   │       └── PanoramaSphere.prefab
│   ├── Materials/
│   │   ├── Skybox/
│   │   └── UI/
│   ├── Textures/
│   └── Media/
│       ├── Images/          # パノラマ画像
│       └── Videos/          # パノラマ動画
├── Packages/
├── ProjectSettings/
└── .claude/
    └── mcp.json            # MCP設定
```

## 🔧 技術仕様

### 必須パッケージ
- XR Interaction Toolkit (3.0.1以上)
- XR Plugin Management (4.4.0以上)
- Oculus XR Plugin (4.1.2以上)
- TextMeshPro (3.0.6以上)

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

## 📝 開発ログ

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

### ClaudeCodeへの初回指示
```
GitHub MCPでREADME.mdを取得し、記載されている開発ルールと仕様に従って開発を開始してください。

本日の作業：
1. XR Originの設置（プレイヤー固定）
2. レーザーポインターの実装
3. 基本UIパネルの作成

Unity MCPとGitHub MCPを使用してください。
```

## 📄 ライセンス

MIT License

## 👤 作成者

YaAkiyama

---

**最終更新**: 2025-08-21