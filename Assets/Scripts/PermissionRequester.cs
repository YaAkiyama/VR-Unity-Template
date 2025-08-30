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
        
        // Android 6.0以上で実行時パーミッションが必要
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Debug.Log("[PermissionRequester] READ_EXTERNAL_STORAGE権限を要求中...");
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            
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
    /// パーミッションステータスを確認
    /// </summary>
    public static bool HasStoragePermission()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
        #else
        return true;
        #endif
    }
}