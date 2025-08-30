using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// シンプルなファイルアクセステスト
/// 各種フォルダへの直接アクセスを試みる
/// </summary>
public static class SimpleFileAccess
{
    /// <summary>
    /// Meta Quest/Androidの標準ディレクトリパスを取得
    /// </summary>
    public static class StandardPaths
    {
        // Meta Quest 3で一般的なパス
        public static string InternalStorage => "/storage/emulated/0";
        public static string Downloads => "/storage/emulated/0/Download";
        public static string Pictures => "/storage/emulated/0/Pictures";
        public static string Documents => "/storage/emulated/0/Documents";
        public static string Movies => "/storage/emulated/0/Movies";
        public static string Music => "/storage/emulated/0/Music";
        public static string DCIM => "/storage/emulated/0/DCIM";
        
        // Oculusアプリ専用フォルダ
        public static string OculusFolder => "/storage/emulated/0/Oculus";
        public static string OculusScreenshots => "/storage/emulated/0/Oculus/Screenshots";
        public static string OculusVideoShots => "/storage/emulated/0/Oculus/VideoShots";
    }
    
    /// <summary>
    /// 指定パスのファイル一覧を取得（シンプル版）
    /// </summary>
    public static string[] GetFilesSimple(string path)
    {
        List<string> files = new List<string>();
        
        Debug.Log($"[SimpleFileAccess] パスをチェック: {path}");
        
        // ディレクトリの存在確認
        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"[SimpleFileAccess] ディレクトリが存在しません: {path}");
            return files.ToArray();
        }
        
        try
        {
            // ファイル一覧取得を試みる
            string[] allFiles = Directory.GetFiles(path);
            Debug.Log($"[SimpleFileAccess] {allFiles.Length}個のファイルを発見");
            
            foreach (string file in allFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName) && !fileName.StartsWith("."))
                {
                    files.Add(fileName);
                    Debug.Log($"[SimpleFileAccess] ファイル: {fileName}");
                }
            }
        }
        catch (System.UnauthorizedAccessException e)
        {
            Debug.LogError($"[SimpleFileAccess] アクセス拒否: {path} - {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleFileAccess] エラー: {e.Message}");
        }
        
        return files.ToArray();
    }
    
    /// <summary>
    /// 利用可能なパスを探索
    /// </summary>
    public static void DiscoverAvailablePaths()
    {
        Debug.Log("[SimpleFileAccess] === 利用可能なパスを探索 ===");
        
        // アプリ固有のパス
        Debug.Log($"[SimpleFileAccess] persistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"[SimpleFileAccess] temporaryCachePath: {Application.temporaryCachePath}");
        Debug.Log($"[SimpleFileAccess] dataPath: {Application.dataPath}");
        
        // 標準フォルダの存在確認
        string[] testPaths = {
            StandardPaths.InternalStorage,
            StandardPaths.Downloads,
            StandardPaths.Pictures,
            StandardPaths.Documents,
            StandardPaths.Movies,
            StandardPaths.Music,
            StandardPaths.DCIM,
            StandardPaths.OculusFolder,
            StandardPaths.OculusScreenshots,
            StandardPaths.OculusVideoShots,
            "/sdcard",
            "/sdcard/Download",
            "/sdcard/Android/data",
            $"/sdcard/Android/data/{Application.identifier}/files",
            "/android_asset",
            "/data/data/" + Application.identifier,
            Application.persistentDataPath + "/../"
        };
        
        foreach (string path in testPaths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Debug.Log($"[SimpleFileAccess] ✓ 存在: {path}");
                    
                    // ファイル数を確認
                    try
                    {
                        string[] files = Directory.GetFiles(path);
                        string[] dirs = Directory.GetDirectories(path);
                        Debug.Log($"[SimpleFileAccess]   → ファイル: {files.Length}個, フォルダ: {dirs.Length}個");
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"[SimpleFileAccess]   → アクセス不可: {e.GetType().Name}");
                    }
                }
                else
                {
                    Debug.Log($"[SimpleFileAccess] ✗ 存在しない: {path}");
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"[SimpleFileAccess] ✗ エラー: {path} - {e.GetType().Name}");
            }
        }
        
        Debug.Log("[SimpleFileAccess] === 探索完了 ===");
    }
}