using UnityEngine;
using UnityEngine.Android;
using System.Collections;

/// <summary>
/// Android実行時パーミッション要求ヘルパー
/// 外部ストレージアクセスに必要な権限を自動的に要求
/// </summary>
public class PermissionRequester : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[PermissionRequester] Start() 実行開始");
        StartCoroutine(RequestPermissions());
    }
    
    IEnumerator RequestPermissions()
    {
        Debug.Log("[PermissionRequester] RequestPermissions() コルーチン開始");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[PermissionRequester] Android実機モードで実行中");
        
        // Androidバージョンを確認
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = version.GetStatic<int>("SDK_INT");
            Debug.Log($"[PermissionRequester] Android SDK Version: {sdkInt}");
            
            // Android 11（API 30）以降は特別な処理が必要
            if (sdkInt >= 30)
            {
                Debug.Log("[PermissionRequester] Android 11以降のため、特別な処理を実行");
                
                // MANAGE_EXTERNAL_STORAGE権限の確認（Android 11+）
                bool hasAllFilesAccess = CheckAllFilesAccess();
                if (!hasAllFilesAccess)
                {
                    Debug.Log("[PermissionRequester] MANAGE_EXTERNAL_STORAGE権限がありません");
                    Debug.Log("[PermissionRequester] 注意: Meta Questでは設定アプリから手動で権限を付与する必要があります");
                    
                    // アプリ専用領域の使用を推奨
                    Debug.Log($"[PermissionRequester] 推奨: アプリ専用領域を使用 - {Application.persistentDataPath}");
                }
                else
                {
                    Debug.Log("[PermissionRequester] MANAGE_EXTERNAL_STORAGE権限を持っています");
                }
            }
        }
        
        // 従来のREAD_EXTERNAL_STORAGE権限も試す（Android 10以下との互換性）
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Debug.Log("[PermissionRequester] READ_EXTERNAL_STORAGE権限を要求中...");
            
            // Meta Questでは権限ダイアログが表示されない可能性があるため、
            // Callback方式で権限要求を試みる
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += permissionName => {
                Debug.Log($"[PermissionRequester] 権限が付与されました: {permissionName}");
            };
            callbacks.PermissionDenied += permissionName => {
                Debug.Log($"[PermissionRequester] 権限が拒否されました: {permissionName}");
            };
            callbacks.PermissionDeniedAndDontAskAgain += permissionName => {
                Debug.Log($"[PermissionRequester] 権限が拒否され、今後表示しない: {permissionName}");
            };
            
            Permission.RequestUserPermission(Permission.ExternalStorageRead, callbacks);
            
            // パーミッションダイアログの応答を待つ
            yield return new WaitForSeconds(1f);
            
            // 権限が付与されたか確認
            if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Debug.Log("[PermissionRequester] READ_EXTERNAL_STORAGE権限が付与されました");
            }
            else
            {
                Debug.LogWarning("[PermissionRequester] READ_EXTERNAL_STORAGE権限が拒否されました");
            }
        }
        else
        {
            Debug.Log("[PermissionRequester] 既にREAD_EXTERNAL_STORAGE権限を持っています");
        }
        
        // Android 10以下の場合のみWRITE_EXTERNAL_STORAGE権限も要求
        if (Application.platform == RuntimePlatform.Android)
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = version.GetStatic<int>("SDK_INT");
                if (sdkInt <= 29) // Android 10以下
                {
                    if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                    {
                        Debug.Log("[PermissionRequester] WRITE_EXTERNAL_STORAGE権限を要求中...");
                        Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                        
                        yield return new WaitForSeconds(1f);
                        
                        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                        {
                            Debug.Log("[PermissionRequester] WRITE_EXTERNAL_STORAGE権限が付与されました");
                        }
                        else
                        {
                            Debug.LogWarning("[PermissionRequester] WRITE_EXTERNAL_STORAGE権限が拒否されました");
                        }
                    }
                }
            }
        }
        #else
        Debug.Log("[PermissionRequester] エディタモードではパーミッション要求をスキップ");
        yield return null;
        #endif
    }
    
    /// <summary>
    /// Android 11以降のMANAGE_EXTERNAL_STORAGE権限を確認
    /// </summary>
    static bool CheckAllFilesAccess()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var environment = new AndroidJavaClass("android.os.Environment"))
            {
                bool isExternalStorageManager = environment.CallStatic<bool>("isExternalStorageManager");
                return isExternalStorageManager;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PermissionRequester] CheckAllFilesAccess error: {e.Message}");
            return false;
        }
        #else
        return true;
        #endif
    }
    
    /// <summary>
    /// パーミッションステータスを確認
    /// </summary>
    public static bool HasStoragePermission()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Android 11以降はMANAGE_EXTERNAL_STORAGE権限も確認
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = version.GetStatic<int>("SDK_INT");
            if (sdkInt >= 30)
            {
                return CheckAllFilesAccess();
            }
        }
        
        return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
        #else
        return true;
        #endif
    }
}