using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

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
    [SerializeField] private Vector2 canvasSize = new Vector2(3f, 2f);
    [SerializeField] private float canvasScale = 0.01f;
    
    [Header("ボタン設定")]
    [SerializeField] private int buttonRows = 2;
    [SerializeField] private int buttonColumns = 3;
    [SerializeField] private float buttonSpacing = 0.1f;
    
    // 柔軟なパネル管理
    private List<Canvas> panelCanvases = new List<Canvas>();
    private List<RectTransform> panelRects = new List<RectTransform>();
    private List<Transform> panelTransforms = new List<Transform>();
    private Camera mainCamera;
    private PanoramaSkyboxManager panoramaManager;
    private FileExplorerManager fileExplorerManager;
    
    void Start()
    {
        // カメラ参照を確実に取得
        StartCoroutine(InitializeAfterFrame());
    }
    
    System.Collections.IEnumerator InitializeAfterFrame()
    {
        // 1フレーム待ってからカメラを検索（他のオブジェクトが初期化される時間を確保）
        yield return null;
        
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
                    return new Vector3(0, 0, 4.5f);       // X:0, Z:4.5 前方中央（距離増加）
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
        scaler.dynamicPixelsPerUnit = 10;
        
        // GraphicRaycaster追加
        GraphicRaycaster raycaster = panelGO.AddComponent<GraphicRaycaster>();
        
        // Canvas RectTransform設定
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize * 100f;
        canvasRect.localScale = Vector3.one * canvasScale;
        
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
        Debug.Log("[UISetup] 実際のFileExplorerManagerを使用してファイルエクスプローラーを作成");
        
        // FileExplorerManagerを使用して実際のファイルシステムUI作成
        if (fileExplorerManager != null)
        {
            fileExplorerManager.SetupFileExplorerUI(parent);
            Debug.Log("[UISetup] FileExplorerManagerによるUI作成完了");
        }
        else
        {
            Debug.LogError("[UISetup] FileExplorerManagerが見つかりません");
            
            // フォールバック: シンプルなエラーメッセージ表示
            GameObject errorContainer = new GameObject("FileExplorerError");
            errorContainer.transform.SetParent(parent);
            
            RectTransform errorRect = errorContainer.AddComponent<RectTransform>();
            errorRect.anchorMin = Vector2.zero;
            errorRect.anchorMax = Vector2.one;
            errorRect.sizeDelta = Vector2.zero;
            errorRect.anchoredPosition = Vector2.zero;
            errorRect.localScale = Vector3.one;
            
            Image bgImage = errorContainer.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            
            GameObject textGO = new GameObject("ErrorText");
            textGO.transform.SetParent(errorContainer.transform);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.localScale = Vector3.one;
            
            TextMeshProUGUI errorText = textGO.AddComponent<TextMeshProUGUI>();
            errorText.text = "FileExplorerManager\nが見つかりません";
            errorText.fontSize = 6f;
            errorText.color = Color.red;
            errorText.alignment = TextAlignmentOptions.Center;
            errorText.enableWordWrapping = true;
        }
    }
    
}