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
    
    // 常時表示メディアパネルの参照
    public GameObject permanentMediaPanel;
    
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
    /// 常時表示メディアパネルの設定
    /// </summary>
    public void SetMediaPanel(GameObject panel)
    {
        permanentMediaPanel = panel;
        Debug.Log("[MediaViewer] 常時表示メディアパネルが設定されました");
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
        bool isVideo = IsVideoFile(extension);
        bool isImage = IsImageFile(extension);
        
        if (!isVideo && !isImage)
        {
            Debug.LogWarning($"[MediaViewer] サポートされていないファイル形式: {extension}");
            return;
        }
        
        // メタデータ解析でパノラマ判定
        UpdateStatus("メタデータを解析中...");
        MediaMetadataAnalyzer.Instance.CheckIfPanorama(filePath, (result) =>
        {
            Debug.Log($"[MediaViewer] メタデータ解析結果 - パノラマ: {result.IsPanorama}");
            Debug.Log($"[MediaViewer] 判定理由: {result.Reason}");
            Debug.Log($"[MediaViewer] 解像度: {result.Width}x{result.Height}, アスペクト比: {result.AspectRatio:F2}");
            
            // 解析結果に基づいて適切な表示方法を選択
            if (result.IsPanorama)
            {
                Debug.Log($"[MediaViewer] パノラマコンテンツとして表示: {fileName} (タイプ: {result.Type})");
                if (isVideo)
                {
                    DisplayPanoramaVideo(filePath, targetPanel);
                }
                else if (isImage)
                {
                    DisplayPanoramaImage(filePath, targetPanel);
                }
            }
            else
            {
                Debug.Log($"[MediaViewer] 通常メディアとして表示: {fileName}");
                if (isVideo)
                {
                    DisplayRegularVideo(filePath);
                }
                else if (isImage)
                {
                    DisplayRegularImage(filePath);
                }
            }
        });
    }
    
    /// <summary>
    /// パノラマコンテンツかどうかを判定（廃止予定 - メタデータ判定を使用）
    /// </summary>
    [System.Obsolete("メタデータベースの判定（MediaMetadataAnalyzer）を使用してください")]
    private bool IsPanoramaContent(string fileName)
    {
        string lowerName = fileName.ToLower();
        
        // パノラマを示すキーワードをチェック（後方互換性のため残す）
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
    /// パノラマ画像をSkyboxとして表示
    /// </summary>
    private void DisplayPanoramaImage(string filePath, GameObject targetPanel)
    {
        StartCoroutine(LoadPanoramaImageForSkyboxCoroutine(filePath));
    }
    
    private IEnumerator LoadPanoramaImageForSkyboxCoroutine(string filePath)
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
            
            // テクスチャを取得してSkyboxに設定
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            Debug.Log($"[MediaViewer] パノラマ画像をSkyboxに設定: {Path.GetFileName(filePath)}");
            
            // Skybox用マテリアルを動的に作成してSkyboxに適用
            SetPanoramaSkybox(texture);
            
            UpdateStatus("パノラマ画像をSkyboxに表示中");
            Debug.Log($"[MediaViewer] パノラマ画像Skybox表示完了: {Path.GetFileName(filePath)}");
        }
    }
    
    /// <summary>
    /// パノラマ動画をSkyboxとして表示
    /// </summary>
    private void DisplayPanoramaVideo(string filePath, GameObject targetPanel)
    {
        UpdateStatus("パノラマ動画を準備中...");
        
        // 既存のVideoPlayerを停止（あれば）
        StopAllVideoPlayers();
        
        // 新しいVideoPlayerオブジェクトを作成
        GameObject videoPlayerObj = new GameObject("PanoramaVideoPlayer");
        VideoPlayer vp = videoPlayerObj.AddComponent<VideoPlayer>();
        
        // RenderTextureを作成（動画用）
        RenderTexture renderTexture = new RenderTexture(2048, 1024, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        
        // VideoPlayer設定
        vp.source = VideoSource.Url;
        vp.url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = renderTexture;
        vp.isLooping = true;
        vp.playOnAwake = false;
        
        // 動画準備完了時のコールバック
        vp.prepareCompleted += (VideoPlayer source) =>
        {
            Debug.Log("[MediaViewer] パノラマ動画準備完了、Skyboxに設定");
            SetPanoramaSkybox(renderTexture);
            source.Play();
            UpdateStatus("パノラマ動画をSkyboxで再生中");
        };
        
        // 動画準備開始
        vp.Prepare();
        
        Debug.Log($"[MediaViewer] パノラマ動画準備開始: {Path.GetFileName(filePath)}");
    }
    
    /// <summary>
    /// 全てのVideoPlayerを停止
    /// </summary>
    private void StopAllVideoPlayers()
    {
        VideoPlayer[] players = FindObjectsOfType<VideoPlayer>();
        foreach (VideoPlayer player in players)
        {
            if (player != null && player.gameObject.name == "PanoramaVideoPlayer")
            {
                player.Stop();
                if (player.targetTexture != null)
                {
                    player.targetTexture.Release();
                }
                Destroy(player.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 通常の画像を表示（常時表示パネルに表示）
    /// </summary>
    private void DisplayRegularImage(string filePath)
    {
        StartCoroutine(LoadRegularImageCoroutine(filePath));
    }
    
    private IEnumerator LoadRegularImageCoroutine(string filePath)
    {
        UpdateStatus("画像を読み込み中...");
        
        // 常時表示パネルを使用、なければ新規作成
        GameObject panel = permanentMediaPanel != null ? permanentMediaPanel : GetOrCreateMediaPanel();
        
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
            
            // 表示エリアを探す
            Transform displayArea = panel.transform.Find("MediaDisplayArea");
            if (displayArea == null)
            {
                Debug.LogWarning("[MediaViewer] MediaDisplayAreaが見つかりません");
                yield break;
            }
            
            // 既存のプレースホルダーテキストを非表示
            Transform placeholder = displayArea.Find("PlaceholderText");
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(false);
            }
            
            // RawImageに画像を表示
            RawImage image = displayArea.GetComponentInChildren<RawImage>();
            if (image == null)
            {
                GameObject imageGO = new GameObject("MediaImage");
                imageGO.transform.SetParent(displayArea);
                image = imageGO.AddComponent<RawImage>();
                
                RectTransform rt = image.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.localPosition = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
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
        
        // 常時表示パネルを使用、なければ新規作成
        GameObject panel = permanentMediaPanel != null ? permanentMediaPanel : GetOrCreateMediaPanel();
        
        // 表示エリアを探す
        Transform displayArea = panel.transform.Find("MediaDisplayArea");
        if (displayArea == null)
        {
            Debug.LogWarning("[MediaViewer] MediaDisplayAreaが見つかりません");
            return;
        }
        
        // 既存のプレースホルダーテキストを非表示
        Transform placeholder = displayArea.Find("PlaceholderText");
        if (placeholder != null)
        {
            placeholder.gameObject.SetActive(false);
        }
        
        // VideoPlayerコンポーネントを追加
        VideoPlayer vp = displayArea.GetComponent<VideoPlayer>();
        if (vp == null)
        {
            vp = displayArea.gameObject.AddComponent<VideoPlayer>();
        }
        
        // RenderTextureを作成
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 16);
        vp.targetTexture = renderTexture;
        
        // RawImageに表示
        RawImage image = displayArea.GetComponentInChildren<RawImage>();
        if (image == null)
        {
            GameObject imageGO = new GameObject("VideoDisplay");
            imageGO.transform.SetParent(displayArea);
            image = imageGO.AddComponent<RawImage>();
            
            RectTransform rt = image.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
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
        // 常時表示パネルのタイトルを更新
        if (permanentMediaPanel != null)
        {
            Transform titleBar = permanentMediaPanel.transform.Find("MediaTitleBar");
            if (titleBar != null)
            {
                Transform titleTextTransform = titleBar.Find("TitleText");
                if (titleTextTransform != null)
                {
                    TextMeshProUGUI titleTextComp = titleTextTransform.GetComponent<TextMeshProUGUI>();
                    if (titleTextComp != null)
                    {
                        titleTextComp.text = title;
                    }
                }
            }
        }
        
        // 従来のtitleTextも更新（互換性維持）
        if (titleText != null)
        {
            titleText.text = title;
        }
    }
    
    /// <summary>
    /// パノラマテクスチャをSkyboxとして設定（画像用）
    /// </summary>
    private void SetPanoramaSkybox(Texture2D texture)
    {
        // Skybox/Panoramicシェーダーを使用してマテリアルを作成
        Shader panoramicShader = Shader.Find("Skybox/Panoramic");
        if (panoramicShader == null)
        {
            Debug.LogError("[MediaViewer] Skybox/Panoramicシェーダーが見つかりません");
            return;
        }
        
        Material skyboxMaterial = new Material(panoramicShader);
        skyboxMaterial.SetTexture("_MainTex", texture);
        skyboxMaterial.SetFloat("_Mapping", 6f); // Latitude Longitude Layout
        skyboxMaterial.SetFloat("_ImageType", 0f); // 360 Degrees
        skyboxMaterial.SetFloat("_Exposure", 1.3f);
        
        // RenderSettingsのSkyboxを更新
        RenderSettings.skybox = skyboxMaterial;
        
        Debug.Log("[MediaViewer] パノラマ画像をSkyboxとして設定完了");
    }
    
    /// <summary>
    /// パノラマテクスチャをSkyboxとして設定（RenderTexture用）
    /// </summary>
    private void SetPanoramaSkybox(RenderTexture renderTexture)
    {
        // Skybox/Panoramicシェーダーを使用してマテリアルを作成
        Shader panoramicShader = Shader.Find("Skybox/Panoramic");
        if (panoramicShader == null)
        {
            Debug.LogError("[MediaViewer] Skybox/Panoramicシェーダーが見つかりません");
            return;
        }
        
        Material skyboxMaterial = new Material(panoramicShader);
        skyboxMaterial.SetTexture("_MainTex", renderTexture);
        skyboxMaterial.SetFloat("_Mapping", 6f); // Latitude Longitude Layout
        skyboxMaterial.SetFloat("_ImageType", 0f); // 360 Degrees
        skyboxMaterial.SetFloat("_Exposure", 1.3f);
        
        // RenderSettingsのSkyboxを更新
        RenderSettings.skybox = skyboxMaterial;
        
        Debug.Log("[MediaViewer] パノラマ動画をSkyboxとして設定完了");
    }
}