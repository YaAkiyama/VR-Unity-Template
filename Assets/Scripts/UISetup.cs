using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIパネルのセットアップヘルパー
/// World SpaceのCanvasとボタンを作成
/// </summary>
public class UISetup : MonoBehaviour
{
    [Header("Canvas設定")]
    [SerializeField] private Vector2 canvasSize = new Vector2(3f, 2f);
    [SerializeField] private float canvasScale = 0.01f;
    
    [Header("ボタン設定")]
    [SerializeField] private int buttonRows = 2;
    [SerializeField] private int buttonColumns = 3;
    [SerializeField] private float buttonSpacing = 0.1f;
    
    private Canvas canvas;
    private RectTransform canvasRect;
    private PanoramaSkyboxManager panoramaManager;
    
    void Start()
    {
        // PanoramaSkyboxManagerを取得
        panoramaManager = FindObjectOfType<PanoramaSkyboxManager>();
        if (panoramaManager == null)
        {
            Debug.LogWarning("[UISetup] PanoramaSkyboxManagerが見つかりません。パノラマ機能は無効です。");
        }
        
        SetupCanvas();
        CreateButtons();
        RemoveOldColliders(); // 既存の古いコライダーを削除
        // ForceAddCollider(); // 個別ボタンコライダーを使用するため無効化
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
    
    void SetupCanvas()
    {
        // このGameObject自体をCanvasとして使用
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 0; // 3D空間での正しいレンダリング順序
        
        // CanvasScaler追加
        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.dynamicPixelsPerUnit = 10;
        
        // GraphicRaycaster追加（UIインタラクション用）
        GraphicRaycaster raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // Canvas RectTransform設定
        canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize * 100f; // Unity単位に変換
        canvasRect.localScale = Vector3.one * canvasScale;
        
        // Canvasのコライダーは削除（個別ボタンコライダーを使用）
        BoxCollider canvasCollider = gameObject.GetComponent<BoxCollider>();
        if (canvasCollider != null)
        {
            DestroyImmediate(canvasCollider);
            Debug.Log("[UISetup] Canvas BoxColliderを削除しました");
        }
        
        // 背景パネル作成
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(transform);
        
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.anchoredPosition3D = Vector3.zero; // Z位置も0に設定
        bgRect.localPosition = Vector3.zero; // ローカル位置を明示的に設定
        bgRect.localScale = Vector3.one;
        
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // 背景パネルにコライダーを追加（基本的なヒット検出用）
        BoxCollider bgCollider = bgPanel.AddComponent<BoxCollider>();
        // Canvas scale (0.01) の影響を補正 - 100倍にする
        float scaleCorrection = 100f; // 0.01 の逆数
        bgCollider.size = new Vector3(canvasSize.x * scaleCorrection, canvasSize.y * scaleCorrection, 5f); 
        bgCollider.isTrigger = false;
        bgCollider.center = Vector3.zero;
        
        Debug.Log($"[UISetup] 背景パネルにコライダー追加: Size={bgCollider.size}");
        Debug.Log($"[UISetup] 背景パネル詳細 - Position: {bgPanel.transform.position}, Scale: {bgPanel.transform.lossyScale}");
        Debug.Log($"[UISetup] Canvas詳細 - Position: {transform.position}, Scale: {transform.lossyScale}");
    }
    
    void CreateButtons()
    {
        if (canvas == null) return;
        
        // ボタンコンテナ作成
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(transform);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.9f);
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.anchoredPosition3D = Vector3.zero; // Z位置も0に設定
        containerRect.localPosition = new Vector3(0, 0, 0); // ローカル位置を明示的に設定
        containerRect.localScale = Vector3.one;
        
        // GridLayoutGroup追加
        GridLayoutGroup grid = buttonContainer.AddComponent<GridLayoutGroup>();
        float buttonWidth = (containerRect.rect.width - buttonSpacing * (buttonColumns - 1)) / buttonColumns;
        float buttonHeight = (containerRect.rect.height - buttonSpacing * (buttonRows - 1)) / buttonRows;
        
        grid.cellSize = new Vector2(40, 30);
        grid.spacing = new Vector2(buttonSpacing * 30, buttonSpacing * 30);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // ボタン作成
        for (int row = 0; row < buttonRows; row++)
        {
            for (int col = 0; col < buttonColumns; col++)
            {
                int buttonIndex = row * buttonColumns + col + 1;
                CreateButton(buttonContainer.transform, $"Button {buttonIndex}", buttonIndex);
            }
        }
    }
    
    void CreateButton(Transform parent, string buttonText, int index)
    {
        // ボタンGameObject作成
        GameObject buttonGO = new GameObject(buttonText);
        buttonGO.transform.SetParent(parent);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.localScale = Vector3.one;
        buttonRect.anchoredPosition3D = Vector3.zero; // Z位置を0に設定
        
        // ボタンコンポーネント追加
        Button button = buttonGO.AddComponent<Button>();
        
        // ボタン背景画像
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
        
        // テキスト作成
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.anchoredPosition3D = Vector3.zero; // Z位置を0に設定
        textRect.localPosition = Vector3.zero; // ローカル位置を明示的に設定
        textRect.localScale = Vector3.one;
        
        // TextMeshPro使用（利用可能な場合）
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
            // 通常のTextコンポーネントにフォールバック
            Text text = textGO.AddComponent<Text>();
            text.text = buttonText;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }
        
        // ボタンクリックイベント設定
        button.onClick.AddListener(() => OnButtonClick(index));
        
        // ボタンのカラー設定
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
        button.colors = colors;
        
        // VRレーザーポインター用のコライダーを追加
        BoxCollider buttonCollider = buttonGO.AddComponent<BoxCollider>();
        // UI要素のサイズをそのまま使用（Canvasが既にスケール適用済み）
        buttonCollider.size = new Vector3(40f, 30f, 0.05f);
        buttonCollider.isTrigger = false; // Raycast検出用
        buttonCollider.center = Vector3.zero;
        
        Debug.Log($"[UISetup] ボタンにコライダー追加: {buttonText}, WorldSize: {buttonCollider.size}");
    }
    
    void OnButtonClick(int buttonIndex)
    {
        Debug.Log($"ボタン {buttonIndex} がクリックされました！");
        
        if (panoramaManager == null)
        {
            Debug.LogWarning("[UISetup] PanoramaSkyboxManagerが無効です");
            return;
        }
        
        // ボタン機能の割り当て
        switch (buttonIndex)
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
                panoramaManager.ShowPanoramaVideo(0);
                Debug.Log("パノラマ動画1を再生");
                break;
            case 4:
                panoramaManager.NextImage();
                Debug.Log("次の画像に切り替え");
                break;
            case 5:
                panoramaManager.ToggleVideoPause();
                Debug.Log("動画の一時停止/再生");
                break;
            case 6:
                panoramaManager.SetDefaultSkybox();
                Debug.Log("デフォルトSkyboxに戻す");
                break;
            default:
                Debug.Log($"未定義のボタン: {buttonIndex}");
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
}