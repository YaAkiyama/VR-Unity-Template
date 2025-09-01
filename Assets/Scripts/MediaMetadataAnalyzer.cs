using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

/// <summary>
/// メディアファイルのメタデータを解析して360度コンテンツかどうかを判定
/// </summary>
public class MediaMetadataAnalyzer : MonoBehaviour
{
    private static MediaMetadataAnalyzer instance;
    public static MediaMetadataAnalyzer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MediaMetadataAnalyzer");
                instance = go.AddComponent<MediaMetadataAnalyzer>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    /// <summary>
    /// パノラマコンテンツ判定結果
    /// </summary>
    public class PanoramaCheckResult
    {
        public bool IsPanorama { get; set; }
        public string Reason { get; set; }
        public PanoramaType Type { get; set; }
        public float AspectRatio { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    
    public enum PanoramaType
    {
        None,
        Equirectangular,  // 正距円筒図法（2:1の比率）
        Spherical,        // 球面パノラマ
        Cubemap,          // キューブマップ（6:1または3:2）
        Unknown
    }
    
    /// <summary>
    /// 画像ファイルがパノラマかどうかをメタデータから判定
    /// </summary>
    public IEnumerator CheckImagePanorama(string filePath, System.Action<PanoramaCheckResult> callback)
    {
        PanoramaCheckResult result = new PanoramaCheckResult();
        
        // ファイルパスをURL形式に変換
        string url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MetadataAnalyzer] 画像読み込みエラー: {www.error}");
                result.IsPanorama = false;
                result.Reason = "画像読み込みエラー";
                callback?.Invoke(result);
                yield break;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            
            // 画像のサイズとアスペクト比を取得
            result.Width = texture.width;
            result.Height = texture.height;
            result.AspectRatio = (float)texture.width / texture.height;
            
            Debug.Log($"[MetadataAnalyzer] 画像解析 - サイズ: {result.Width}x{result.Height}, アスペクト比: {result.AspectRatio}");
            
            // パノラマ判定ロジック
            result = AnalyzeImageDimensions(result);
            
            // EXIFメタデータチェック（可能な場合）
            CheckEXIFMetadata(filePath, result);
            
            callback?.Invoke(result);
        }
    }
    
    /// <summary>
    /// 動画ファイルがパノラマかどうかをメタデータから判定
    /// </summary>
    public IEnumerator CheckVideoPanorama(string filePath, System.Action<PanoramaCheckResult> callback)
    {
        PanoramaCheckResult result = new PanoramaCheckResult();
        
        // VideoPlayerを一時的に作成してメタデータを取得
        GameObject tempGO = new GameObject("TempVideoPlayer");
        VideoPlayer vp = tempGO.AddComponent<VideoPlayer>();
        
        vp.source = VideoSource.Url;
        vp.url = "file:///" + filePath.Replace('\\', '/').Replace(" ", "%20");
        vp.playOnAwake = false;
        
        // 動画の準備完了を待つ
        bool isPrepared = false;
        vp.prepareCompleted += (VideoPlayer source) =>
        {
            isPrepared = true;
        };
        
        vp.errorReceived += (VideoPlayer source, string message) =>
        {
            Debug.LogError($"[MetadataAnalyzer] 動画読み込みエラー: {message}");
            result.IsPanorama = false;
            result.Reason = "動画読み込みエラー";
            isPrepared = true;
        };
        
        vp.Prepare();
        
        // 準備完了を待つ（タイムアウト5秒）
        float timeout = 5f;
        float elapsed = 0f;
        while (!isPrepared && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (isPrepared && vp.isPrepared)
        {
            // 動画のメタデータを取得
            result.Width = (int)vp.width;
            result.Height = (int)vp.height;
            result.AspectRatio = (float)vp.width / vp.height;
            
            Debug.Log($"[MetadataAnalyzer] 動画解析 - サイズ: {result.Width}x{result.Height}, アスペクト比: {result.AspectRatio}");
            Debug.Log($"[MetadataAnalyzer] フレームレート: {vp.frameRate}, 長さ: {vp.length}秒");
            
            // パノラマ判定ロジック
            result = AnalyzeVideoDimensions(result);
            
            // MP4メタデータチェック（spherical-video-v2など）
            CheckMP4Metadata(filePath, result);
        }
        else
        {
            result.IsPanorama = false;
            result.Reason = "動画メタデータ取得タイムアウト";
        }
        
        // クリーンアップ
        Destroy(tempGO);
        
        callback?.Invoke(result);
    }
    
    /// <summary>
    /// 画像の寸法からパノラマタイプを判定
    /// </summary>
    private PanoramaCheckResult AnalyzeImageDimensions(PanoramaCheckResult result)
    {
        // 最小解像度チェック - 小さすぎる画像はパノラマではない
        if (result.Width < 2000 || result.Height < 1000)
        {
            result.IsPanorama = false;
            result.Type = PanoramaType.None;
            result.Reason = "解像度が低すぎる（パノラマではない）";
            return result;
        }
        
        // 正距円筒図法（Equirectangular）の判定
        // 標準的な360度パノラマは2:1のアスペクト比
        if (Math.Abs(result.AspectRatio - 2.0f) < 0.1f)
        {
            result.IsPanorama = true;
            result.Type = PanoramaType.Equirectangular;
            result.Reason = "2:1アスペクト比（正距円筒図法）";
            return result;
        }
        
        // キューブマップの判定（6:1）
        if (Math.Abs(result.AspectRatio - 6.0f) < 0.1f)
        {
            result.IsPanorama = true;
            result.Type = PanoramaType.Cubemap;
            result.Reason = "6:1アスペクト比（キューブマップ横並び）";
            return result;
        }
        
        // 3:2アスペクト比のキューブマップ判定（より厳密な条件）
        // キューブマップは通常高解像度で、幅が3の倍数、高さが2の倍数
        if (Math.Abs(result.AspectRatio - 1.5f) < 0.05f && 
            result.Width >= 3000 && // 最小幅3000px（キューブマップは高解像度）
            result.Width % 3 == 0 && // 幅が3の倍数
            result.Height % 2 == 0)   // 高さが2の倍数
        {
            result.IsPanorama = true;
            result.Type = PanoramaType.Cubemap;
            result.Reason = "3:2アスペクト比（高解像度キューブマップ3x2配置）";
            return result;
        }
        
        // 大きな横幅を持つ画像（水平パノラマ写真の可能性）
        // ただし、より厳密な条件で判定
        if (result.AspectRatio > 4.0f && result.Width > 5000)
        {
            result.IsPanorama = true;
            result.Type = PanoramaType.Spherical;
            result.Reason = "超広角アスペクト比（水平パノラマ）";
            return result;
        }
        
        // 一般的な360度画像の解像度パターン
        int[][] common360Resolutions = new int[][]
        {
            new int[] {4096, 2048},  // 4K 360
            new int[] {3840, 1920},  // 別の4K 360
            new int[] {5760, 2880},  // 高解像度360
            new int[] {8192, 4096},  // 8K 360
            new int[] {2048, 1024},  // 2K 360
        };
        
        foreach (var res in common360Resolutions)
        {
            if (result.Width == res[0] && result.Height == res[1])
            {
                result.IsPanorama = true;
                result.Type = PanoramaType.Equirectangular;
                result.Reason = $"一般的な360度解像度 ({res[0]}x{res[1]})";
                return result;
            }
        }
        
        result.IsPanorama = false;
        result.Type = PanoramaType.None;
        result.Reason = "通常のアスペクト比";
        return result;
    }
    
    /// <summary>
    /// 動画の寸法からパノラマタイプを判定
    /// </summary>
    private PanoramaCheckResult AnalyzeVideoDimensions(PanoramaCheckResult result)
    {
        // 動画も基本的に画像と同じ判定ロジックを使用
        return AnalyzeImageDimensions(result);
    }
    
    /// <summary>
    /// EXIFメタデータから360度情報をチェック
    /// </summary>
    private void CheckEXIFMetadata(string filePath, PanoramaCheckResult result)
    {
        // 注：UnityではEXIFデータの直接読み取りは標準サポートされていないため、
        // 実装には外部ライブラリ（ExifLib等）が必要
        // ここではプレースホルダーとして実装
        
        // XMP メタデータに ProjectionType="equirectangular" があるかチェック
        // GPanoデータ（Google Photo Sphere XMP）のチェック
        
        Debug.Log("[MetadataAnalyzer] EXIF/XMPメタデータチェック（未実装）");
    }
    
    /// <summary>
    /// MP4メタデータから360度情報をチェック
    /// </summary>
    private void CheckMP4Metadata(string filePath, PanoramaCheckResult result)
    {
        // spherical-video-v2 メタデータのチェック
        // YouTube 360度動画規格のメタデータ確認
        
        try
        {
            // MP4ファイルのバイナリを部分的に読み取って確認
            byte[] buffer = new byte[1024];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // MP4 ボックス構造を解析
                // "uuid" ボックスで spherical メタデータを探す
                // ここでは簡易的なチェックのみ
                
                fs.Read(buffer, 0, buffer.Length);
                string data = System.Text.Encoding.ASCII.GetString(buffer);
                
                if (data.Contains("spherical") || data.Contains("st3d"))
                {
                    result.IsPanorama = true;
                    result.Type = PanoramaType.Spherical;
                    result.Reason += " + Sphericalメタデータ検出";
                    Debug.Log("[MetadataAnalyzer] Sphericalメタデータを検出");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MetadataAnalyzer] MP4メタデータ読み取りエラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// 汎用的なパノラマチェック（画像・動画自動判定）
    /// </summary>
    public void CheckIfPanorama(string filePath, System.Action<PanoramaCheckResult> callback)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        
        // 動画ファイルの拡張子
        string[] videoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v" };
        
        if (System.Array.Exists(videoExtensions, ext => ext == extension))
        {
            StartCoroutine(CheckVideoPanorama(filePath, callback));
        }
        else
        {
            StartCoroutine(CheckImagePanorama(filePath, callback));
        }
    }
}