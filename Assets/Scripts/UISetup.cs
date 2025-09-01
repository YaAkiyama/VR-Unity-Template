using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 柔軟なUIパネルシステム
/// パネル数の増減に対応し、常にカメラを向く
/// </summary>
public class UISetup : MonoBehaviour
{
    [Header("パネルシステム設定")]
    [SerializeField] private int panelCount = 3; // パネルの数
    [SerializeField] private float panelSpacing = 0f; // 3パネル時は固定位置を使用
    [SerializeField] private bool alwaysFaceCamera = true; // 常にカメラを向く
    [SerializeField] private float distanceFromCamera = 4.5f; // カメラからの距離を増加
    
    [Header("Canvas設定")]
    [SerializeField] private Vector2 canvasSize = new Vector2(3.5f, 2.5f);
    [SerializeField] private float canvasScale = 0.01f;
    
    [Header("ボタン設定")]
    [SerializeField] private int buttonRows = 2;
    [SerializeField] private int buttonColumns = 3;
    [SerializeField] private float buttonSpacing = 0.1f;
    
    [Header("ファイルエクスプローラーレイアウト設定")]
    [SerializeField] private float explorerPaddingPercent = 0.02f;  // パディング: 2%
    [SerializeField] private float explorerMarginPercent = 0.02f;   // マージン: 2%
    [SerializeField] private int explorerColumnsCount = 4;          // 横に並べる数: 4個
    
    // 柔軟なパネル管理
    private List<Canvas> panelCanvases = new List<Canvas>();
    private List<RectTransform> panelRects = new List<RectTransform>();
    private List<Transform> panelTransforms = new List<Transform>();
    private Camera mainCamera;
    private PanoramaSkyboxManager panoramaManager;
    private FileExplorerManager fileExplorerManager;
    
    // ファイルエクスプローラー用の状態管理
    private string currentFolderPath = "";  // 現在表示中のフォルダパス（相対パス）
    private string baseFolderPath = "";  // ベースフォルダの絶対パス
    private TextMeshProUGUI pathBarText;  // パスバーのテキスト参照
    private Transform fileButtonsContainer;  // ファイルボタンのコンテナ参照
    
    // 外部ストレージアクセス用の状態管理
    private string[] availablePaths;  // 利用可能なストレージパス一覧
    private int currentPathIndex = 0;  // 現在選択中のパス
    
    // VRコントローラー入力の状態管理
    private bool previousBButtonPressed = false;  // 右コントローラーBボタンの前回の状態
    private bool previousLeftBButtonPressed = false;  // 左コントローラーBボタンの前回の状態
    private bool previousAButtonPressed = false;  // 右コントローラーAボタンの前回の状態（ストレージ切替用）
    
    void Start()
    {
        // パーミッション要求コンポーネントを追加（Android実機用）
        if (!gameObject.GetComponent<PermissionRequester>())
        {
            gameObject.AddComponent<PermissionRequester>();
            Debug.Log("[UISetup] PermissionRequesterコンポーネントを追加");
        }
        
        // テストファイル作成コンポーネントを追加（Android実機用）
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!gameObject.GetComponent<TestFileCreator>())
        {
            gameObject.AddComponent<TestFileCreator>();
            Debug.Log("[UISetup] TestFileCreatorコンポーネントを追加");
        }
        #endif
        
        // カメラ参照を確実に取得
        StartCoroutine(InitializeAfterFrame());
    }
    
    System.Collections.IEnumerator InitializeAfterFrame()
    {
        // 1フレーム待ってからカメラを検索（他のオブジェクトが初期化される時間を確保）
        yield return null;
        
        // 権限取得後にパス探索を実行（Android実機のみ）
        #if UNITY_ANDROID && !UNITY_EDITOR
        yield return new WaitForSeconds(2f);  // 権限処理を待つ
        Debug.Log("[UISetup] === パス探索開始 ===");
        SimpleFileAccess.DiscoverAvailablePaths();
        Debug.Log("[UISetup] === パス探索完了 ===");
        #endif
        
        // コンポーネント参照を取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("[UISetup] カメラが見つかりません。パネルの向き調整は無効になります。");
        }
        else
        {
            Debug.Log($"[UISetup] カメラを見つけました: {mainCamera.name} at {mainCamera.transform.position}");
        }
        
        panoramaManager = FindObjectOfType<PanoramaSkyboxManager>();
        if (panoramaManager == null)
        {
            Debug.LogWarning("[UISetup] PanoramaSkyboxManagerが見つかりません。パノラマ機能は無効です。");
        }
        
        // FileExplorerManagerを取得または作成
        fileExplorerManager = FindObjectOfType<FileExplorerManager>();
        if (fileExplorerManager == null)
        {
            GameObject fileExplorerGO = new GameObject("FileExplorerManager");
            fileExplorerManager = fileExplorerGO.AddComponent<FileExplorerManager>();
            Debug.Log("[UISetup] FileExplorerManagerを作成しました");
        }
        
        CreateFlexiblePanels(); // 柔軟なパネルシステムを作成
        RemoveOldColliders(); // 既存の古いコライダーを削除
    }
    
    void Update()
    {
        // パネルをカメラに向ける処理
        if (alwaysFaceCamera && mainCamera != null)
        {
            FacePanelsToCamera();
        }
        
        // Bボタンで上位フォルダへ遷移
        CheckNavigationInput();
        
        // Xボタンでファイルリスト強制更新（Android実機用）
        CheckRefreshInput();
    }
    
    void ForceAddCollider()
    {
        try
        {
            // 別のGameObjectにColliderを作成（Canvasのスケールの影響を避けるため）
            GameObject colliderObject = GameObject.Find("UIPanel_Collider");
            if (colliderObject == null)
            {
                colliderObject = new GameObject("UIPanel_Collider");
                colliderObject.transform.SetParent(transform.parent); // UIPanel と同じ親に設置
                Debug.Log("[UISetup] UIPanel_Collider GameObjectを作成しました");
            }
            
            // 位置をUIPanel と同じに設定
            colliderObject.transform.position = transform.position;
            colliderObject.transform.rotation = transform.rotation;
            colliderObject.transform.localScale = Vector3.one; // スケールは1のまま
            
            BoxCollider collider = colliderObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = colliderObject.AddComponent<BoxCollider>();
                Debug.Log("[UISetup] BoxColliderを追加しました");
            }
            
            // Colliderサイズを設定（スケールの影響なし）
            Vector3 colliderSize = new Vector3(canvasSize.x, canvasSize.y, 0.1f);
            collider.size = colliderSize;
            collider.isTrigger = false;
            collider.center = Vector3.zero;
            
            Debug.Log($"[UISetup] Canvas Scale: {canvasScale}, Canvas Size: {canvasSize}");
            Debug.Log($"[UISetup] BoxCollider設定完了 - Size: {collider.size}, Position: {colliderObject.transform.position}");
            Debug.Log($"[UISetup] Collider Bounds: {collider.bounds}");
            
            // レイヤーをDefaultに設定
            colliderObject.layer = 0;
            
            Debug.Log($"[UISetup] ColliderObject Layer: {colliderObject.layer}, Tag: {colliderObject.tag}");
            Debug.Log($"[UISetup] ColliderObject Name: {colliderObject.name}, Active: {colliderObject.activeInHierarchy}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISetup] ForceAddColliderでエラー: {e.Message}");
        }
    }
    
    void CreateFlexiblePanels()
    {
        // 既存のパネルをすべて削除
        GameObject[] existingPanels = new GameObject[] {
            GameObject.Find("LeftPanel"),
            GameObject.Find("CenterPanel"),
            GameObject.Find("RightPanel")
        };
        
        foreach (GameObject panel in existingPanels)
        {
            if (panel != null)
            {
                Debug.Log($"[UISetup] 既存パネル {panel.name} を削除します");
                DestroyImmediate(panel);
            }
        }
        
        Debug.Log($"[UISetup] {panelCount}個のパネルを作成します");
        
        // カメラ参照がない場合は再取得を試みる
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            Debug.Log($"[UISetup] カメラ参照: {(mainCamera != null ? mainCamera.name : "null")}");
        }
        
        // パネルの初期位置を計算
        for (int i = 0; i < panelCount; i++)
        {
            string panelName = GetPanelName(i);
            Vector3 panelPosition = CalculatePanelPosition(i);
            Debug.Log($"[UISetup] パネル{i} ({panelName}) の計算位置: {panelPosition}");
            CreateSinglePanel(i, panelName, panelPosition);
        }
    }
    
    string GetPanelName(int index)
    {
        // パネル名を柔軟に生成
        if (panelCount <= 3)
        {
            string[] names = {"LeftPanel", "CenterPanel", "RightPanel"};
            return index < names.Length ? names[index] : $"Panel{index + 1}";
        }
        else
        {
            return $"Panel{index + 1}";
        }
    }
    
    Vector3 CalculatePanelPosition(int index)
    {
        if (panelCount == 1)
        {
            return Vector3.zero; // 単一パネルは中央
        }
        
        // 3パネルの場合の特別な配置（カメラから等距離）
        if (panelCount == 3)
        {
            switch (index)
            {
                case 0: // LeftPanel
                    return new Vector3(-2.8f, 0, 1.3f);   // X:-2.8, Z:1.3
                case 1: // CenterPanel  
                    return new Vector3(0, 0, 2.8f);       // X:0, Z:2.8 前方中央
                case 2: // RightPanel
                    return new Vector3(2.8f, 0, 1.3f);    // X:2.8, Z:1.3
            }
        }
        
        // その他のパネル数の場合は従来の等間隔配置
        float totalWidth = (panelCount - 1) * (canvasSize.x + panelSpacing);
        float startX = -totalWidth / 2f;
        float x = startX + index * (canvasSize.x + panelSpacing);
        
        return new Vector3(x, 0, 0);
    }
    
    void CreateSinglePanel(int panelIndex, string panelName, Vector3 offset)
    {
        // パネル用のGameObjectを作成
        GameObject panelGO = new GameObject(panelName);
        panelGO.transform.SetParent(transform.parent); // UISetupオブジェクトと同じ親に配置
        
        // パネルの初期位置を設定（カメラ位置を基準にした絶対座標）
        Vector3 cameraPosition = mainCamera != null ? mainCamera.transform.position : new Vector3(0, 1.6f, 0);
        Vector3 worldPosition = cameraPosition + offset;
        panelGO.transform.position = worldPosition;
        
        // 初期状態でカメラの方向を向くように設定
        if (mainCamera != null && alwaysFaceCamera)
        {
            Vector3 directionToCamera = mainCamera.transform.position - worldPosition;
            directionToCamera.y = 0; // Y軸回転のみ（水平回転のみ）
            
            if (directionToCamera.magnitude > 0.01f)
            {
                // UI要素が正面を向くように180度回転を追加（初期設定）
                panelGO.transform.rotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(0, 180, 0);
                Debug.Log($"[UISetup] {panelName} を初期設定でカメラ方向に向けました: {panelGO.transform.rotation.eulerAngles}");
            }
            else
            {
                panelGO.transform.rotation = transform.rotation;
                Debug.Log($"[UISetup] {panelName} は距離が近すぎるため、デフォルト回転を使用");
            }
        }
        else
        {
            panelGO.transform.rotation = transform.rotation;
            Debug.Log($"[UISetup] {panelName} はカメラ追従無効のため、デフォルト回転を使用");
        }
        
        panelGO.transform.localScale = transform.localScale;
        
        // Canvasコンポーネント追加
        Canvas canvas = panelGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = panelIndex; // パネルごとに異なるソート順序
        
        // CanvasScaler追加
        CanvasScaler scaler = panelGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        
        Debug.Log($"[UISetup] CanvasScaler設定 - ScaleMode: {scaler.uiScaleMode}, ScaleFactor: {scaler.scaleFactor}");
        
        // GraphicRaycaster追加
        GraphicRaycaster raycaster = panelGO.AddComponent<GraphicRaycaster>();
        
        // Canvas RectTransform設定 - 正常な1:1座標系を使用
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize * 100f;  // Canvas内部のピクセル解像度
        canvasRect.localScale = Vector3.one * canvasScale;  // 最終的なワールドスケール
        
        Debug.Log($"[UISetup] Canvas設定 - sizeDelta: {canvasRect.sizeDelta}, localScale: {canvasRect.localScale}");
        
        // リストに追加
        panelCanvases.Add(canvas);
        panelRects.Add(canvasRect);
        panelTransforms.Add(panelGO.transform);
        
        // 背景パネル作成
        CreateBackgroundPanel(panelGO.transform, panelIndex);
        
        // ボタンコンテナ作成
        CreateButtonsForPanel(panelGO.transform, panelIndex);
        
        Debug.Log($"[UISetup] {panelName}を作成しました - 位置: {panelGO.transform.position}");
    }
    
    void CreateBackgroundPanel(Transform parent, int panelIndex)
    {
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(parent);
        
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.anchoredPosition3D = Vector3.zero;
        bgRect.localPosition = Vector3.zero;
        bgRect.localRotation = Quaternion.identity; // ローカル回転をリセット
        bgRect.localScale = Vector3.one;
        
        Image bgImage = bgPanel.AddComponent<Image>();
        // 柔軟なパネル色システム
        bgImage.color = GetPanelColor(panelIndex);
        
        // 背景パネルにコライダーを追加
        BoxCollider bgCollider = bgPanel.AddComponent<BoxCollider>();
        float scaleCorrection = 100f;
        bgCollider.size = new Vector3(canvasSize.x * scaleCorrection, canvasSize.y * scaleCorrection, 5f);
        bgCollider.isTrigger = false;
        bgCollider.center = Vector3.zero;
        
        Debug.Log($"[UISetup] {parent.name}に背景パネル作成 - Size: {bgCollider.size}");
    }
    
    void FacePanelsToCamera()
    {
        if (mainCamera == null || panelTransforms.Count == 0) return;
        
        Vector3 cameraPosition = mainCamera.transform.position;
        
        // 各パネルをカメラの方向に向ける
        for (int i = 0; i < panelTransforms.Count; i++)
        {
            Transform panelTransform = panelTransforms[i];
            if (panelTransform != null)
            {
                // カメラに向ける方向を計算
                Vector3 directionToCamera = cameraPosition - panelTransform.position;
                directionToCamera.y = 0; // Y軸回転のみ（水平回転のみ）
                
                if (directionToCamera.magnitude > 0.01f)
                {
                    // UI要素が正面を向くように180度回転を追加
                    Quaternion targetRotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(0, 180, 0);
                    
                    // 初回は即座に回転、その後はスムーズに回転
                    float rotationSpeed = Time.timeSinceLevelLoad < 1f ? 10f : 2f;
                    panelTransform.rotation = Quaternion.Slerp(
                        panelTransform.rotation, 
                        targetRotation, 
                        Time.deltaTime * rotationSpeed
                    );
                }
            }
        }
    }
    
    /// <summary>
    /// パネル数を動的に変更
    /// </summary>
    public void SetPanelCount(int newCount)
    {
        if (newCount < 1) newCount = 1;
        if (newCount > 10) newCount = 10; // 最大10パネル
        
        if (newCount != panelCount)
        {
            panelCount = newCount;
            RebuildPanels();
        }
    }
    
    /// <summary>
    /// パネルシステムを再構築
    /// </summary>
    public void RebuildPanels()
    {
        // 既存のパネルを削除
        for (int i = 0; i < panelTransforms.Count; i++)
        {
            if (panelTransforms[i] != null)
            {
                DestroyImmediate(panelTransforms[i].gameObject);
            }
        }
        
        // リストをクリア
        panelCanvases.Clear();
        panelRects.Clear();
        panelTransforms.Clear();
        
        // 新しいパネルを作成
        CreateFlexiblePanels();
        
        Debug.Log($"[UISetup] パネルシステムを再構築しました - パネル数: {panelCount}");
    }
    
    /// <summary>
    /// パネル位置をカメラから指定距離に設定
    /// </summary>
    public void UpdatePanelPositions()
    {
        if (mainCamera == null) return;
        
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 basePosition = mainCamera.transform.position + cameraForward * distanceFromCamera;
        
        for (int i = 0; i < panelTransforms.Count; i++)
        {
            if (panelTransforms[i] != null)
            {
                Vector3 offset = CalculatePanelPosition(i);
                // カメラの右方向を基準にオフセットを計算
                Vector3 worldOffset = cameraRight * offset.x + Vector3.up * offset.y;
                panelTransforms[i].position = basePosition + worldOffset;
            }
        }
    }
    
    Color GetPanelColor(int panelIndex)
    {
        // HSVを使って柔軟に色を生成
        if (panelCount <= 3)
        {
            // 3個以下の場合は固定色
            Color[] fixedColors = {
                new Color(0.3f, 0.2f, 0.2f, 0.9f), // 赤系
                new Color(0.2f, 0.2f, 0.2f, 0.9f), // グレー
                new Color(0.2f, 0.2f, 0.3f, 0.9f)  // 青系
            };
            return panelIndex < fixedColors.Length ? fixedColors[panelIndex] : Color.gray;
        }
        else
        {
            // 4個以上の場合はHSVで等間隔に分割
            float hue = (float)panelIndex / panelCount;
            Color color = Color.HSVToRGB(hue, 0.3f, 0.8f);
            color.a = 0.9f;
            return color;
        }
    }
    
    void CreateButtonsForPanel(Transform parent, int panelIndex)
    {
        // 中央パネル（インデックス1）の場合はファイルエクスプローラーを作成
        if (panelIndex == 1)
        {
            Debug.Log("[UISetup] 中央パネルにシンプルなファイルエクスプローラーを作成");
            CreateSimpleFileExplorer(parent);
            return;
        }
        
        // 右パネル（インデックス2）の場合はメディアビューアを作成
        if (panelIndex == 2)
        {
            Debug.Log("[UISetup] 右パネルにメディアビューアを作成");
            CreateMediaViewerPanel(parent);
            return;
        }
        
        // 他のパネル（左・右）は従来のボタンを作成
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(parent);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.9f);
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.anchoredPosition3D = Vector3.zero;
        containerRect.localPosition = new Vector3(0, 0, 0);
        containerRect.localRotation = Quaternion.identity; // ローカル回転をリセット
        containerRect.localScale = Vector3.one;
        
        // GridLayoutGroup追加
        GridLayoutGroup grid = buttonContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(40, 30);
        grid.spacing = new Vector2(buttonSpacing * 30, buttonSpacing * 30);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // パネルごとに異なるボタンを作成
        string[] panelPrefixes = {"L", "C", "R"}; // Left, Center, Right
        
        for (int row = 0; row < buttonRows; row++)
        {
            for (int col = 0; col < buttonColumns; col++)
            {
                int buttonIndex = row * buttonColumns + col + 1;
                int globalButtonIndex = panelIndex * (buttonRows * buttonColumns) + buttonIndex;
                string buttonText = $"{panelPrefixes[panelIndex]}{buttonIndex}";
                CreateButton(buttonContainer.transform, buttonText, globalButtonIndex, panelIndex);
            }
        }
    }
    
    void CreateButton(Transform parent, string buttonText, int globalIndex, int panelIndex)
    {
        // ボタンGameObject作成
        GameObject buttonGO = new GameObject(buttonText);
        buttonGO.transform.SetParent(parent);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity; // ローカル回転をリセット
        buttonRect.anchoredPosition3D = Vector3.zero;
        
        // ボタンコンポーネント追加
        Button button = buttonGO.AddComponent<Button>();
        
        // パネルごとに異なるボタン色
        Color[] panelButtonColors = {
            new Color(0.8f, 0.3f, 0.3f, 1f), // 左パネル: 赤系
            new Color(0.3f, 0.5f, 0.8f, 1f), // 中央パネル: 青系
            new Color(0.3f, 0.8f, 0.3f, 1f) // 右パネル: 緑系
        };
        
        // ボタン背景画像
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = panelButtonColors[panelIndex];
        
        // テキスト作成
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.anchoredPosition3D = Vector3.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localRotation = Quaternion.identity; // ローカル回転をリセット
        textRect.localScale = Vector3.one;
        
        // TextMeshPro使用
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = buttonText;
            tmpText.fontSize = 12;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
        }
        else
        {
            Text text = textGO.AddComponent<Text>();
            text.text = buttonText;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }
        
        // ボタンクリックイベント設定
        button.onClick.AddListener(() => OnButtonClick(globalIndex, panelIndex));
        
        // ボタンのカラー設定
        ColorBlock colors = button.colors;
        colors.normalColor = panelButtonColors[panelIndex];
        colors.highlightedColor = panelButtonColors[panelIndex] * 1.2f;
        colors.pressedColor = panelButtonColors[panelIndex] * 0.8f;
        button.colors = colors;
        
        // VRレーザーポインター用のコライダーを追加
        BoxCollider buttonCollider = buttonGO.AddComponent<BoxCollider>();
        buttonCollider.size = new Vector3(40f, 30f, 0.05f);
        buttonCollider.isTrigger = false;
        buttonCollider.center = Vector3.zero;
        
        Debug.Log($"[UISetup] ボタン作成: {buttonText} (パネル{panelIndex}), GlobalIndex: {globalIndex}");
    }
    
    void OnButtonClick(int globalIndex, int panelIndex)
    {
        string[] panelNames = {"左パネル", "中央パネル", "右パネル"};
        Debug.Log($"{panelNames[panelIndex]}のボタン {globalIndex} がクリックされました！");
        
        if (panoramaManager == null)
        {
            Debug.LogWarning("[UISetup] PanoramaSkyboxManagerが無効です");
            return;
        }
        
        // パネルごとに異なる機能を割り当て
        switch (panelIndex)
        {
            case 0: // 左パネル: パノラマ画像操作
                HandleLeftPanelClick(globalIndex);
                break;
            case 1: // 中央パネル: パノラマ動画操作
                HandleCenterPanelClick(globalIndex);
                break;
            case 2: // 右パネル: システム操作
                HandleRightPanelClick(globalIndex);
                break;
        }
    }
    
    void HandleLeftPanelClick(int buttonIndex)
    {
        // 左パネル: パノラマ画像関連
        int localIndex = (buttonIndex - 1) % 6 + 1;
        switch (localIndex)
        {
            case 1:
                panoramaManager.ShowPanoramaImage(0);
                Debug.Log("パノラマ画像1を表示");
                break;
            case 2:
                panoramaManager.ShowPanoramaImage(1);
                Debug.Log("パノラマ画像2を表示");
                break;
            case 3:
                panoramaManager.ShowPanoramaImage(2);
                Debug.Log("パノラマ画像3を表示");
                break;
            case 4:
                panoramaManager.NextImage();
                Debug.Log("次の画像に切り替え");
                break;
            case 5:
                panoramaManager.PreviousImage();
                Debug.Log("前の画像に切り替え");
                break;
            case 6:
                panoramaManager.SetDefaultSkybox();
                Debug.Log("デフォルトSkyboxに戻す");
                break;
        }
    }
    
    void HandleCenterPanelClick(int buttonIndex)
    {
        // 中央パネル: パノラマ動画関連
        int localIndex = (buttonIndex - 7) % 6 + 1;
        switch (localIndex)
        {
            case 1:
                panoramaManager.ShowPanoramaVideo(0);
                Debug.Log("パノラマ動画1を再生");
                break;
            case 2:
                if (panoramaManager.PanoramaVideoCount > 1)
                    panoramaManager.ShowPanoramaVideo(1);
                Debug.Log("パノラマ動画2を再生");
                break;
            case 3:
                panoramaManager.ToggleVideoPause();
                Debug.Log("動画の一時停止/再生");
                break;
            case 4:
                Debug.Log("動画を停止");
                break;
            case 5:
                Debug.Log("音量調整");
                break;
            case 6:
                Debug.Log("動画情報表示");
                break;
        }
    }
    
    void HandleRightPanelClick(int buttonIndex)
    {
        // 右パネル: システム操作
        int localIndex = (buttonIndex - 13) % 6 + 1;
        switch (localIndex)
        {
            case 1:
                Debug.Log("設定メニューを開く");
                break;
            case 2:
                Debug.Log("ヘルプを表示");
                break;
            case 3:
                Debug.Log("ファイルブラウザーを開く");
                break;
            case 4:
                Debug.Log("アプリケーションを終了");
                break;
            case 5:
                Debug.Log("リセット");
                break;
            case 6:
                Debug.Log("情報表示");
                break;
        }
    }
    
    void RemoveOldColliders()
    {
        // 既存のUIPanel_Colliderオブジェクトを削除
        GameObject colliderObject = GameObject.Find("UIPanel_Collider");
        if (colliderObject != null)
        {
            Debug.Log("[UISetup] 古いUIPanel_Colliderを削除します");
            DestroyImmediate(colliderObject);
        }
        
        // このCanvasのBoxColliderも削除
        BoxCollider canvasCollider = GetComponent<BoxCollider>();
        if (canvasCollider != null)
        {
            Debug.Log("[UISetup] CanvasのBoxColliderを削除します");
            DestroyImmediate(canvasCollider);
        }
    }
    
    
    void CreateSimpleFileExplorer(Transform parent)
    {
        Debug.Log("[UISetup] テスト用ファイルエクスプローラーを作成");
        
        // テスト用のファイルエクスプローラーコンテナ作成
        GameObject explorerContainer = new GameObject("TestFileExplorerContainer");
        explorerContainer.transform.SetParent(parent);
        
        RectTransform containerRect = explorerContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.anchoredPosition3D = Vector3.zero;
        containerRect.localPosition = Vector3.zero;
        containerRect.localRotation = Quaternion.identity;
        containerRect.localScale = Vector3.one;
        
        // テスト用パスバー作成
        CreateTestPathBar(explorerContainer.transform);
        
        // テスト用スクロールビュー作成
        CreateTestScrollView(explorerContainer.transform);
    }
    
    void CreateTestPathBar(Transform parent)
    {
        GameObject pathBar = new GameObject("TestPathBar");
        pathBar.transform.SetParent(parent);
        
        RectTransform pathRect = pathBar.AddComponent<RectTransform>();
        // 上端に固定、高さ18ピクセル（フォントサイズ10に適した高さ）
        pathRect.anchorMin = new Vector2(0f, 1f);
        pathRect.anchorMax = new Vector2(1f, 1f);
        pathRect.pivot = new Vector2(0.5f, 1f);
        pathRect.sizeDelta = new Vector2(0f, 18f);
        pathRect.anchoredPosition = new Vector2(0f, 0f);
        
        // 明示的にlocalPositionをリセット（Z軸の問題を防ぐ）
        pathRect.localPosition = new Vector3(pathRect.localPosition.x, pathRect.localPosition.y, 0f);
        pathRect.localRotation = Quaternion.identity;
        pathRect.localScale = Vector3.one;
        
        // 背景（少し濃くして視認性を向上）
        Image pathBg = pathBar.AddComponent<Image>();
        pathBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        
        Debug.Log($"[UISetup] TestPathBar配置 - AnchoredPos: {pathRect.anchoredPosition}, LocalPos: {pathRect.localPosition}, SizeDelta: {pathRect.sizeDelta}");
        
        // パステキスト
        GameObject pathTextGO = new GameObject("TestPathText");
        pathTextGO.transform.SetParent(pathBar.transform);
        
        RectTransform pathTextRect = pathTextGO.AddComponent<RectTransform>();
        pathTextRect.anchorMin = Vector2.zero;
        pathTextRect.anchorMax = Vector2.one;
        pathTextRect.sizeDelta = Vector2.zero;
        pathTextRect.anchoredPosition = new Vector2(0f, -9f);  // パスバーの高さ(18px)の半分下げる
        pathTextRect.anchoredPosition3D = new Vector3(0f, -9f, 0f);
        pathTextRect.localPosition = new Vector3(0f, -9f, 0f);
        pathTextRect.localRotation = Quaternion.identity;
        pathTextRect.localScale = Vector3.one;
        
        TextMeshProUGUI pathText = pathTextGO.AddComponent<TextMeshProUGUI>();
        // プラットフォームに応じた表示
        #if UNITY_EDITOR
            pathText.text = "Assets/StreamingAssets" + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
        #else
            // 実機では現在のストレージ名を表示
            string displayName = string.IsNullOrEmpty(baseFolderPath) ? "Storage" : AndroidFileAccess.GetDisplayName(baseFolderPath);
            pathText.text = displayName + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
        #endif
        pathText.fontSize = 10f;  // 視認性を考慮して大きくする
        pathText.color = Color.white;
        pathText.alignment = TextAlignmentOptions.Left;  // 水平左揃え
        pathText.verticalAlignment = VerticalAlignmentOptions.Middle;  // 垂直中央揃え
        
        // パスバーテキストの参照を保存
        pathBarText = pathText;
        pathText.margin = new Vector4(8f, 0f, 8f, 0f);  // 左右マージンのみ、上下は0
        pathText.enableWordWrapping = false;
        pathText.overflowMode = TextOverflowModes.Ellipsis;
    }
    
    void CreateMediaViewerPanel(Transform parent)
    {
        Debug.Log("[UISetup] メディアビューアパネルを作成");
        
        // メディアビューアコンテナ作成
        GameObject mediaContainer = new GameObject("MediaViewerContainer");
        mediaContainer.transform.SetParent(parent);
        
        RectTransform containerRect = mediaContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.anchoredPosition3D = Vector3.zero;
        containerRect.localPosition = Vector3.zero;
        containerRect.localRotation = Quaternion.identity;
        containerRect.localScale = Vector3.one;
        
        // タイトルバー作成
        CreateMediaTitleBar(mediaContainer.transform);
        
        // メディア表示エリア作成
        CreateMediaDisplayArea(mediaContainer.transform);
        
        // MediaViewerに参照を保存
        MediaViewer viewer = MediaViewer.Instance;
        if (viewer != null)
        {
            // MediaViewerにパネル参照を渡す
            viewer.SetMediaPanel(mediaContainer);
        }
    }
    
    void CreateMediaTitleBar(Transform parent)
    {
        GameObject titleBar = new GameObject("MediaTitleBar");
        titleBar.transform.SetParent(parent);
        
        RectTransform titleRect = titleBar.AddComponent<RectTransform>();
        // 上端に固定、高さ20ピクセル
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 20f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.localPosition = new Vector3(titleRect.localPosition.x, titleRect.localPosition.y, 0f);
        titleRect.localRotation = Quaternion.identity;
        titleRect.localScale = Vector3.one;
        
        // 背景
        Image titleBg = titleBar.AddComponent<Image>();
        titleBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // タイトルテキスト
        GameObject titleTextGO = new GameObject("TitleText");
        titleTextGO.transform.SetParent(titleBar.transform);
        
        RectTransform titleTextRect = titleTextGO.AddComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0.05f, 0f);
        titleTextRect.anchorMax = new Vector2(0.95f, 1f);
        titleTextRect.sizeDelta = Vector2.zero;
        titleTextRect.anchoredPosition = Vector2.zero;
        titleTextRect.localPosition = Vector3.zero;
        titleTextRect.localRotation = Quaternion.identity;
        titleTextRect.localScale = Vector3.one;
        
        TextMeshProUGUI titleText = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "メディアビューア";
        titleText.fontSize = 12f;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        titleText.color = Color.white;
        titleText.margin = new Vector4(5f, 0f, 5f, 0f);
    }
    
    void CreateMediaDisplayArea(Transform parent)
    {
        GameObject displayArea = new GameObject("MediaDisplayArea");
        displayArea.transform.SetParent(parent);
        
        RectTransform displayRect = displayArea.AddComponent<RectTransform>();
        // タイトルバーの下から下端まで
        displayRect.anchorMin = new Vector2(0.05f, 0.05f);
        displayRect.anchorMax = new Vector2(0.95f, 0.9f);  // タイトルバーの分だけ上を空ける
        displayRect.sizeDelta = Vector2.zero;
        displayRect.anchoredPosition = Vector2.zero;
        displayRect.localPosition = Vector3.zero;
        displayRect.localRotation = Quaternion.identity;
        displayRect.localScale = Vector3.one;
        
        // 背景（少し暗めの色）
        Image displayBg = displayArea.AddComponent<Image>();
        displayBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // プレースホルダーテキスト
        GameObject placeholderGO = new GameObject("PlaceholderText");
        placeholderGO.transform.SetParent(displayArea.transform);
        
        RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.localPosition = Vector3.zero;
        placeholderRect.localRotation = Quaternion.identity;
        placeholderRect.localScale = Vector3.one;
        
        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "ファイルを選択してください\n\n対応形式:\n- 画像: JPG, PNG, GIF等\n- 動画: MP4, MOV, AVI等\n\nパノラマコンテンツ(360度)\nは左パネルに表示されます";
        placeholderText.fontSize = 10f;
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.verticalAlignment = VerticalAlignmentOptions.Middle;
        placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        placeholderText.enableWordWrapping = true;
        
        Debug.Log("[UISetup] メディア表示エリア作成完了");
    }
    
    void CreateTestScrollView(Transform parent)
    {
        // CenterPanelのサイズを取得（canvasSize.x * 100 = 実際の幅）
        float panelWidth = canvasSize.x * 100f;  // CenterPanelの実際の幅
        
        // スクロールバーの幅を定義
        float scrollbarWidth = 10f;  // スクロールバーの幅
        float scrollbarSpacing = 3f;  // スクロールバーとコンテンツの間隔
        
        // 実際に使用可能な幅（スクロールバー分を除く）
        float usableWidth = panelWidth - scrollbarWidth - scrollbarSpacing;
        
        // パーセンテージベースの計算（SerializeFieldから取得）
        float paddingPercent = explorerPaddingPercent;
        float marginPercent = explorerMarginPercent;
        int columnsCount = explorerColumnsCount;
        
        // アイコンサイズを計算して指定個数がぴったり収まるようにする
        // 使用可能幅 = パディング左 + (アイコン幅×N + マージン×(N-1)) + パディング右
        float availableWidthPercent = 1.0f - (paddingPercent * 2);
        float totalMarginPercent = marginPercent * (columnsCount - 1);
        float iconSizePercent = (availableWidthPercent - totalMarginPercent) / columnsCount;
        
        // 実際のピクセル値を計算（使用可能幅を基準に）
        float paddingSize = usableWidth * paddingPercent;
        float iconSize = usableWidth * iconSizePercent;
        float marginSize = usableWidth * marginPercent;
        
        Debug.Log($"[UISetup] ファイルエクスプローラーレイアウト計算:");
        Debug.Log($"  - CenterPanel幅: {panelWidth}px");
        Debug.Log($"  - スクロールバー幅: {scrollbarWidth}px + 間隔: {scrollbarSpacing}px");
        Debug.Log($"  - 使用可能幅: {usableWidth}px");
        Debug.Log($"  - パディング: {paddingSize:F1}px ({paddingPercent*100:F1}%)");
        Debug.Log($"  - アイコンサイズ: {iconSize:F1}px ({iconSizePercent*100:F1}%)");
        Debug.Log($"  - マージン: {marginSize:F1}px ({marginPercent*100:F1}%)");
        Debug.Log($"  - 横に並ぶ数: {columnsCount}個");
        
        // スクロールビュー作成（パスバーの下に配置）
        GameObject scrollView = new GameObject("TestScrollView");
        scrollView.transform.SetParent(parent);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        // パスバーの下（18ピクセル下）から下端まで
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(0f, 0f);     // Left=0, Bottom=0
        scrollRect.offsetMax = new Vector2(0f, -18f);   // Right=0, Top=-18（パスバーの高さ分）
        
        // localPositionを明示的に設定
        scrollRect.anchoredPosition = Vector2.zero;
        scrollRect.localPosition = new Vector3(0f, -9f, 0f); // Y位置を調整（パスバー高さの半分）
        scrollRect.localRotation = Quaternion.identity;
        scrollRect.localScale = Vector3.one;
        
        // ScrollRectコンポーネント
        ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();
        scrollRectComponent.horizontal = false;
        scrollRectComponent.vertical = true;
        scrollRectComponent.movementType = ScrollRect.MovementType.Elastic;
        scrollRectComponent.elasticity = 0.1f;
        scrollRectComponent.scrollSensitivity = 30f;
        scrollRectComponent.inertia = true;
        scrollRectComponent.decelerationRate = 0.135f;
        
        // VRコントローラースクロール機能を追加
        VRScrollController vrScrollController = scrollView.AddComponent<VRScrollController>();
        Debug.Log("[UISetup] VRScrollControllerを追加しました");
        
        // 縦スクロールバーを作成
        GameObject verticalScrollbar = new GameObject("VerticalScrollbar");
        verticalScrollbar.transform.SetParent(scrollView.transform);
        
        RectTransform scrollbarRect = verticalScrollbar.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, 0f);
        scrollbarRect.anchoredPosition = new Vector2(-2f, 0f);
        scrollbarRect.localPosition = new Vector3(scrollbarRect.localPosition.x, scrollbarRect.localPosition.y, 0f);
        scrollbarRect.localRotation = Quaternion.identity;
        scrollbarRect.localScale = Vector3.one;
        
        Image scrollbarBg = verticalScrollbar.AddComponent<Image>();
        scrollbarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        
        Scrollbar scrollbarComponent = verticalScrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
        
        // スクロールバーハンドル
        GameObject scrollbarHandle = new GameObject("Handle");
        scrollbarHandle.transform.SetParent(verticalScrollbar.transform);
        
        RectTransform handleRect = scrollbarHandle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.localPosition = Vector3.zero;
        handleRect.localRotation = Quaternion.identity;
        handleRect.localScale = Vector3.one;
        
        // ハンドルを細くするために左側にマージンを設定
        handleRect.offsetMin = new Vector2(4f, 0f);  // Left (0.4 * 10), Bottom
        handleRect.offsetMax = new Vector2(0f, 0f);  // Right, Top
        
        Image handleImage = scrollbarHandle.AddComponent<Image>();
        handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        
        scrollbarComponent.targetGraphic = handleImage;
        scrollbarComponent.handleRect = handleRect;
        scrollbarComponent.size = 0.3f;
        
        // ScrollRectにスクロールバーを接続
        scrollRectComponent.verticalScrollbar = scrollbarComponent;
        scrollRectComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRectComponent.verticalScrollbarSpacing = -scrollbarSpacing;
        
        // ビューポート
        GameObject viewport = new GameObject("TestViewport");
        viewport.transform.SetParent(scrollView.transform);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.anchoredPosition3D = Vector3.zero;
        viewportRect.localPosition = Vector3.zero;
        viewportRect.localRotation = Quaternion.identity;
        viewportRect.localScale = Vector3.one;
        
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        
        scrollRectComponent.viewport = viewportRect;
        
        // コンテント
        GameObject contentGO = new GameObject("TestContent");
        contentGO.transform.SetParent(viewport.transform);
        
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);  // 上部を基準にする
        contentRect.sizeDelta = new Vector2(0f, 0f);  // 初期サイズは0（ContentSizeFitterが自動調整）
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.anchoredPosition3D = Vector3.zero;
        contentRect.localPosition = Vector3.zero;
        contentRect.localRotation = Quaternion.identity;
        contentRect.localScale = Vector3.one;
        
        scrollRectComponent.content = contentRect;
        
        // ContentSizeFitterを追加して自動的にコンテンツサイズを調整
        ContentSizeFitter contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // GridLayoutGroup
        GridLayoutGroup gridLayout = contentGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(iconSize, iconSize);  // アイコンサイズ: 20%
        gridLayout.spacing = new Vector2(marginSize, marginSize);      // マージン: 2%
        gridLayout.padding = new RectOffset((int)paddingSize, (int)paddingSize, (int)paddingSize, (int)paddingSize); // パディング: 2%
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnsCount;  // 横に指定個数並べる
        
        // コンテナの参照を保存
        fileButtonsContainer = contentGO.transform;
        
        // テストデータでファイル・フォルダボタンを作成（サイズ情報を渡す）
        CreateTestFileButtons(contentGO.transform, iconSize);
        
        Debug.Log("[UISetup] テスト用ファイルエクスプローラー作成完了");
    }
    
    void CreateTestFileButtons(Transform parent, float iconSize)
    {
        // プラットフォームに応じて適切なパスを設定
        string targetPath;
        
        if (string.IsNullOrEmpty(baseFolderPath))
        {
            // 初回実行時：利用可能なパスを取得してベースパスを設定
            #if UNITY_EDITOR
                // エディタではStreamingAssetsを使用
                availablePaths = new string[] { Path.Combine(Application.dataPath, "StreamingAssets") };
                baseFolderPath = availablePaths[0];
                targetPath = baseFolderPath;
            #else
                // 実機では利用可能な外部ストレージパスを取得
                availablePaths = AndroidFileAccess.GetAvailablePaths();
                
                if (availablePaths.Length > 0)
                {
                    baseFolderPath = availablePaths[currentPathIndex];
                    targetPath = baseFolderPath;
                    
                    // persistentDataPathの場合のみサンプルフォルダ構造を作成
                    if (baseFolderPath == Application.persistentDataPath)
                    {
                        CreateSampleFolderStructure(baseFolderPath);
                    }
                }
                else
                {
                    // フォールバック
                    baseFolderPath = Application.persistentDataPath;
                    targetPath = baseFolderPath;
                    CreateSampleFolderStructure(baseFolderPath);
                }
                
                Debug.Log($"[UISetup] 利用可能なストレージパス数: {availablePaths.Length}");
                for (int i = 0; i < availablePaths.Length; i++)
                {
                    Debug.Log($"[UISetup] Path[{i}]: {AndroidFileAccess.GetDisplayName(availablePaths[i])} - {availablePaths[i]}");
                }
            #endif
            
            Debug.Log($"[UISetup] ベースフォルダパス設定: {baseFolderPath}");
        }
        else
        {
            // ベースパスに相対パスを結合
            targetPath = string.IsNullOrEmpty(currentFolderPath) 
                ? baseFolderPath 
                : Path.Combine(baseFolderPath, currentFolderPath);
        }
        
        // フォルダが存在しない場合は作成
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
            Debug.Log($"[UISetup] フォルダを作成: {targetPath}");
        }
        
        Debug.Log($"[UISetup] フォルダ内容を読み込み: {targetPath}");
        
        List<string> items = new List<string>();
        List<bool> isFolder = new List<bool>();
        List<bool> isParentFolder = new List<bool>();  // 上位フォルダフラグ
        
        // 上位フォルダがある場合は「↑」アイコンを最初に追加
        if (!string.IsNullOrEmpty(currentFolderPath))
        {
            items.Add("↑");
            isFolder.Add(true);
            isParentFolder.Add(true);
            Debug.Log("[UISetup] 上位フォルダアイコンを追加");
        }
        
        // フォルダを先に追加
        try
        {
            string[] directories = Directory.GetDirectories(targetPath);
            Debug.Log($"[UISetup] フォルダ検索結果: {directories.Length}個のフォルダを発見");
            
            foreach (string dir in directories)
            {
                string dirName = Path.GetFileName(dir);
                Debug.Log($"[UISetup] フォルダ発見: {dirName}");
                
                // .metaファイルのフォルダは除外
                if (!dirName.EndsWith(".meta"))
                {
                    items.Add(dirName);
                    isFolder.Add(true);
                    isParentFolder.Add(false);  // 通常のフォルダ
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISetup] フォルダ取得エラー: {e.Message}");
        }
        
        // ファイルを追加
        try
        {
            string[] fileNames;
            
            // プラットフォーム判定（ランタイム版）
            if (Application.isEditor)
            {
                // エディタでは従来のDirectory.GetFiles使用
                string[] files = Directory.GetFiles(targetPath);
                fileNames = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    fileNames[i] = Path.GetFileName(files[i]);
                }
                Debug.Log($"[UISetup] ファイル検索結果: {fileNames.Length}個のファイルを発見");
            }
            else
            {
                // 実機ではMediaStore APIを組み合わせたファイル取得を使用
                fileNames = AndroidFileAccess.GetFilesWithMediaStore(targetPath);
                Debug.Log($"[UISetup] MediaStore統合ファイル検索結果: {fileNames.Length}個のファイルを発見");
            }
            
            foreach (string fileName in fileNames)
            {
                Debug.Log($"[UISetup] ファイル発見: {fileName}");
                
                // .metaファイルは除外
                if (!fileName.EndsWith(".meta"))
                {
                    items.Add(fileName);
                    isFolder.Add(false);
                    isParentFolder.Add(false);  // 通常のファイル
                    Debug.Log($"[UISetup] ファイル追加: {fileName}");
                }
                else
                {
                    Debug.Log($"[UISetup] .metaファイルをスキップ: {fileName}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISetup] ファイル取得エラー: {e.Message}");
        }
        
        Debug.Log($"[UISetup] 最終的な検出アイテム数: {items.Count} (フォルダ含む)");
        
        // ボタンを作成
        for (int i = 0; i < items.Count; i++)
        {
            CreateTestFileButton(parent, items[i], isFolder[i], iconSize, isParentFolder[i]);
        }
    }
    
    void CreateTestFileButton(Transform parent, string itemName, bool isDirectory, float iconSize, bool isParentFolder = false)
    {
        GameObject button = new GameObject(itemName);
        button.transform.SetParent(parent);
        
        RectTransform buttonRect = button.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(iconSize, iconSize);  // 動的アイコンサイズ
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.anchoredPosition3D = Vector3.zero;
        buttonRect.localPosition = Vector3.zero;
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;
        
        Button buttonComponent = button.AddComponent<Button>();
        Image buttonImage = button.AddComponent<Image>();
        buttonImage.color = isDirectory ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f); // フォルダは黄色、ファイルは白
        
        // VRインタラクション用のコライダーを追加
        BoxCollider buttonCollider = button.AddComponent<BoxCollider>();
        buttonCollider.size = new Vector3(iconSize, iconSize, 1f);  // 動的コライダーサイズ
        buttonCollider.isTrigger = false;
        
        // ボタンテキスト
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(button.transform);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        // テキストマージンの設定
        if (isParentFolder)
        {
            // 上位フォルダアイコンは中央配置なのでマージン不要
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        else
        {
            // 通常のファイル・フォルダは左右に同量のマージンを追加（アイコンサイズの5%）
            float margin = iconSize * 0.05f;
            textRect.offsetMin = new Vector2(margin, 0f);
            textRect.offsetMax = new Vector2(-margin, 0f);
        }
        textRect.anchoredPosition3D = Vector3.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;
        
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = itemName;
        
        if (isParentFolder)
        {
            // 上位フォルダアイコン（↑）の場合は大きめフォントで中央配置
            text.fontSize = iconSize * 0.35f;  // 通常より大きく（35%）
            text.alignment = TextAlignmentOptions.Center;  // 中央揃え
            text.fontStyle = FontStyles.Bold;
        }
        else
        {
            // 通常のファイル・フォルダの場合
            text.fontSize = iconSize * 0.15f;  // アイコンサイズの15%でフォントサイズを動的に調整
            text.alignment = TextAlignmentOptions.Left;  // 左揃え
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
        }
        
        text.color = Color.black;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        
        // クリックイベント
        buttonComponent.onClick.AddListener(() => {
            if (isParentFolder)
            {
                // 上位フォルダアイコンの場合は親フォルダに移動
                NavigateToParentFolder();
            }
            else if (isDirectory)
            {
                // 通常のフォルダの場合はそのフォルダに移動
                NavigateToFolder(itemName);
            }
            else
            {
                // ファイルの場合はメディアビューアで開く
                Debug.Log($"[UISetup] ファイルクリック: {itemName}");
                OpenMediaFile(itemName);
            }
        });
        
        Debug.Log($"[UISetup] テストボタン作成: {itemName}");
    }
    
    /// <summary>
    /// 指定されたフォルダに移動
    /// </summary>
    void NavigateToFolder(string folderName)
    {
        // 新しいパスを構築（相対パス）
        currentFolderPath = string.IsNullOrEmpty(currentFolderPath) 
            ? folderName 
            : Path.Combine(currentFolderPath, folderName);
        
        Debug.Log($"[UISetup] フォルダ移動: {currentFolderPath}");
        
        // パスバーを更新
        if (pathBarText != null)
        {
            #if UNITY_EDITOR
                pathBarText.text = "Assets/StreamingAssets" + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
            #else
                string displayName = AndroidFileAccess.GetDisplayName(baseFolderPath);
                pathBarText.text = displayName + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
            #endif
        }
        
        // ファイルリストを更新
        RefreshFileList();
    }
    
    /// <summary>
    /// ファイルリストを再読み込み
    /// </summary>
    void RefreshFileList()
    {
        if (fileButtonsContainer == null) return;
        
        // 既存のボタンをすべて削除
        foreach (Transform child in fileButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // GridLayoutGroupから必要な情報を取得
        GridLayoutGroup gridLayout = fileButtonsContainer.GetComponent<GridLayoutGroup>();
        float iconSize = gridLayout.cellSize.x;
        
        // 新しいファイルリストを作成
        CreateTestFileButtons(fileButtonsContainer, iconSize);
        
        Debug.Log($"[UISetup] ファイルリスト更新完了: {currentFolderPath}");
    }
    
    /// <summary>
    /// 利用可能なストレージパス間を切り替え
    /// </summary>
    void SwitchStoragePath()
    {
        #if !UNITY_EDITOR
        if (availablePaths == null || availablePaths.Length <= 1)
        {
            Debug.Log("[UISetup] 切り替え可能なストレージパスがありません");
            return;
        }
        
        // 次のパスに切り替え
        currentPathIndex = (currentPathIndex + 1) % availablePaths.Length;
        string newBasePath = availablePaths[currentPathIndex];
        
        // パス変更
        baseFolderPath = newBasePath;
        currentFolderPath = "";  // ルートに戻る
        
        string displayName = AndroidFileAccess.GetDisplayName(baseFolderPath);
        Debug.Log($"[UISetup] ストレージを切り替え: {displayName} ({baseFolderPath})");
        
        // パスバーを更新
        if (pathBarText != null)
        {
            pathBarText.text = displayName;
        }
        
        // ファイルリストを更新
        RefreshFileList();
        #else
        Debug.Log("[UISetup] ストレージ切り替えはエディタでは無効です");
        #endif
    }
    
    /// <summary>
    /// VRコントローラーのナビゲーション入力をチェック
    /// </summary>
    void CheckNavigationInput()
    {
        // 右コントローラーのBボタンをチェック（上位フォルダ）
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
        {
            bool bButtonPressed;
            if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bButtonPressed))
            {
                if (bButtonPressed && !previousBButtonPressed)  // ボタンが押された瞬間のみ
                {
                    NavigateToParentFolder();
                }
            }
            previousBButtonPressed = bButtonPressed;
            
            // 右コントローラーのAボタンをチェック（ストレージ切替）
            bool aButtonPressed;
            if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out aButtonPressed))
            {
                if (aButtonPressed && !previousAButtonPressed)  // ボタンが押された瞬間のみ
                {
                    SwitchStoragePath();
                }
            }
            previousAButtonPressed = aButtonPressed;
        }
        
        // 左コントローラーのBボタンもチェック（予備）
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid)
        {
            bool bButtonPressed;
            if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bButtonPressed))
            {
                if (bButtonPressed && !previousLeftBButtonPressed)  // ボタンが押された瞬間のみ
                {
                    NavigateToParentFolder();
                }
            }
            previousLeftBButtonPressed = bButtonPressed;
        }
    }
    
    /// <summary>
    /// VRコントローラーのグリップボタン（リフレッシュ）入力をチェック
    /// </summary>
    void CheckRefreshInput()
    {
        // 右コントローラーのグリップボタンでファイルリスト更新
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
        {
            float gripValue;
            if (rightController.TryGetFeatureValue(CommonUsages.grip, out gripValue))
            {
                // グリップボタンが押された（0.5以上の値）
                bool gripPressed = gripValue > 0.5f;
                
                if (gripPressed && !previousGripPressed)
                {
                    Debug.Log("=====================================");
                    Debug.Log("[UISetup] グリップボタン押下 - ファイルリスト更新開始");
                    Debug.Log("=====================================");
                    
                    // 現在のフォルダをMediaScannerで強制スキャン
                    string targetPath = string.IsNullOrEmpty(currentFolderPath) 
                        ? baseFolderPath 
                        : Path.Combine(baseFolderPath, currentFolderPath);
                    
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        Debug.Log($"[UISetup] 📁 スキャン対象フォルダ: {targetPath}");
                        Debug.Log($"[UISetup] 🔄 MediaScannerで強制スキャン実行中...");
                        AndroidFileAccess.ScanFolder(targetPath);
                    }
                    
                    Debug.Log("[UISetup] 🔄 ファイルリスト更新中...");
                    RefreshFileList();
                    Debug.Log("[UISetup] ✅ ファイルリスト更新完了！");
                    Debug.Log("=====================================");
                }
                
                previousGripPressed = gripPressed;
            }
        }
        
        // 左コントローラーのグリップボタンも同様にチェック（オプション）
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid)
        {
            float gripValue;
            if (leftController.TryGetFeatureValue(CommonUsages.grip, out gripValue))
            {
                bool gripPressed = gripValue > 0.5f;
                
                if (gripPressed && !previousLeftGripPressed)
                {
                    Debug.Log("=====================================");
                    Debug.Log("[UISetup] 左グリップボタン押下 - ファイルリスト更新開始");
                    Debug.Log("=====================================");
                    
                    string targetPath = string.IsNullOrEmpty(currentFolderPath) 
                        ? baseFolderPath 
                        : Path.Combine(baseFolderPath, currentFolderPath);
                    
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        Debug.Log($"[UISetup] 📁 スキャン対象フォルダ: {targetPath}");
                        Debug.Log($"[UISetup] 🔄 MediaScannerで強制スキャン実行中...");
                        AndroidFileAccess.ScanFolder(targetPath);
                    }
                    
                    Debug.Log("[UISetup] 🔄 ファイルリスト更新中...");
                    RefreshFileList();
                    Debug.Log("[UISetup] ✅ ファイルリスト更新完了！");
                    Debug.Log("=====================================");
                }
                
                previousLeftGripPressed = gripPressed;
            }
        }
    }
    
    private bool previousGripPressed = false; // 右グリップボタンの前回の状態
    private bool previousLeftGripPressed = false; // 左グリップボタンの前回の状態
    
    /// <summary>
    /// メディアファイルを開く
    /// </summary>
    void OpenMediaFile(string fileName)
    {
        // フルパスを構築
        string fullPath = string.IsNullOrEmpty(currentFolderPath) 
            ? Path.Combine(baseFolderPath, fileName)
            : Path.Combine(baseFolderPath, currentFolderPath, fileName);
        
        Debug.Log($"[UISetup] メディアファイルを開く: {fullPath}");
        
        // ファイルが存在するか確認
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[UISetup] ファイルが見つかりません: {fullPath}");
            return;
        }
        
        // MediaViewerが存在しない場合は作成
        MediaViewer viewer = MediaViewer.Instance;
        if (viewer == null)
        {
            Debug.Log("[UISetup] MediaViewerを作成します");
            GameObject viewerGO = new GameObject("MediaViewer");
            viewer = viewerGO.AddComponent<MediaViewer>();
        }
        
        // 常時表示メディアパネルが設定されていない場合は設定する
        Transform rightPanel = transform.Find("RightPanel");
        if (rightPanel != null)
        {
            Transform mediaContainer = rightPanel.Find("MediaViewerContainer");
            if (mediaContainer != null && viewer.permanentMediaPanel == null)
            {
                viewer.SetMediaPanel(mediaContainer.gameObject);
            }
        }
        
        // パノラマコンテンツの場合は左パネルを渡す
        GameObject targetPanel = null;
        string lowerName = fileName.ToLower();
        if (lowerName.Contains("360") || lowerName.Contains("panorama") || lowerName.Contains("pano"))
        {
            // 左パネルを探す
            Transform leftPanel = transform.Find("LeftPanel");
            if (leftPanel != null)
            {
                targetPanel = leftPanel.gameObject;
                Debug.Log("[UISetup] パノラマコンテンツを左パネルに表示します");
            }
        }
        
        // メディアファイルを開く
        viewer.OpenMediaFile(fullPath, targetPanel);
    }
    
    /// <summary>
    /// 上位フォルダ（親フォルダ）へ遷移
    /// </summary>
    void NavigateToParentFolder()
    {
        // ルートフォルダ（空の場合）なら何もしない
        if (string.IsNullOrEmpty(currentFolderPath))
        {
            Debug.Log("[UISetup] 既にルートフォルダにいます");
            return;
        }
        
        // パスを分解して親フォルダのパスを取得
        string parentPath = Path.GetDirectoryName(currentFolderPath);
        
        // パスの区切り文字を統一（Windowsの\をLinux/Unityの/に変換）
        if (!string.IsNullOrEmpty(parentPath))
        {
            parentPath = parentPath.Replace('\\', '/');
        }
        
        currentFolderPath = parentPath ?? "";  // nullの場合は空文字列
        
        Debug.Log($"[UISetup] 上位フォルダへ移動: '{currentFolderPath}' (元: '{parentPath}')");
        
        // パスバーを更新
        if (pathBarText != null)
        {
            #if UNITY_EDITOR
                pathBarText.text = "Assets/StreamingAssets" + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
            #else
                string displayName = AndroidFileAccess.GetDisplayName(baseFolderPath);
                pathBarText.text = displayName + (string.IsNullOrEmpty(currentFolderPath) ? "" : "/" + currentFolderPath);
            #endif
        }
        
        // ファイルリストを更新
        RefreshFileList();
    }
    
    /// <summary>
    /// 実機用のサンプルフォルダ構造を作成
    /// </summary>
    void CreateSampleFolderStructure(string basePath)
    {
        Debug.Log($"[UISetup] サンプルフォルダ構造を作成: {basePath}");
        
        // サンプルフォルダを作成
        string[] sampleFolders = {
            "Images",
            "Videos",
            "Documents",
            "Downloads",
            "Music"
        };
        
        foreach (string folder in sampleFolders)
        {
            string folderPath = Path.Combine(basePath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log($"[UISetup] フォルダ作成: {folder}");
            }
        }
        
        // サンプルファイルを作成（テキストファイル）
        string[] sampleFiles = {
            "readme.txt",
            "sample.txt",
            "test_document.txt"
        };
        
        foreach (string fileName in sampleFiles)
        {
            string filePath = Path.Combine(basePath, fileName);
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, $"This is a sample file: {fileName}\nCreated for VR File Explorer");
                Debug.Log($"[UISetup] ファイル作成: {fileName}");
            }
        }
        
        // Documentsフォルダにもサンプルファイルを作成
        string docsPath = Path.Combine(basePath, "Documents");
        string docFile = Path.Combine(docsPath, "document1.txt");
        if (!File.Exists(docFile))
        {
            File.WriteAllText(docFile, "Sample document in Documents folder");
        }
        
        Debug.Log($"[UISetup] サンプルフォルダ構造の作成完了");
    }
    
}