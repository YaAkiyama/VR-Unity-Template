using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR用ファイルエクスプローラー
/// 中央パネルにWindowsエクスプローラー風のUIを実装
/// </summary>
public class FileExplorerManager : MonoBehaviour
{
    [Header("ファイルエクスプローラー設定")]
    [SerializeField] private string[] supportedExtensions = {".jpg", ".png", ".mp4", ".mov"};
    
    [Header("UI設定")]
    [SerializeField] private int columnsPerRow = 5;
    [SerializeField] private Vector2 buttonSize = new Vector2(80f, 60f);
    [SerializeField] private Vector2 buttonSpacing = new Vector2(10f, 10f);
    
    [Header("色設定")]
    [SerializeField] private Color folderColor = new Color(1f, 0.8f, 0.2f, 1f); // 黄色
    [SerializeField] private Color fileColor = new Color(0.9f, 0.9f, 0.9f, 1f); // 白
    [SerializeField] private Color backButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 赤色
    [SerializeField] private Color textColor = Color.black;
    
    // UI要素
    private Transform centerPanel;
    private ScrollRect scrollRect;
    private Transform content;
    private GameObject buttonTemplate;
    private GameObject backButtonObject;
    
    // ナビゲーション
    private string currentPath;
    private string rootPath;
    private List<string> pathHistory = new List<string>();
    private int currentHistoryIndex = -1;
    
    void Start()
    {
        InitializeFileExplorer();
    }
    
    void InitializeFileExplorer()
    {
        // 初期パス設定
        currentPath = Application.streamingAssetsPath;
        
        // StreamingAssetsパスが存在しない場合は作成
        if (string.IsNullOrEmpty(currentPath))
        {
            currentPath = Application.dataPath + "/StreamingAssets";
        }
        
        if (!Directory.Exists(currentPath))
        {
            try
            {
                Directory.CreateDirectory(currentPath);
                Debug.Log($"[FileExplorerManager] StreamingAssetsフォルダを作成: {currentPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileExplorerManager] フォルダ作成エラー: {e.Message}");
                currentPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
        }
        
        // ルートパスを設定（戻るボタンの制御に使用）
        rootPath = currentPath;
        
        Debug.Log($"[FileExplorerManager] 初期化完了 - ルートパス: {rootPath}");
    }
    
    /// <summary>
    /// 中央パネルにファイルエクスプローラーUIを設定
    /// </summary>
    public void SetupFileExplorerUI(Transform panelTransform)
    {
        centerPanel = panelTransform;
        
        // 初期化がまだなら実行
        if (string.IsNullOrEmpty(currentPath))
        {
            InitializeFileExplorer();
        }
        
        CreateFileExplorerUI();
        NavigateToPath(currentPath);
    }
    
    void CreateFileExplorerUI()
    {
        if (centerPanel == null) return;
        
        // 既存のボタンコンテナを削除
        Transform existingContainer = centerPanel.Find("ButtonContainer");
        if (existingContainer != null)
        {
            DestroyImmediate(existingContainer.gameObject);
        }
        
        // ファイルエクスプローラーコンテナ作成
        GameObject explorerContainer = new GameObject("FileExplorerContainer");
        explorerContainer.transform.SetParent(centerPanel);
        
        RectTransform containerRect = explorerContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.anchoredPosition3D = Vector3.zero; // Z軸を明示的に設定
        containerRect.localPosition = Vector3.zero;
        containerRect.localRotation = Quaternion.identity;
        containerRect.localScale = Vector3.one;
        
        // パスバー作成
        CreatePathBar(explorerContainer.transform);
        
        // 戻るボタン作成
        CreateBackButton(explorerContainer.transform);
        
        // スクロールビュー作成
        CreateScrollView(explorerContainer.transform);
        
        // ボタンテンプレート作成
        CreateButtonTemplate();
        
        Debug.Log("[FileExplorerManager] ファイルエクスプローラーUI作成完了");
    }
    
    void CreatePathBar(Transform parent)
    {
        GameObject pathBar = new GameObject("PathBar");
        pathBar.transform.SetParent(parent);
        
        RectTransform pathRect = pathBar.AddComponent<RectTransform>();
        pathRect.anchorMin = new Vector2(0f, 0.9f);
        pathRect.anchorMax = new Vector2(1f, 1f);
        pathRect.sizeDelta = Vector2.zero;
        pathRect.anchoredPosition = Vector2.zero;
        pathRect.anchoredPosition3D = Vector3.zero;
        pathRect.localPosition = Vector3.zero;
        pathRect.localRotation = Quaternion.identity;
        pathRect.localScale = Vector3.one;
        
        // 背景
        Image pathBg = pathBar.AddComponent<Image>();
        pathBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // パステキスト
        GameObject pathTextGO = new GameObject("PathText");
        pathTextGO.transform.SetParent(pathBar.transform);
        
        RectTransform pathTextRect = pathTextGO.AddComponent<RectTransform>();
        pathTextRect.anchorMin = Vector2.zero;
        pathTextRect.anchorMax = Vector2.one;
        pathTextRect.sizeDelta = Vector2.zero;
        pathTextRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI pathText = pathTextGO.AddComponent<TextMeshProUGUI>();
        pathText.text = "Network";
        pathText.fontSize = 6f; // 実機に適したフォントサイズ
        pathText.color = Color.white;
        pathText.alignment = TextAlignmentOptions.MidlineLeft;
        pathText.margin = new Vector4(5f, 0f, 5f, 0f);
        pathText.enableWordWrapping = true;
        pathText.overflowMode = TextOverflowModes.Truncate;
    }
    
    void CreateBackButton(Transform parent)
    {
        // 戻るボタンコンテナ
        backButtonObject = new GameObject("BackButton");
        backButtonObject.transform.SetParent(parent);
        
        RectTransform backRect = backButtonObject.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.02f, 0.87f);
        backRect.anchorMax = new Vector2(0.15f, 1f);
        backRect.sizeDelta = Vector2.zero;
        backRect.anchoredPosition = Vector2.zero;
        backRect.anchoredPosition3D = Vector3.zero;
        backRect.localPosition = Vector3.zero;
        backRect.localRotation = Quaternion.identity;
        backRect.localScale = Vector3.one;
        
        // ボタンコンポーネント
        Button backButton = backButtonObject.AddComponent<Button>();
        Image backImage = backButtonObject.AddComponent<Image>();
        backImage.color = backButtonColor;
        
        // VRインタラクション用コライダー
        BoxCollider backCollider = backButtonObject.AddComponent<BoxCollider>();
        backCollider.size = new Vector3(40f, 25f, 1f);
        backCollider.isTrigger = false;
        
        // 戻るボタンのテキスト
        GameObject backTextGO = new GameObject("Text");
        backTextGO.transform.SetParent(backButtonObject.transform);
        
        RectTransform backTextRect = backTextGO.AddComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.sizeDelta = Vector2.zero;
        backTextRect.anchoredPosition = Vector2.zero;
        backTextRect.anchoredPosition3D = Vector3.zero;
        backTextRect.localScale = Vector3.one;
        backTextRect.localRotation = Quaternion.identity;
        
        TextMeshProUGUI backText = backTextGO.AddComponent<TextMeshProUGUI>();
        backText.text = "← Back";
        backText.fontSize = 5f;
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;
        backText.fontStyle = FontStyles.Bold;
        
        // クリックイベント
        backButton.onClick.AddListener(() => NavigateUp());
        
        // 初期状態では非表示（ルートフォルダの場合）
        UpdateBackButtonVisibility();
        
        Debug.Log("[FileExplorerManager] 戻るボタンを作成しました");
    }
    
    void CreateScrollView(Transform parent)
    {
        // スクロールビュー作成
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(parent);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 0.9f);
        scrollRect.sizeDelta = Vector2.zero;
        scrollRect.anchoredPosition = Vector2.zero;
        scrollRect.localPosition = Vector3.zero;
        scrollRect.localRotation = Quaternion.identity;
        scrollRect.localScale = Vector3.one;
        
        // ScrollRectコンポーネント
        this.scrollRect = scrollView.AddComponent<ScrollRect>();
        this.scrollRect.horizontal = false;
        this.scrollRect.vertical = true;
        this.scrollRect.movementType = ScrollRect.MovementType.Clamped;
        this.scrollRect.scrollSensitivity = 20f;
        
        // ビューポート
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // より見やすい背景色
        
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        
        this.scrollRect.viewport = viewportRect;
        
        // コンテント
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewport.transform);
        
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 200f);
        contentRect.anchoredPosition = Vector2.zero;
        
        this.content = contentGO.transform;
        this.scrollRect.content = contentRect;
        
        // GridLayoutGroup
        GridLayoutGroup gridLayout = contentGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = buttonSize;
        gridLayout.spacing = buttonSpacing;
        gridLayout.padding = new RectOffset(20, 20, 20, 20);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnsPerRow;
    }
    
    void CreateButtonTemplate()
    {
        buttonTemplate = new GameObject("ButtonTemplate");
        buttonTemplate.SetActive(false);
        
        RectTransform buttonRect = buttonTemplate.AddComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;
        
        Button button = buttonTemplate.AddComponent<Button>();
        Image buttonImage = buttonTemplate.AddComponent<Image>();
        buttonImage.color = Color.white; // デフォルト色
        
        // VRインタラクション用のコライダーを追加
        BoxCollider buttonCollider = buttonTemplate.AddComponent<BoxCollider>();
        buttonCollider.size = new Vector3(buttonSize.x, buttonSize.y, 1f);
        buttonCollider.isTrigger = false;
        
        // ボタンテキスト
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonTemplate.transform);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;
        
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 4f; // フォントサイズを小さくして改行に対応
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
    }
    
    /// <summary>
    /// 指定パスに移動してファイル・フォルダを表示
    /// </summary>
    public void NavigateToPath(string path)
    {
        Debug.Log($"[FileExplorerManager] NavigateToPath呼び出し - 引数パス: '{path}'");
        
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[FileExplorerManager] パスが空または無効です");
            return;
        }
        
        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"[FileExplorerManager] パスが存在しません: {path}");
            // エラーでもUI作成は続行
        }
        
        currentPath = path;
        UpdatePathHistory(path);
        RefreshFileList();
        UpdatePathBar();
    }
    
    void RefreshFileList()
    {
        if (content == null) return;
        
        // 既存のボタンを削除
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(content.GetChild(i).gameObject);
        }
        
        List<string> items = new List<string>();
        
        try
        {
            // フォルダを追加
            string[] directories = Directory.GetDirectories(currentPath);
            foreach (string dir in directories)
            {
                items.Add(dir);
            }
            
            // サポートされているファイルを追加
            string[] files = Directory.GetFiles(currentPath);
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (System.Array.Exists(supportedExtensions, ext => ext == extension))
                {
                    items.Add(file);
                }
            }
            
            // ボタンを作成
            foreach (string item in items)
            {
                CreateFileButton(item);
            }
            
            // コンテンツサイズを調整
            UpdateContentSize(items.Count);
            
            Debug.Log($"[FileExplorerManager] {items.Count}個のアイテムを表示: {currentPath}");
            
            // デバッグ: 作成されたアイテムのリスト
            for (int i = 0; i < items.Count && i < 5; i++)
            {
                Debug.Log($"[FileExplorerManager] アイテム{i}: {Path.GetFileName(items[i])}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FileExplorerManager] ファイル読み込みエラー: {e.Message}");
        }
    }
    
    void CreateFileButton(string itemPath)
    {
        GameObject button = Instantiate(buttonTemplate, content);
        button.SetActive(true);
        button.name = Path.GetFileName(itemPath);
        
        bool isDirectory = Directory.Exists(itemPath);
        
        // ボタンの色設定
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = isDirectory ? folderColor : fileColor;
        
        // テキスト設定（改善された表示機能）
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        string fileName = Path.GetFileName(itemPath);
        text.text = FormatFileName(fileName);
        
        // クリックイベント
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.onClick.AddListener(() => OnItemClicked(itemPath, isDirectory));
        
        Debug.Log($"[FileExplorerManager] ボタン作成: {fileName} ({(isDirectory ? "フォルダ" : "ファイル")})");
    }
    
    void OnItemClicked(string itemPath, bool isDirectory)
    {
        if (isDirectory)
        {
            Debug.Log($"[FileExplorerManager] フォルダクリック: {itemPath}");
            NavigateToPath(itemPath);
            UpdateBackButtonVisibility();
        }
        else
        {
            Debug.Log($"[FileExplorerManager] ファイルクリック: {itemPath}");
            OnFileSelected(itemPath);
        }
    }
    
    void OnFileSelected(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        
        // PanoramaSkyboxManagerにファイルを送信
        PanoramaSkyboxManager panoramaManager = FindObjectOfType<PanoramaSkyboxManager>();
        if (panoramaManager != null)
        {
            if (extension == ".jpg" || extension == ".png")
            {
                // 画像ファイルの処理
                Debug.Log($"[FileExplorerManager] 画像ファイル選択: {filePath}");
            }
            else if (extension == ".mp4" || extension == ".mov")
            {
                // 動画ファイルの処理
                Debug.Log($"[FileExplorerManager] 動画ファイル選択: {filePath}");
            }
        }
    }
    
    void UpdateContentSize(int itemCount)
    {
        if (content == null) return;
        
        int rows = Mathf.CeilToInt((float)itemCount / columnsPerRow);
        float contentHeight = rows * (buttonSize.y + buttonSpacing.y) + 40f; // パディング追加
        
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(0f, Mathf.Max(contentHeight, 200f));
    }
    
    void UpdatePathBar()
    {
        Transform pathBar = centerPanel?.Find("FileExplorerContainer/PathBar/PathText");
        if (pathBar != null)
        {
            TextMeshProUGUI pathText = pathBar.GetComponent<TextMeshProUGUI>();
            if (pathText != null)
            {
                pathText.text = "Network"; // サンプル画像に合わせて固定表示
            }
        }
    }
    
    void UpdatePathHistory(string path)
    {
        // 履歴に追加
        if (currentHistoryIndex == -1 || pathHistory[currentHistoryIndex] != path)
        {
            if (currentHistoryIndex < pathHistory.Count - 1)
            {
                pathHistory.RemoveRange(currentHistoryIndex + 1, pathHistory.Count - currentHistoryIndex - 1);
            }
            pathHistory.Add(path);
            currentHistoryIndex = pathHistory.Count - 1;
        }
    }
    
    /// <summary>
    /// 戻るボタン機能
    /// </summary>
    public void NavigateBack()
    {
        if (currentHistoryIndex > 0)
        {
            currentHistoryIndex--;
            string previousPath = pathHistory[currentHistoryIndex];
            currentPath = previousPath;
            RefreshFileList();
            UpdatePathBar();
        }
    }
    
    /// <summary>
    /// 進むボタン機能
    /// </summary>
    public void NavigateForward()
    {
        if (currentHistoryIndex < pathHistory.Count - 1)
        {
            currentHistoryIndex++;
            string nextPath = pathHistory[currentHistoryIndex];
            currentPath = nextPath;
            RefreshFileList();
            UpdatePathBar();
        }
    }
    
    void Update()
    {
        HandleControllerInput();
    }
    
    void HandleControllerInput()
    {
        // コントローラーの上下スティックでスクロール
        if (scrollRect != null)
        {
            Vector2 scrollInput = Vector2.zero;
            
            // XRコントローラーからの入力を取得
            try 
            {
                // 左コントローラーのスティック入力
                var leftHand = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>("LeftHand");
                if (leftHand != null)
                {
                    var leftStick = leftHand.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("primary2DAxis");
                    if (leftStick != null)
                    {
                        Vector2 leftValue = leftStick.ReadValue();
                        scrollInput.y += leftValue.y;
                    }
                }
                
                // 右コントローラーのスティック入力
                var rightHand = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>("RightHand");
                if (rightHand != null)
                {
                    var rightStick = rightHand.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("primary2DAxis");
                    if (rightStick != null)
                    {
                        Vector2 rightValue = rightStick.ReadValue();
                        scrollInput.y += rightValue.y;
                    }
                }
            }
            catch (System.Exception)
            {
                // XR Input Systemが利用できない場合のフォールバック
            }
            
            // キーボード入力（デバッグ用）
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)
                    scrollInput.y += 1f;
                else if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                    scrollInput.y -= 1f;
            }
            
            // スクロール実行
            if (Mathf.Abs(scrollInput.y) > 0.1f)
            {
                float scrollSpeed = 0.03f;
                scrollRect.verticalNormalizedPosition += scrollInput.y * scrollSpeed;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }
        }
    }
    
    void OnDestroy()
    {
        // クリーンアップ
        if (buttonTemplate != null)
        {
            DestroyImmediate(buttonTemplate);
        }
    }
    
    /// <summary>
    /// ファイル名を表示用にフォーマット（改行対応、省略表示）
    /// </summary>
    string FormatFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "";
        
        // 拡張子を分離
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        
        // 1行の最大文字数（ボタンサイズに応じて調整）
        int maxCharsPerLine = 8;
        int maxLines = 2;
        int totalMaxChars = maxCharsPerLine * maxLines;
        
        // 短い場合はそのまま返す
        if (fileName.Length <= maxCharsPerLine)
        {
            return fileName;
        }
        
        // 2行以内に収まる場合は改行を入れる
        if (fileName.Length <= totalMaxChars)
        {
            // 適当な位置で改行（単語の境界を考慮）
            if (nameWithoutExtension.Length > maxCharsPerLine)
            {
                int breakPoint = FindBreakPoint(nameWithoutExtension, maxCharsPerLine);
                string firstLine = nameWithoutExtension.Substring(0, breakPoint);
                string secondLine = nameWithoutExtension.Substring(breakPoint) + extension;
                
                // 2行目が長すぎる場合は省略
                if (secondLine.Length > maxCharsPerLine)
                {
                    secondLine = secondLine.Substring(0, maxCharsPerLine - 2) + "..";
                }
                
                return firstLine + "\n" + secondLine;
            }
        }
        
        // 長すぎる場合は省略表示
        return fileName.Substring(0, totalMaxChars - 3) + "...";
    }
    
    /// <summary>
    /// 改行位置を見つける（単語境界を考慮）
    /// </summary>
    int FindBreakPoint(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text.Length;
        
        // 単語境界（スペース、アンダースコア、ハイフン）を探す
        char[] delimiters = {' ', '_', '-', '.'};
        
        for (int i = maxLength - 1; i >= maxLength / 2; i--)
        {
            if (System.Array.Exists(delimiters, c => c == text[i]))
            {
                return i + 1; // 区切り文字の後で改行
            }
        }
        
        // 区切り文字が見つからない場合は強制的に切断
        return maxLength;
    }
    
    /// <summary>
    /// 一つ上のフォルダに移動
    /// </summary>
    void NavigateUp()
    {
        if (string.IsNullOrEmpty(currentPath) || currentPath == rootPath)
        {
            Debug.Log("[FileExplorerManager] 既にルートフォルダです");
            return;
        }
        
        try
        {
            DirectoryInfo parentDir = Directory.GetParent(currentPath);
            if (parentDir != null)
            {
                string parentPath = parentDir.FullName;
                Debug.Log($"[FileExplorerManager] 上位フォルダに移動: {parentPath}");
                NavigateToPath(parentPath);
                UpdateBackButtonVisibility();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FileExplorerManager] フォルダ移動エラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// 戻るボタンの表示/非表示を更新
    /// </summary>
    void UpdateBackButtonVisibility()
    {
        if (backButtonObject == null) return;
        
        // ルートフォルダまたはそれより上位の場合は非表示
        bool shouldShow = !string.IsNullOrEmpty(currentPath) && 
                         !string.Equals(currentPath, rootPath, System.StringComparison.OrdinalIgnoreCase) &&
                         !IsParentOrEqual(rootPath, currentPath);
        
        backButtonObject.SetActive(shouldShow);
        
        Debug.Log($"[FileExplorerManager] 戻るボタン: {(shouldShow ? "表示" : "非表示")} - Current: {currentPath}, Root: {rootPath}");
    }
    
    /// <summary>
    /// パスAがパスBの親またはパスBと同じかチェック
    /// </summary>
    bool IsParentOrEqual(string pathA, string pathB)
    {
        if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB)) return false;
        
        try
        {
            string normalizedA = Path.GetFullPath(pathA).TrimEnd(Path.DirectorySeparatorChar);
            string normalizedB = Path.GetFullPath(pathB).TrimEnd(Path.DirectorySeparatorChar);
            
            return normalizedB.StartsWith(normalizedA, System.StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}