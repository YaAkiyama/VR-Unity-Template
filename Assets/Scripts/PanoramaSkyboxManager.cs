using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// パノラマ画像・動画をSkyboxとして表示・管理するマネージャー
/// Skybox/Panoramic シェーダーを使用して360度パノラマを表示
/// </summary>
public class PanoramaSkyboxManager : MonoBehaviour
{
    [Header("パノラマ画像設定")]
    [SerializeField] private Texture2D[] panoramaImages;
    [SerializeField] private string[] imageNames;
    
    [Header("パノラマ動画設定")]
    [SerializeField] private VideoClip[] panoramaVideos;
    [SerializeField] private string[] videoNames;
    
    [Header("システム設定")]
    [SerializeField] private bool useDefaultSkybox = false;
    [SerializeField] private bool showFirstImageOnStart = true;
    
    [Header("マテリアルテンプレート（オプション）")]
    [SerializeField] private Material skyboxMaterialTemplate;
    
    // 内部変数
    private Material currentSkyboxMaterial;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private Camera mainCamera;
    
    // 現在の状態
    private int currentImageIndex = -1;
    private int currentVideoIndex = -1;
    private bool isPlayingVideo = false;
    
    void Start()
    {
        SetupComponents();
        
        if (useDefaultSkybox)
        {
            SetDefaultSkybox();
        }
        else if (showFirstImageOnStart && panoramaImages != null && panoramaImages.Length > 0)
        {
            // アプリ開始時に最初のパノラマ画像を表示
            ShowPanoramaImage(0);
        }
    }
    
    void SetupComponents()
    {
        // メインカメラの取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // VideoPlayerコンポーネントの作成
        GameObject videoPlayerGO = new GameObject("PanoramaVideoPlayer");
        videoPlayerGO.transform.SetParent(transform);
        videoPlayer = videoPlayerGO.AddComponent<VideoPlayer>();
        
        // VideoPlayer設定
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        
        // AudioSource追加（動画音声用）
        AudioSource audioSource = videoPlayerGO.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSource);
        
        Debug.Log("[PanoramaSkyboxManager] セットアップ完了");
    }
    
    /// <summary>
    /// デフォルトSkyboxに戻す
    /// </summary>
    public void SetDefaultSkybox()
    {
        StopVideo();
        RenderSettings.skybox = null;
        DynamicGI.UpdateEnvironment();
        
        currentImageIndex = -1;
        currentVideoIndex = -1;
        isPlayingVideo = false;
        
        Debug.Log("[PanoramaSkyboxManager] デフォルトSkyboxに設定");
    }
    
    /// <summary>
    /// パノラマ画像を表示
    /// </summary>
    /// <param name="index">画像インデックス</param>
    public void ShowPanoramaImage(int index)
    {
        if (panoramaImages == null || index < 0 || index >= panoramaImages.Length)
        {
            Debug.LogWarning($"[PanoramaSkyboxManager] 無効な画像インデックス: {index}");
            return;
        }
        
        if (panoramaImages[index] == null)
        {
            Debug.LogWarning($"[PanoramaSkyboxManager] 画像が未設定: index {index}");
            return;
        }
        
        // 実機デバッグ用：画像情報をログ出力
        Texture2D texture = panoramaImages[index];
        Debug.Log($"[PanoramaSkyboxManager] 画像情報 - Name: {texture.name}, Size: {texture.width}x{texture.height}, Format: {texture.format}, isReadable: {texture.isReadable}");
        
        StopVideo();
        
        // Skybox用マテリアルを作成
        Material skyboxMat = null;
        
        // 優先順位1: テンプレートマテリアルを使用
        if (skyboxMaterialTemplate != null)
        {
            Debug.Log("[PanoramaSkyboxManager] テンプレートマテリアルを使用");
            skyboxMat = new Material(skyboxMaterialTemplate);
            skyboxMat.SetTexture("_MainTex", panoramaImages[index]);
        }
        else
        {
            // 優先順位2: 複数のシェーダーを試行
            Shader panoramicShader = Shader.Find("Skybox/Panoramic");
            
            if (panoramicShader != null)
            {
                Debug.Log($"[PanoramaSkyboxManager] Skybox/Panoramic見つかりました");
                skyboxMat = new Material(panoramicShader);
                skyboxMat.SetTexture("_MainTex", panoramaImages[index]);
                skyboxMat.SetFloat("_Mapping", 6f); // 6 = Latitude Longitude Layout
                skyboxMat.SetFloat("_ImageType", 0f); // 0 = 360 Degrees
                skyboxMat.SetFloat("_Exposure", 1.3f);
                skyboxMat.SetFloat("_Rotation", 0f);
            }
            else
            {
                // フォールバック：Skybox/Cubemapシェーダーを試行
                Shader cubemapShader = Shader.Find("Skybox/Cubemap");
                if (cubemapShader != null)
                {
                    Debug.LogWarning("[PanoramaSkyboxManager] Skybox/Panoramic不可、Skybox/Cubemapを使用");
                    skyboxMat = new Material(cubemapShader);
                    skyboxMat.SetTexture("_Tex", panoramaImages[index]);
                    skyboxMat.SetFloat("_Exposure", 1.3f);
                    skyboxMat.SetFloat("_Rotation", 0f);
                }
                else
                {
                    Debug.LogError("[PanoramaSkyboxManager] Skyboxシェーダーが見つかりません");
                    return;
                }
            }
        }
        
        // Skyboxに設定
        RenderSettings.skybox = skyboxMat;
        DynamicGI.UpdateEnvironment();
        
        currentSkyboxMaterial = skyboxMat;
        currentImageIndex = index;
        currentVideoIndex = -1;
        isPlayingVideo = false;
        
        string imageName = (imageNames != null && index < imageNames.Length) ? 
            imageNames[index] : $"画像{index + 1}";
        
        Debug.Log($"[PanoramaSkyboxManager] パノラマ画像を表示: {imageName}");
    }
    
    /// <summary>
    /// パノラマ動画を表示
    /// </summary>
    /// <param name="index">動画インデックス</param>
    public void ShowPanoramaVideo(int index)
    {
        if (panoramaVideos == null || index < 0 || index >= panoramaVideos.Length)
        {
            Debug.LogWarning($"[PanoramaSkyboxManager] 無効な動画インデックス: {index}");
            return;
        }
        
        if (panoramaVideos[index] == null)
        {
            Debug.LogWarning($"[PanoramaSkyboxManager] 動画が未設定: index {index}");
            return;
        }
        
        StartCoroutine(LoadAndPlayVideo(index));
    }
    
    IEnumerator LoadAndPlayVideo(int index)
    {
        VideoClip videoClip = panoramaVideos[index];
        
        // RenderTextureのサイズを動画に合わせて作成
        int width = Mathf.Max((int)videoClip.width, 1920);
        int height = Mathf.Max((int)videoClip.height, 1080);
        
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            DestroyImmediate(videoRenderTexture);
        }
        
        videoRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        videoRenderTexture.Create();
        
        // VideoPlayerの設定
        videoPlayer.clip = videoClip;
        videoPlayer.targetTexture = videoRenderTexture;
        
        // 動画の準備
        videoPlayer.Prepare();
        
        // 準備完了まで待機
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        // Skybox用マテリアルを作成
        Material skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMat.SetTexture("_MainTex", videoRenderTexture);
        skyboxMat.SetFloat("_Mapping", 6f); // Latitude Longitude Layout
        skyboxMat.SetFloat("_ImageType", 0f); // 360 Degrees
        skyboxMat.SetFloat("_Exposure", 1.3f);
        skyboxMat.SetFloat("_Rotation", 0f);
        
        // Skyboxに設定
        RenderSettings.skybox = skyboxMat;
        DynamicGI.UpdateEnvironment();
        
        // 動画再生開始
        videoPlayer.Play();
        
        currentSkyboxMaterial = skyboxMat;
        currentImageIndex = -1;
        currentVideoIndex = index;
        isPlayingVideo = true;
        
        string videoName = (videoNames != null && index < videoNames.Length) ? 
            videoNames[index] : $"動画{index + 1}";
        
        Debug.Log($"[PanoramaSkyboxManager] パノラマ動画を再生: {videoName}");
    }
    
    /// <summary>
    /// 動画再生を停止
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        isPlayingVideo = false;
    }
    
    /// <summary>
    /// 動画の一時停止/再開
    /// </summary>
    public void ToggleVideoPause()
    {
        if (!isPlayingVideo || videoPlayer == null) return;
        
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("[PanoramaSkyboxManager] 動画を一時停止");
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("[PanoramaSkyboxManager] 動画再生を再開");
        }
    }
    
    /// <summary>
    /// 次のパノラマ画像を表示
    /// </summary>
    public void NextImage()
    {
        if (panoramaImages == null || panoramaImages.Length == 0) return;
        
        int nextIndex = (currentImageIndex + 1) % panoramaImages.Length;
        ShowPanoramaImage(nextIndex);
    }
    
    /// <summary>
    /// 前のパノラマ画像を表示
    /// </summary>
    public void PreviousImage()
    {
        if (panoramaImages == null || panoramaImages.Length == 0) return;
        
        int prevIndex = currentImageIndex <= 0 ? 
            panoramaImages.Length - 1 : currentImageIndex - 1;
        ShowPanoramaImage(prevIndex);
    }
    
    /// <summary>
    /// 次のパノラマ動画を表示
    /// </summary>
    public void NextVideo()
    {
        if (panoramaVideos == null || panoramaVideos.Length == 0) return;
        
        int nextIndex = (currentVideoIndex + 1) % panoramaVideos.Length;
        ShowPanoramaVideo(nextIndex);
    }
    
    /// <summary>
    /// 前のパノラマ動画を表示
    /// </summary>
    public void PreviousVideo()
    {
        if (panoramaVideos == null || panoramaVideos.Length == 0) return;
        
        int prevIndex = currentVideoIndex <= 0 ? 
            panoramaVideos.Length - 1 : currentVideoIndex - 1;
        ShowPanoramaVideo(prevIndex);
    }
    
    /// <summary>
    /// Skyboxの回転を設定
    /// </summary>
    /// <param name="rotation">回転角度（度）</param>
    public void SetSkyboxRotation(float rotation)
    {
        if (currentSkyboxMaterial != null)
        {
            currentSkyboxMaterial.SetFloat("_Rotation", rotation);
        }
    }
    
    /// <summary>
    /// Skyboxの露出（明るさ）を設定
    /// </summary>
    /// <param name="exposure">露出値</param>
    public void SetSkyboxExposure(float exposure)
    {
        if (currentSkyboxMaterial != null)
        {
            currentSkyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }
    
    /// <summary>
    /// 現在の状態を取得
    /// </summary>
    public string GetCurrentStatus()
    {
        if (isPlayingVideo && currentVideoIndex >= 0)
        {
            string videoName = (videoNames != null && currentVideoIndex < videoNames.Length) ? 
                videoNames[currentVideoIndex] : $"動画{currentVideoIndex + 1}";
            return $"再生中: {videoName}";
        }
        else if (currentImageIndex >= 0)
        {
            string imageName = (imageNames != null && currentImageIndex < imageNames.Length) ? 
                imageNames[currentImageIndex] : $"画像{currentImageIndex + 1}";
            return $"表示中: {imageName}";
        }
        else
        {
            return "デフォルトSkybox";
        }
    }
    
    void Update()
    {
        // 動画の状態監視（必要に応じて）
        if (isPlayingVideo && videoPlayer != null)
        {
            if (!videoPlayer.isPlaying && !videoPlayer.isPaused)
            {
                // 動画が終了した場合の処理
                // （isLooping=trueなので通常は発生しない）
            }
        }
    }
    
    void OnDestroy()
    {
        StopVideo();
        
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            DestroyImmediate(videoRenderTexture);
        }
        
        if (currentSkyboxMaterial != null)
        {
            DestroyImmediate(currentSkyboxMaterial);
        }
    }
    
    // インスペクターから呼び出し可能
    [ContextMenu("Show First Image")]
    public void ShowFirstImage()
    {
        ShowPanoramaImage(0);
    }
    
    [ContextMenu("Show First Video")]
    public void ShowFirstVideo()
    {
        ShowPanoramaVideo(0);
    }
    
    [ContextMenu("Reset to Default")]
    public void ResetToDefault()
    {
        SetDefaultSkybox();
    }
}