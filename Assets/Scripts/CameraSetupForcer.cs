using UnityEngine;
using UnityEngine.InputSystem.XR;

/// <summary>
/// カメラ設定を強制的に修正するスクリプト
/// "Display1 No cameras rendering"エラーを解決
/// </summary>
public class CameraSetupForcer : MonoBehaviour
{
    [Header("強制設定")]
    [SerializeField] private bool forceSetupOnAwake = true;
    [SerializeField] private bool debugLog = true;
    
    void Awake()
    {
        if (forceSetupOnAwake)
        {
            ForceSetupCamera();
        }
    }
    
    [ContextMenu("Force Setup Camera")]
    public void ForceSetupCamera()
    {
        Log("カメラの強制設定を開始します...");
        
        // Cameraコンポーネントを強制追加
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
            Log("Cameraコンポーネントを追加しました");
        }
        
        // AudioListenerを強制追加
        AudioListener listener = GetComponent<AudioListener>();
        if (listener == null)
        {
            listener = gameObject.AddComponent<AudioListener>();
            Log("AudioListenerコンポーネントを追加しました");
        }
        
        // TrackedPoseDriverを強制追加
        TrackedPoseDriver tpd = GetComponent<TrackedPoseDriver>();
        if (tpd == null)
        {
            tpd = gameObject.AddComponent<TrackedPoseDriver>();
            Log("TrackedPoseDriverコンポーネントを追加しました");
            
            // VR設定
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }
        
        // カメラの基本設定
        cam.enabled = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = Color.black;
        cam.cullingMask = -1; // すべて表示
        cam.depth = 0;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;
        cam.renderingPath = RenderingPath.UsePlayerSettings;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        
        // VR用設定
        cam.stereoTargetEye = StereoTargetEyeMask.Both;
        
        Log("カメラの強制設定が完了しました");
        
        // 設定内容をデバッグ出力
        DebugCameraSettings(cam);
    }
    
    void DebugCameraSettings(Camera cam)
    {
        Log($"=== カメラ設定確認 ===");
        Log($"Enabled: {cam.enabled}");
        Log($"ClearFlags: {cam.clearFlags}");
        Log($"CullingMask: {cam.cullingMask}");
        Log($"Depth: {cam.depth}");
        Log($"NearClip: {cam.nearClipPlane}");
        Log($"FarClip: {cam.farClipPlane}");
        Log($"FOV: {cam.fieldOfView}");
        Log($"StereoTargetEye: {cam.stereoTargetEye}");
        
        // コンポーネントの存在確認
        Log($"AudioListener: {(GetComponent<AudioListener>() != null ? "あり" : "なし")}");
        Log($"TrackedPoseDriver: {(GetComponent<TrackedPoseDriver>() != null ? "あり" : "なし")}");
        Log($"====================");
    }
    
    void Log(string message)
    {
        if (debugLog)
        {
            Debug.Log($"[CameraSetupForcer] {message}");
        }
    }
    
    // 実行時にカメラが正しく動作しているかチェック
    void Update()
    {
        if (Time.frameCount == 60) // 1秒後にチェック
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                Log("カメラは正常に動作しています");
            }
            else
            {
                Log("警告: カメラが無効化されています");
            }
        }
    }
}