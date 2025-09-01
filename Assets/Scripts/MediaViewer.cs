using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using TMPro;

/// <summary>
/// VRメディアビューア（画像・動画表示）
/// パノラマコンテンツと通常メディアの表示を管理
/// </summary>
public class MediaViewer : MonoBehaviour
{
    [Header("パノラマ表示設定")]
    [SerializeField] private GameObject panoramaSphere;
    [SerializeField] private Material panoramaMaterial;
    
    [Header("通常メディア表示設定")]
    [SerializeField] private GameObject mediaPanel;
    [SerializeField] private RawImage mediaImage;
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    // シングルトンインスタンス
    private static MediaViewer instance;
    public static MediaViewer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MediaViewer>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MediaViewer");
                    instance = go.AddComponent<MediaViewer>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// メディアファイルを開く
    /// </summary>
    public void OpenMediaFile(string filePath, GameObject targetPanel = null)
    {
        Debug.Log($"[MediaViewer] メディアファイルを開く: {filePath}");
        
        string fileName = Path.GetFileName(filePath);
        string extension = Path.GetExtension(filePath).ToLower();
        
        // ファイルタイプを判定
        bool isPanorama = IsPanoramaContent(fileName);
        bool isVideo = IsVideoFile(extension);
        bool isImage = IsImageFile(extension);
        
        if (isPanorama)
        {
            Debug.Log($"[MediaViewer] パノラマコンテンツとして表示: {fileName}");
            if (isVideo)
            {
                DisplayPanoramaVideo(filePath, targetPanel);
            }
            else if (isImage)
            {
                DisplayPanoramaImage(filePath, targetPanel);
            }
        }
        else if (isVideo)
        {
            Debug.Log($"[MediaViewer] 通常動画として表示: {fileName}");
            DisplayRegularVideo(filePath);
        }
        else if (isImage)
        {
            Debug.Log($"[MediaViewer] 通常画像として表示: {fileName}");
            DisplayRegularImage(filePath);
        }
        else
        {
            Debug.LogWarning($"[MediaViewer] サポートされていないファイル形式: {extension}");
        }
    }
    
    /// <summary>
    /// パノラマコンテンツかどうかを判定
    /// </summary>
    private bool IsPanoramaContent(string fileName)
    {
        string lowerName = fileName.ToLower();
        
        // パノラマを示すキーワードをチェック
        return lowerName.Contains("360") || 
               lowerName.Contains("panorama") || 
               lowerName.Contains("pano") ||
               lowerName.Contains("spherical") ||
               lowerName.Contains("equirectangular");
    }
    
    /// <summary>
    /// 動画ファイルかどうかを判定
    /// </summary>
    private bool IsVideoFile(string extension)
    {
        string[] videoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v", ".3gp", ".wmv" };
        return System.Array.Exists(videoExtensions, ext => ext == extension);
    }
    
    /// <summary>
    /// 画像ファイルかどうかを判定
    /// </summary>
    private bool IsImageFile(string extension)
    {
        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff" };
        return System.Array.Exists(imageExtensions, ext => ext == extension);
    }
    
    /// <summary>
    /// パノラマ画像を表示（左パネルに球体として表示）
    /// </summary>
    private void DisplayPanoramaImage(string filePath, GameObject targetPanel)
    {
        StartCoroutine(LoadPanoramaImageCoroutine(filePath, targetPanel));
    }
    
    private IEnumerator LoadPanoramaImageCoroutine(string filePath, GameObject targetPanel)
    {
        UpdateStatus("パノラマ画像を読み込み中...");
        
        // ファイルパスをfile://形式に変換
        string url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MediaViewer] 画像読み込みエラー: {www.error}");
                UpdateStatus($"エラー: {www.error}");
                yield break;
            }
            
            // パノラマ球体を作成または取得
            GameObject sphere = GetOrCreatePanoramaSphere(targetPanel);
            
            // テクスチャを適用
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = texture;
                // パノラマ画像は内側から見るので裏面を表示
                mat.SetInt("_Cull", 1); // Front面をカリング
                renderer.material = mat;
            }
            
            UpdateStatus("パノラマ画像表示中");
            Debug.Log($"[MediaViewer] パノラマ画像表示完了: {Path.GetFileName(filePath)}");
        }
    }
    
    /// <summary>
    /// パノラマ動画を表示
    /// </summary>
    private void DisplayPanoramaVideo(string filePath, GameObject targetPanel)
    {
        UpdateStatus("パノラマ動画を準備中...");
        
        // パノラマ球体を作成または取得
        GameObject sphere = GetOrCreatePanoramaSphere(targetPanel);
        
        // VideoPlayerコンポーネントを追加または取得
        VideoPlayer vp = sphere.GetComponent<VideoPlayer>();
        if (vp == null)
        {
            vp = sphere.AddComponent<VideoPlayer>();
        }
        
        // 動画設定
        vp.source = VideoSource.Url;
        vp.url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        vp.renderMode = VideoRenderMode.MaterialOverride;
        
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            vp.targetMaterialRenderer = renderer;
            vp.targetMaterialProperty = "_MainTex";
        }
        
        vp.isLooping = true;
        vp.playOnAwake = false;
        
        // 再生開始
        vp.Play();
        
        UpdateStatus("パノラマ動画再生中");
        Debug.Log($"[MediaViewer] パノラマ動画再生開始: {Path.GetFileName(filePath)}");
    }
    
    /// <summary>
    /// 通常の画像を表示（新規パネルに表示）
    /// </summary>
    private void DisplayRegularImage(string filePath)
    {
        StartCoroutine(LoadRegularImageCoroutine(filePath));
    }
    
    private IEnumerator LoadRegularImageCoroutine(string filePath)
    {
        UpdateStatus("画像を読み込み中...");
        
        // メディアパネルを作成または取得
        GameObject panel = GetOrCreateMediaPanel();
        
        string url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MediaViewer] 画像読み込みエラー: {www.error}");
                UpdateStatus($"エラー: {www.error}");
                yield break;
            }
            
            // RawImageに画像を表示
            RawImage image = panel.GetComponentInChildren<RawImage>();
            if (image == null)
            {
                GameObject imageGO = new GameObject("MediaImage");
                imageGO.transform.SetParent(panel.transform);
                image = imageGO.AddComponent<RawImage>();
                
                RectTransform rt = image.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f, 0.1f);
                rt.anchorMax = new Vector2(0.9f, 0.9f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            image.texture = texture;
            
            UpdateStatus("画像表示中");
            UpdateTitle(Path.GetFileName(filePath));
            Debug.Log($"[MediaViewer] 画像表示完了: {Path.GetFileName(filePath)}");
        }
    }
    
    /// <summary>
    /// 通常の動画を表示
    /// </summary>
    private void DisplayRegularVideo(string filePath)
    {
        UpdateStatus("動画を準備中...");
        
        // メディアパネルを作成または取得
        GameObject panel = GetOrCreateMediaPanel();
        
        // VideoPlayerコンポーネントを追加
        VideoPlayer vp = panel.GetComponent<VideoPlayer>();
        if (vp == null)
        {
            vp = panel.AddComponent<VideoPlayer>();
        }
        
        // RenderTextureを作成
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 16);
        vp.targetTexture = renderTexture;
        
        // RawImageに表示
        RawImage image = panel.GetComponentInChildren<RawImage>();
        if (image == null)
        {
            GameObject imageGO = new GameObject("VideoDisplay");
            imageGO.transform.SetParent(panel.transform);
            image = imageGO.AddComponent<RawImage>();
            
            RectTransform rt = image.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        
        image.texture = renderTexture;
        
        // 動画設定
        vp.source = VideoSource.Url;
        vp.url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        vp.isLooping = true;
        vp.playOnAwake = false;
        
        // 再生開始
        vp.Play();
        
        UpdateStatus("動画再生中");
        UpdateTitle(Path.GetFileName(filePath));
        Debug.Log($"[MediaViewer] 動画再生開始: {Path.GetFileName(filePath)}");
    }
    
    /// <summary>
    /// パノラマ球体を作成または取得
    /// </summary>
    private GameObject GetOrCreatePanoramaSphere(GameObject targetPanel)
    {
        // 既存の球体を探す
        Transform existingSphere = targetPanel != null ? 
            targetPanel.transform.Find("PanoramaSphere") : 
            GameObject.Find("PanoramaSphere")?.transform;
        
        if (existingSphere != null)
        {
            return existingSphere.gameObject;
        }
        
        // 新規作成
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PanoramaSphere";
        
        if (targetPanel != null)
        {
            sphere.transform.SetParent(targetPanel.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.8f; // パネルサイズに合わせて調整
        }
        else
        {
            // カメラの位置に配置
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                sphere.transform.position = mainCam.transform.position;
                sphere.transform.localScale = Vector3.one * 10f; // ユーザーを囲む大きさ
            }
        }
        
        // 球体を反転（内側から見るため）
        sphere.transform.localScale = new Vector3(
            -Mathf.Abs(sphere.transform.localScale.x),
            sphere.transform.localScale.y,
            sphere.transform.localScale.z
        );
        
        // コライダーを無効化（視界を妨げないように）
        Collider col = sphere.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        return sphere;
    }
    
    /// <summary>
    /// メディア表示パネルを作成または取得
    /// </summary>
    private GameObject GetOrCreateMediaPanel()
    {
        // 既存のメディアパネルを探す
        GameObject existingPanel = GameObject.Find("MediaDisplayPanel");
        if (existingPanel != null)
        {
            return existingPanel;
        }
        
        // UISetupからCanvasを取得
        UISetup uiSetup = FindObjectOfType<UISetup>();
        Transform canvas = uiSetup != null ? uiSetup.transform : null;
        
        if (canvas == null)
        {
            canvas = GameObject.Find("Canvas")?.transform;
        }
        
        // 新規パネル作成
        GameObject panel = new GameObject("MediaDisplayPanel");
        if (canvas != null)
        {
            panel.transform.SetParent(canvas);
        }
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        
        // 右側に配置（ファイルエクスプローラーの反対側）
        rt.anchorMin = new Vector2(0.7f, 0.2f);
        rt.anchorMax = new Vector2(0.95f, 0.8f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        
        // 背景を追加
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // タイトルバーを追加
        GameObject titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(panel.transform);
        
        RectTransform titleRt = titleBar.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.9f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.sizeDelta = Vector2.zero;
        titleRt.anchoredPosition = Vector2.zero;
        
        Image titleBg = titleBar.AddComponent<Image>();
        titleBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // タイトルテキスト
        GameObject titleTextGO = new GameObject("TitleText");
        titleTextGO.transform.SetParent(titleBar.transform);
        
        RectTransform titleTextRt = titleTextGO.AddComponent<RectTransform>();
        titleTextRt.anchorMin = Vector2.zero;
        titleTextRt.anchorMax = Vector2.one;
        titleTextRt.sizeDelta = Vector2.zero;
        titleTextRt.anchoredPosition = Vector2.zero;
        
        titleText = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "メディアビューア";
        titleText.fontSize = 14f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        // 閉じるボタンを追加
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(titleBar.transform);
        
        RectTransform closeBtnRt = closeButton.AddComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.9f, 0f);
        closeBtnRt.anchorMax = new Vector2(1f, 1f);
        closeBtnRt.sizeDelta = Vector2.zero;
        closeBtnRt.anchoredPosition = Vector2.zero;
        
        Button closeBtnComp = closeButton.AddComponent<Button>();
        Image closeBtnImg = closeButton.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        closeBtnComp.onClick.AddListener(() => {
            CloseMediaPanel();
        });
        
        // ×テキスト
        GameObject closeText = new GameObject("CloseText");
        closeText.transform.SetParent(closeButton.transform);
        
        RectTransform closeTextRt = closeText.AddComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.sizeDelta = Vector2.zero;
        closeTextRt.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI closeTextComp = closeText.AddComponent<TextMeshProUGUI>();
        closeTextComp.text = "×";
        closeTextComp.fontSize = 16f;
        closeTextComp.alignment = TextAlignmentOptions.Center;
        closeTextComp.color = Color.white;
        
        return panel;
    }
    
    /// <summary>
    /// メディアパネルを閉じる
    /// </summary>
    public void CloseMediaPanel()
    {
        GameObject panel = GameObject.Find("MediaDisplayPanel");
        if (panel != null)
        {
            // VideoPlayerを停止
            VideoPlayer vp = panel.GetComponent<VideoPlayer>();
            if (vp != null && vp.isPlaying)
            {
                vp.Stop();
            }
            
            Destroy(panel);
            Debug.Log("[MediaViewer] メディアパネルを閉じました");
        }
    }
    
    /// <summary>
    /// パノラマ表示を停止
    /// </summary>
    public void ClosePanorama()
    {
        GameObject sphere = GameObject.Find("PanoramaSphere");
        if (sphere != null)
        {
            // VideoPlayerを停止
            VideoPlayer vp = sphere.GetComponent<VideoPlayer>();
            if (vp != null && vp.isPlaying)
            {
                vp.Stop();
            }
            
            Destroy(sphere);
            Debug.Log("[MediaViewer] パノラマ表示を停止しました");
        }
    }
    
    /// <summary>
    /// ステータステキストを更新
    /// </summary>
    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log($"[MediaViewer] ステータス: {status}");
    }
    
    /// <summary>
    /// タイトルテキストを更新
    /// </summary>
    private void UpdateTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }
}