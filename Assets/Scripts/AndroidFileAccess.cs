using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        
        // Meta Quest用の追加パス（直接指定）
        string[] metaQuestPaths = new string[]
        {
            "/sdcard/Download",
            "/sdcard/Pictures",
            "/sdcard/Documents",
            "/sdcard/Movies",
            "/sdcard/Music",
            "/sdcard/DCIM",
            "/sdcard/Oculus",
            "/storage/emulated/0/Download",
            "/storage/emulated/0/Pictures",
            "/storage/emulated/0/Documents",
            "/storage/emulated/0/Movies",
            "/storage/emulated/0/Music",
            "/storage/emulated/0/DCIM",
            "/storage/emulated/0/Oculus"
        };
        
        // Meta Quest専用パスを試す
        foreach (string path in metaQuestPaths)
        {
            if (IsPathAccessible(path) && !paths.Contains(path))
            {
                paths.Add(path);
                Debug.Log($"[AndroidFileAccess] Meta Quest path added: {path}");
            }
        }
        
        // 各標準フォルダをチェック（従来の方法も維持）
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
                        "_data",           // ファイルパス（MediaStoreの正しいカラム名）
                        "_display_name",   // ファイル名（MediaStoreの正しいカラム名）
                        "mime_type"        // MIMEタイプ
                    };
                    
                    // WHERE句でフォルダパスをフィルタ（大文字小文字を区別しない）
                    string selection = "_data LIKE ? AND _data NOT LIKE ?";
                    string[] selectionArgs = { folderPath + "/%", folderPath + "/%/%" };
                    
                    using (var cursor = contentResolver.Call<AndroidJavaObject>("query", uri, projection, selection, selectionArgs, null))
                    {
                        if (cursor != null && cursor.Call<bool>("moveToFirst"))
                        {
                            int dataColumnIndex = cursor.Call<int>("getColumnIndex", "_data");
                            int displayNameIndex = cursor.Call<int>("getColumnIndex", "_display_name");
                            int mimeTypeIndex = cursor.Call<int>("getColumnIndex", "mime_type");
                            
                            do
                            {
                                string filePath = cursor.Call<string>("getString", dataColumnIndex);
                                string fileName = cursor.Call<string>("getString", displayNameIndex);
                                string mimeType = cursor.Call<string>("getString", mimeTypeIndex);
                                
                                // 直接そのフォルダ内のファイルのみ取得（サブフォルダは除外）
                                if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                                {
                                    mediaFiles.Add(fileName);
                                    Debug.Log($"[AndroidFileAccess] MediaStoreファイル発見: {fileName} (MIME: {mimeType}, Path: {filePath})");
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
    /// MediaScannerを呼び出してファイルをMediaStoreに登録
    /// </summary>
    public static void ScanFile(string filePath)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
            {
                string[] paths = new string[] { filePath };
                mediaScannerConnection.CallStatic("scanFile", context, paths, null, null);
                Debug.Log($"[AndroidFileAccess] MediaScanner呼び出し: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidFileAccess] MediaScanner error: {e.Message}");
        }
        #endif
    }
    
    /// <summary>
    /// フォルダ内のすべてのファイルをMediaScannerでスキャン
    /// </summary>
    public static void ScanFolder(string folderPath)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log($"[AndroidFileAccess] === フォルダスキャン開始: {folderPath} ===");
            
            // フォルダ内のすべてのファイルを取得
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                Debug.Log($"[AndroidFileAccess] 検出ファイル数: {files.Length}");
                
                // 各ファイルの詳細を出力
                foreach (string file in files)
                {
                    Debug.Log($"[AndroidFileAccess] スキャン対象: {Path.GetFileName(file)}");
                }
                
                if (files.Length > 0)
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
                    {
                        mediaScannerConnection.CallStatic("scanFile", context, files, null, null);
                        Debug.Log($"[AndroidFileAccess] MediaScanner呼び出し完了: {files.Length}ファイル");
                    }
                }
                else
                {
                    Debug.Log($"[AndroidFileAccess] スキャン対象ファイルなし");
                }
            }
            else
            {
                Debug.LogWarning($"[AndroidFileAccess] フォルダが存在しません: {folderPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidFileAccess] ScanFolder error: {e.Message}");
        }
        #else
        Debug.Log($"[AndroidFileAccess] エディタモード: スキャンをスキップ");
        #endif
    }
    
    /// <summary>
    /// VRアプリで再生可能なファイル形式の拡張子リスト
    /// </summary>
    public static class SupportedExtensions
    {
        // 画像形式（通常画像）
        public static readonly string[] Images = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff" };
        
        // 動画形式
        public static readonly string[] Videos = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v", ".3gp", ".wmv" };
        
        // パノラマ画像（360度画像）
        public static readonly string[] Panorama360Images = { ".jpg", ".jpeg", ".png", ".exr", ".hdr" };
        
        // パノラマ動画（360度動画）
        public static readonly string[] Panorama360Videos = { ".mp4", ".mov", ".mkv", ".webm" };
        
        // テキストファイル（テスト用）
        public static readonly string[] TextFiles = { ".txt", ".log", ".md", ".json", ".xml" };
        
        /// <summary>
        /// サポートされているすべての拡張子を取得
        /// </summary>
        public static string[] GetAllSupportedExtensions()
        {
            var extensions = new System.Collections.Generic.HashSet<string>();
            
            foreach (string ext in Images) extensions.Add(ext.ToLower());
            foreach (string ext in Videos) extensions.Add(ext.ToLower());
            foreach (string ext in Panorama360Images) extensions.Add(ext.ToLower());
            foreach (string ext in Panorama360Videos) extensions.Add(ext.ToLower());
            foreach (string ext in TextFiles) extensions.Add(ext.ToLower());
            
            string[] result = new string[extensions.Count];
            extensions.CopyTo(result);
            return result;
        }
        
        /// <summary>
        /// ファイル形式の判定
        /// </summary>
        public static string GetFileType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            
            if (System.Array.IndexOf(Images, ext) >= 0) return "画像";
            if (System.Array.IndexOf(Videos, ext) >= 0) return "動画";
            if (System.Array.IndexOf(TextFiles, ext) >= 0) return "テキスト";
            
            return "その他";
        }
    }
    
    /// <summary>
    /// 通常のDirectory.GetFilesと組み合わせた拡張ファイル取得
    /// </summary>
    public static string[] GetFilesWithMediaStore(string directoryPath)
    {
        var allFiles = new System.Collections.Generic.HashSet<string>();
        
        // まず、フォルダ全体をMediaScannerでスキャン（新しいファイルを認識させる）
        ScanFolder(directoryPath);
        
        try
        {
            // サポート対象拡張子をデバッグ出力
            string[] supportedExts = SupportedExtensions.GetAllSupportedExtensions();
            Debug.Log($"[AndroidFileAccess] === サポート拡張子 ({supportedExts.Length}個) ===");
            foreach (string ext in supportedExts)
            {
                Debug.Log($"[AndroidFileAccess] サポート: '{ext}'");
            }
            Debug.Log($"[AndroidFileAccess] === サポート拡張子終了 ===");
            
            // 通常のDirectory.GetFilesを試す
            string[] standardFiles = Directory.GetFiles(directoryPath);
            Debug.Log($"[AndroidFileAccess] Directory.GetFilesで発見: {standardFiles.Length}個");
            
            // ContentProviderを使用したファイル一覧取得（Android 14対応）
            var contentFiles = GetFilesViaContentProvider(directoryPath);
            Debug.Log($"[AndroidFileAccess] ContentProvider経由で発見: {contentFiles.Count}個");
            
            // 別のアプローチでもファイル一覧を取得してみる
            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                var fileInfos = dirInfo.GetFiles();
                Debug.Log($"[AndroidFileAccess] DirectoryInfo.GetFiles()で発見: {fileInfos.Length}個");
                
                for (int i = 0; i < fileInfos.Length; i++)
                {
                    string fileName = fileInfos[i].Name;
                    string extension = Path.GetExtension(fileName).ToLower();
                    Debug.Log($"[AndroidFileAccess] DirectoryInfo[{i}]: {fileName} (拡張子: '{extension}')");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AndroidFileAccess] DirectoryInfo.GetFiles() failed: {e.Message}");
            }
            
            // すべてのファイルをリストアップ（デバッグ用）
            Debug.Log($"[AndroidFileAccess] === Directory.GetFiles()の生データ ===");
            for (int i = 0; i < standardFiles.Length; i++)
            {
                string fileName = Path.GetFileName(standardFiles[i]);
                string extension = Path.GetExtension(fileName).ToLower();
                bool isSupported = supportedExts.Contains(extension);
                Debug.Log($"[AndroidFileAccess] Raw[{i}]: {fileName} (拡張子: '{extension}', サポート: {isSupported})");
            }
            Debug.Log($"[AndroidFileAccess] === 生データ終了 ===");
            
            // ContentProviderの結果をallFilesに追加
            foreach (string file in contentFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string extension = Path.GetExtension(fileName).ToLower();
                    if (supportedExts.Contains(extension))
                    {
                        allFiles.Add(fileName);
                        Debug.Log($"[AndroidFileAccess] 🔧 ContentProvider発見: {fileName} ({extension})");
                    }
                }
            }
            
            // 詳細なデバッグ情報を出力
            var extensionCounts = new System.Collections.Generic.Dictionary<string, int>();
            var filesByType = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
            
            foreach (string file in standardFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string extension = Path.GetExtension(fileName).ToLower();
                    string fileType = SupportedExtensions.GetFileType(fileName);
                    
                    // サポートされている拡張子のみ追加
                    if (SupportedExtensions.GetAllSupportedExtensions().Contains(extension))
                    {
                        allFiles.Add(fileName);
                        
                        // 拡張子別カウント
                        if (!extensionCounts.ContainsKey(extension))
                            extensionCounts[extension] = 0;
                        extensionCounts[extension]++;
                        
                        // ファイル詳細情報
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            Debug.Log($"[AndroidFileAccess]   📄 {fileName} ({fileType}) - {fileInfo.Length} bytes");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[AndroidFileAccess] ファイル情報取得失敗: {fileName} - {e.Message}");
                        }
                        
                        // MediaScannerで個別スキャン
                        ScanFile(file);
                    }
                    else
                    {
                        Debug.Log($"[AndroidFileAccess] サポート外ファイル: {fileName} ({extension})");
                    }
                }
            }
            
            // 拡張子別サマリーを出力
            Debug.Log($"[AndroidFileAccess] === 拡張子別サマリー ===");
            Debug.Log($"[AndroidFileAccess] 📸 画像: {GetCountByCategory(extensionCounts, SupportedExtensions.Images)}個");
            Debug.Log($"[AndroidFileAccess] 🎬 動画: {GetCountByCategory(extensionCounts, SupportedExtensions.Videos)}個");
            Debug.Log($"[AndroidFileAccess] 📝 テキスト: {GetCountByCategory(extensionCounts, SupportedExtensions.TextFiles)}個");
            Debug.Log($"[AndroidFileAccess] === サマリー終了 ===");
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
        
        Debug.Log($"[AndroidFileAccess] === ファイル検出結果サマリー ===");
        Debug.Log($"[AndroidFileAccess] 合計発見ファイル数: {allFiles.Count}個");
        
        // 発見されたファイル一覧を出力
        foreach (string fileName in allFiles)
        {
            Debug.Log($"[AndroidFileAccess] 📄 検出: {fileName}");
        }
        Debug.Log($"[AndroidFileAccess] === サマリー終了 ===");
        
        // HashSetを配列に変換
        string[] result = new string[allFiles.Count];
        allFiles.CopyTo(result);
        return result;
    }
    
    /// <summary>
    /// ContentProviderを使用してファイル一覧を直接取得（Android 14対応）
    /// </summary>
    static System.Collections.Generic.List<string> GetFilesViaContentProvider(string folderPath)
    {
        var files = new System.Collections.Generic.List<string>();
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log($"[AndroidFileAccess] ContentProvider検索開始: {folderPath}");
            
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = context.Call<AndroidJavaObject>("getContentResolver"))
            {
                // 外部ストレージの全ファイルを検索
                using (var mediaStoreFiles = new AndroidJavaClass("android.provider.MediaStore$Files"))
                {
                    var externalUri = mediaStoreFiles.CallStatic<AndroidJavaObject>("getContentUri", "external");
                    
                    string[] projection = {
                    "_data",           // ファイルパス
                    "_display_name",   // ファイル名
                    "mime_type",       // MIMEタイプ
                    "_size"            // ファイルサイズ
                };
                
                // 指定フォルダ内のファイルのみ検索
                string selection = "_data LIKE ?";
                string[] selectionArgs = { folderPath + "/%" };
                
                using (var cursor = contentResolver.Call<AndroidJavaObject>("query", externalUri, projection, selection, selectionArgs, "_display_name"))
                {
                    if (cursor != null && cursor.Call<bool>("moveToFirst"))
                    {
                        int dataIndex = cursor.Call<int>("getColumnIndex", "_data");
                        int nameIndex = cursor.Call<int>("getColumnIndex", "_display_name");
                        int mimeIndex = cursor.Call<int>("getColumnIndex", "mime_type");
                        int sizeIndex = cursor.Call<int>("getColumnIndex", "_size");
                        
                        do
                        {
                            string filePath = cursor.Call<string>("getString", dataIndex);
                            string fileName = cursor.Call<string>("getString", nameIndex);
                            string mimeType = cursor.Call<string>("getString", mimeIndex);
                            long fileSize = cursor.Call<long>("getLong", sizeIndex);
                            
                            // サブフォルダを除外（直接そのフォルダ内のファイルのみ）
                            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                            {
                                string directory = Path.GetDirectoryName(filePath).Replace('\\', '/');
                                
                                // 正確にそのフォルダ内のファイルかチェック
                                if (string.Equals(directory, folderPath.Replace('\\', '/'), System.StringComparison.OrdinalIgnoreCase))
                                {
                                    files.Add(fileName);
                                    Debug.Log($"[AndroidFileAccess] ContentProvider: {fileName} ({mimeType}, {fileSize}bytes) at {filePath}");
                                }
                            }
                        }
                        while (cursor.Call<bool>("moveToNext"));
                        
                        cursor.Call("close");
                    }
                }
                }
            }
            
            Debug.Log($"[AndroidFileAccess] ContentProvider検索完了: {files.Count}個のファイルを発見");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidFileAccess] ContentProvider検索エラー: {e.Message}");
        }
        #else
        Debug.Log("[AndroidFileAccess] エディタモードのためContentProvider検索をスキップ");
        #endif
        
        return files;
    }
    
    /// <summary>
    /// 指定された拡張子カテゴリの合計ファイル数を取得
    /// </summary>
    private static int GetCountByCategory(System.Collections.Generic.Dictionary<string, int> extensionCounts, string[] categoryExtensions)
    {
        int total = 0;
        foreach (string ext in categoryExtensions)
        {
            if (extensionCounts.ContainsKey(ext.ToLower()))
            {
                total += extensionCounts[ext.ToLower()];
            }
        }
        return total;
    }
}