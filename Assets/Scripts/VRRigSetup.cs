using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;

/// <summary>
/// VR_Viewerプロジェクトの動作確認済み設定を移植
/// Meta Quest 3で正しく動作するXR Originの構成を作成
/// </summary>
public class VRRigSetup : MonoBehaviour
{
    [Header("XR Rig構成")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool debugMode = true;
    
    private XROrigin xrOrigin;
    private Camera xrCamera;
    private Transform cameraOffset;
    private Transform leftController;
    private Transform rightController;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupVRRig();
        }
    }
    
    [ContextMenu("Setup VR Rig")]
    public void SetupVRRig()
    {
        Log("VR Rigのセットアップを開始します...");
        
        // 1. XROriginの設定
        SetupXROrigin();
        
        // 2. カメラの設定
        SetupCamera();
        
        // 3. コントローラーの設定
        SetupControllers();
        
        Log("VR Rigのセットアップが完了しました！");
    }
    
    void SetupXROrigin()
    {
        Log("XROriginを設定中...");
        
        // XROriginコンポーネントを取得または追加
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            xrOrigin = gameObject.AddComponent<XROrigin>();
        }
        
        // 基本設定（VR_Viewerプロジェクトの設定を移植）
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        xrOrigin.CameraYOffset = 0.0f; // Floor modeでは0
        
        // Camera Offsetを探す
        cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
            Log("Camera Offsetを設定しました");
        }
        else
        {
            Log("エラー: Camera Offsetが見つかりません");
        }
        
        // カメラを探す
        xrCamera = GetComponentInChildren<Camera>();
        if (xrCamera != null && xrCamera.tag == "MainCamera")
        {
            xrOrigin.Camera = xrCamera;
            Log("Main Cameraを設定しました");
        }
        else
        {
            Log("エラー: Main Cameraが見つかりません");
        }
    }
    
    void SetupCamera()
    {
        if (xrCamera == null)
        {
            Log("エラー: カメラが見つかりません");
            return;
        }
        
        Log("カメラを設定中...");
        
        // TrackedPoseDriverの設定（最重要：これがないとヘッドトラッキングが動作しない）
        var trackedPoseDriver = xrCamera.GetComponent<TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = xrCamera.gameObject.AddComponent<TrackedPoseDriver>();
            Log("TrackedPoseDriverを追加しました");
        }
        
        // VR_Viewerプロジェクトと同じ設定
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        
        // カメラの基本設定
        xrCamera.nearClipPlane = 0.01f;
        xrCamera.farClipPlane = 1000.0f;
        
        Log("カメラの設定が完了しました");
    }
    
    void SetupControllers()
    {
        Log("コントローラーを設定中...");
        
        if (cameraOffset == null)
        {
            Log("エラー: Camera Offsetが見つかりません");
            return;
        }
        
        // 左手コントローラー
        leftController = cameraOffset.Find("LeftHand Controller");
        if (leftController != null)
        {
            SetupController(leftController, true);
        }
        else
        {
            Log("警告: LeftHand Controllerが見つかりません");
        }
        
        // 右手コントローラー
        rightController = cameraOffset.Find("RightHand Controller");
        if (rightController != null)
        {
            SetupController(rightController, false);
        }
        else
        {
            Log("警告: RightHand Controllerが見つかりません");
        }
    }
    
    void SetupController(Transform controller, bool isLeftHand)
    {
        string handName = isLeftHand ? "左手" : "右手";
        Log($"{handName}コントローラーを設定中...");
        
        // ActionBasedControllerの設定
        var actionBasedController = controller.GetComponent<ActionBasedController>();
        if (actionBasedController == null)
        {
            actionBasedController = controller.gameObject.AddComponent<ActionBasedController>();
            Log($"{handName}にActionBasedControllerを追加しました");
        }
        
        // TrackedPoseDriverの設定（これが最重要：コントローラーの位置・回転を正しく取得）
        var trackedPoseDriver = controller.GetComponent<TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = controller.gameObject.AddComponent<TrackedPoseDriver>();
            Log($"{handName}にTrackedPoseDriverを追加しました");
        }
        
        // Controller Nodeの設定
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        
        Log($"{handName}コントローラーの設定が完了しました");
    }
    
    void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[VRRigSetup] {message}");
        }
    }
    
    // デバッグ用：現在の状態を表示
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Log("=== VR Rig状態 ===");
        
        if (xrOrigin != null)
        {
            Log($"XROrigin: OK, TrackingMode: {xrOrigin.RequestedTrackingOriginMode}");
        }
        else
        {
            Log("XROrigin: 見つかりません");
        }
        
        if (xrCamera != null)
        {
            var trackedPoseDriver = xrCamera.GetComponent<TrackedPoseDriver>();
            Log($"Camera: OK, TrackedPoseDriver: {(trackedPoseDriver != null ? "あり" : "なし")}");
        }
        else
        {
            Log("Camera: 見つかりません");
        }
        
        if (leftController != null)
        {
            var trackedPoseDriver = leftController.GetComponent<TrackedPoseDriver>();
            Log($"左手Controller: OK, TrackedPoseDriver: {(trackedPoseDriver != null ? "あり" : "なし")}");
        }
        
        if (rightController != null)
        {
            var trackedPoseDriver = rightController.GetComponent<TrackedPoseDriver>();
            Log($"右手Controller: OK, TrackedPoseDriver: {(trackedPoseDriver != null ? "あり" : "なし")}");
        }
        
        Log("=================");
    }
}