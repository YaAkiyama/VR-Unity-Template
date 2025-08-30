using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Android/Meta Quest向けのファイルアクセス機能
/// 外部ストレージの標準フォルダ（Downloads、Pictures等）へのアクセスを提供
/// </summary>
public static class AndroidFileAccess
{
    /// <summary>
    /// 利用可能な外部ストレージパスを取得
    /// </summary>
    public static class ExternalPaths
    {
        // Android標準フォルダへのパス
        public static string Downloads => GetAndroidPath("Download");
        public static string Pictures => GetAndroidPath("Pictures"); 
        public static string Documents => GetAndroidPath("Documents");
        public static string Movies => GetAndroidPath("Movies");
        public static string Music => GetAndroidPath("Music");
        public static string DCIM => GetAndroidPath("DCIM");
        
        /// <summary>
        /// Android標準フォルダのパスを取得
        /// </summary>
        private static string GetAndroidPath(string folderName)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                // Android実機の場合
                try
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var environment = new AndroidJavaClass("android.os.Environment"))
                    {
                        // getExternalStoragePublicDirectory を使用
                        var javaString = new AndroidJavaObject("java.lang.String", folderName);
                        var file = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", javaString);
                        
                        if (file != null)
                        {
                            string path = file.Call<string>("getAbsolutePath");
                            Debug.Log($"[AndroidFileAccess] {folderName} path: {path}");
                            return path;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AndroidFileAccess] Error getting {folderName} path: {e.Message}");
                }
            #endif
            
            // エディタや失敗時はpersistentDataPath内にフォルダを作成
            string fallbackPath = Path.Combine(Application.persistentDataPath, folderName);
            Directory.CreateDirectory(fallbackPath);
            return fallbackPath;
        }
    }
    
    /// <summary>
    /// 指定されたパスが読み取り可能かチェック
    /// </summary>
    public static bool IsPathAccessible(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"[AndroidFileAccess] Directory does not exist: {path}");
                return false;
            }
            
            // 読み取りテスト
            Directory.GetDirectories(path);
            Directory.GetFiles(path);
            
            Debug.Log($"[AndroidFileAccess] Path is accessible: {path}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidFileAccess] Path not accessible: {path}, Error: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 利用可能な全ての外部ストレージパスを取得
    /// </summary>
    public static string[] GetAvailablePaths()
    {
        var paths = new System.Collections.Generic.List<string>
        {
            Application.persistentDataPath  // アプリ専用領域（常に利用可能）
        };
        
        // 各標準フォルダをチェック
        var externalPaths = new string[]
        {
            ExternalPaths.Downloads,
            ExternalPaths.Pictures,
            ExternalPaths.Documents,
            ExternalPaths.Movies,
            ExternalPaths.Music,
            ExternalPaths.DCIM
        };
        
        foreach (string path in externalPaths)
        {
            if (IsPathAccessible(path) && !paths.Contains(path))
            {
                paths.Add(path);
            }
        }
        
        Debug.Log($"[AndroidFileAccess] Found {paths.Count} accessible paths");
        return paths.ToArray();
    }
    
    /// <summary>
    /// パスの表示用名前を取得
    /// </summary>
    public static string GetDisplayName(string path)
    {
        if (path == Application.persistentDataPath)
            return "App Storage";
        
        if (path.Contains("Download"))
            return "Downloads";
        if (path.Contains("Pictures"))
            return "Pictures";
        if (path.Contains("Documents"))
            return "Documents";
        if (path.Contains("Movies"))
            return "Movies";
        if (path.Contains("Music"))
            return "Music";
        if (path.Contains("DCIM"))
            return "Camera";
            
        // パスの最後の部分を返す
        return Path.GetFileName(path) ?? "Unknown";
    }
    
    /// <summary>
    /// MediaStore APIを使用してメディアファイルを取得
    /// Android 10以降のスコープドストレージ制限に対応
    /// </summary>
    public static System.Collections.Generic.List<string> GetMediaFiles(string folderPath)
    {
        var mediaFiles = new System.Collections.Generic.List<string>();
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = context.Call<AndroidJavaObject>("getContentResolver"))
            {
                // MediaStore.Files.getContentUri を使用
                using (var mediaStore = new AndroidJavaClass("android.provider.MediaStore$Files"))
                {
                    var uri = mediaStore.CallStatic<AndroidJavaObject>("getContentUri", "external");
                    
                    // クエリ用のプロジェクション（取得する列）
                    string[] projection = {
                        "data",           // ファイルパス
                        "display_name"    // ファイル名
                    };
                    
                    // WHERE句でフォルダパスをフィルタ
                    string selection = "data LIKE ?";
                    string[] selectionArgs = { folderPath + "/%" };
                    
                    using (var cursor = contentResolver.Call<AndroidJavaObject>("query", uri, projection, selection, selectionArgs, null))
                    {
                        if (cursor != null && cursor.Call<bool>("moveToFirst"))
                        {
                            int dataColumnIndex = cursor.Call<int>("getColumnIndex", "data");
                            int displayNameIndex = cursor.Call<int>("getColumnIndex", "display_name");
                            
                            do
                            {
                                string filePath = cursor.Call<string>("getString", dataColumnIndex);
                                string fileName = cursor.Call<string>("getString", displayNameIndex);
                                
                                // 直接そのフォルダ内のファイルのみ取得（サブフォルダは除外）
                                if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                                {
                                    string parentDir = Path.GetDirectoryName(filePath);
                                    if (parentDir == folderPath)
                                    {
                                        mediaFiles.Add(fileName);
                                        Debug.Log($"[AndroidFileAccess] MediaStoreファイル発見: {fileName}");
                                    }
                                }
                            }
                            while (cursor.Call<bool>("moveToNext"));
                        }
                        
                        if (cursor != null)
                            cursor.Call("close");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidFileAccess] MediaStore API error: {e.Message}");
        }
        #endif
        
        Debug.Log($"[AndroidFileAccess] MediaStoreで検出されたファイル数: {mediaFiles.Count}");
        return mediaFiles;
    }
    
    /// <summary>
    /// 通常のDirectory.GetFilesと組み合わせた拡張ファイル取得
    /// </summary>
    public static string[] GetFilesWithMediaStore(string directoryPath)
    {
        var allFiles = new System.Collections.Generic.HashSet<string>();
        
        try
        {
            // 通常のDirectory.GetFilesを試す
            string[] standardFiles = Directory.GetFiles(directoryPath);
            foreach (string file in standardFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    allFiles.Add(fileName);
                }
            }
            Debug.Log($"[AndroidFileAccess] Directory.GetFilesで発見: {standardFiles.Length}個");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AndroidFileAccess] Directory.GetFiles failed: {e.Message}");
        }
        
        // MediaStore APIで追加取得
        var mediaFiles = GetMediaFiles(directoryPath);
        foreach (string mediaFile in mediaFiles)
        {
            allFiles.Add(mediaFile);
        }
        
        Debug.Log($"[AndroidFileAccess] 合計発見ファイル数: {allFiles.Count}個");
        
        // HashSetを配列に変換
        string[] result = new string[allFiles.Count];
        allFiles.CopyTo(result);
        return result;
    }
}