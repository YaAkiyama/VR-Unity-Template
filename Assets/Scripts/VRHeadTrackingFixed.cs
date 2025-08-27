using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

/// <summary>
/// 修正版VRヘッドトラッキング設定
/// Meta Quest 3でのヘッドセット動きとカメラ同期を実現
/// </summary>
public class VRHeadTrackingFixed : MonoBehaviour
{
    [Header("VR設定")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionAsset;
    
    private XROrigin xrOrigin;
    private Camera xrCamera;
    private Transform cameraOffset;
    
    void Awake()
    {
        if (setupOnAwake)
        {
            SetupVRHeadTracking();
        }
    }
    
    [ContextMenu("Setup VR Head Tracking")]
    public void SetupVRHeadTracking()
    {
        Log("VRヘッドトラッキングのセットアップを開始します...");
        
        // Input Action Assetを自動検索
        FindInputActionAsset();
        
        // XR Originの設定
        SetupXROrigin();
        
        // カメラのヘッドトラッキング設定
        SetupCameraHeadTracking();
        
        // コントローラーの設定
        SetupControllers();
        
        Log("VRヘッドトラッキングのセットアップが完了しました！");
    }
    
    void FindInputActionAsset()
    {
        if (inputActionAsset == null)
        {
            // プロジェクト内のInputSystem_Actionsを探す
            inputActionAsset = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActionAsset == null)
            {
                // Assetsフォルダから探す
                var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                foreach (var asset in assets)
                {
                    if (asset.name.Contains("InputSystem_Actions"))
                    {
                        inputActionAsset = asset;
                        break;
                    }
                }
            }
            
            if (inputActionAsset != null)
            {
                Log($"Input Action Assetを発見: {inputActionAsset.name}");
            }
            else
            {
                Log("警告: Input Action Assetが見つかりません。手動で設定してください。");
            }
        }
    }
    
    void SetupXROrigin()
    {
        // XROriginコンポーネントを取得または追加
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            xrOrigin = gameObject.AddComponent<XROrigin>();
            Log("XROriginコンポーネントを追加しました");
        }
        
        // 基本設定
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        xrOrigin.CameraYOffset = 0.0f;
        
        // Camera Offsetを取得
        cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
            Log("Camera Offsetを設定しました");
        }
        
        // カメラを取得
        xrCamera = GetComponentInChildren<Camera>();
        if (xrCamera != null && xrCamera.tag == "MainCamera")
        {
            xrOrigin.Camera = xrCamera;
            Log("Main Cameraを設定しました");
        }
    }
    
    void SetupCameraHeadTracking()
    {
        if (xrCamera == null)
        {
            Log("エラー: カメラが見つかりません");
            return;
        }
        
        Log("カメラのヘッドトラッキングを設定中...");
        
        // 既存のTrackedPoseDriverを削除
        var existingDriver = xrCamera.GetComponent<TrackedPoseDriver>();
        if (existingDriver != null)
        {
            DestroyImmediate(existingDriver);
            Log("既存のTrackedPoseDriverを削除しました");
        }
        
        // 新しいTrackedPoseDriverを追加
        var trackedPoseDriver = xrCamera.gameObject.AddComponent<TrackedPoseDriver>();
        
        // 基本設定
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        
        // Input Actionの設定
        if (inputActionAsset != null)
        {
            var hmdMap = inputActionAsset.FindActionMap("XRI HMD");
            if (hmdMap != null)
            {
                var positionAction = hmdMap.FindAction("centerEyePosition");
                var rotationAction = hmdMap.FindAction("centerEyeRotation");
                
                if (positionAction != null)
                {
                    trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
                    Log("HMD Position Actionを設定しました");
                }
                
                if (rotationAction != null)
                {
                    trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
                    Log("HMD Rotation Actionを設定しました");
                }
            }
        }
        else
        {
            Log("警告: Input Action Assetが設定されていません。手動で設定してください。");
        }
        
        Log("カメラのヘッドトラッキング設定が完了しました");
    }
    
    void SetupControllers()
    {
        if (cameraOffset == null) return;
        
        Log("コントローラーを設定中...");
        
        // 左手コントローラー
        Transform leftController = cameraOffset.Find("LeftHand Controller");
        if (leftController != null)
        {
            SetupController(leftController, true);
        }
        
        // 右手コントローラー
        Transform rightController = cameraOffset.Find("RightHand Controller");
        if (rightController != null)
        {
            SetupController(rightController, false);
        }
    }
    
    void SetupController(Transform controller, bool isLeftHand)
    {
        string handName = isLeftHand ? "左手" : "右手";
        string mapName = isLeftHand ? "XRI LeftHand" : "XRI RightHand";
        
        Log($"{handName}コントローラーを設定中...");
        
        // ActionBasedControllerの設定
        var actionBasedController = controller.GetComponent<ActionBasedController>();
        if (actionBasedController == null)
        {
            actionBasedController = controller.gameObject.AddComponent<ActionBasedController>();
        }
        
        // TrackedPoseDriverの設定
        var trackedPoseDriver = controller.GetComponent<TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = controller.gameObject.AddComponent<TrackedPoseDriver>();
        }
        
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        
        // Input Actionの設定
        if (inputActionAsset != null)
        {
            var handMap = inputActionAsset.FindActionMap(mapName);
            if (handMap != null)
            {
                var positionAction = handMap.FindAction("Position");
                var rotationAction = handMap.FindAction("Rotation");
                var selectAction = handMap.FindAction("Select");
                
                if (positionAction != null)
                {
                    trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
                    actionBasedController.positionAction = new InputActionProperty(positionAction);
                }
                
                if (rotationAction != null)
                {
                    trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
                    actionBasedController.rotationAction = new InputActionProperty(rotationAction);
                }
                
                if (selectAction != null)
                {
                    actionBasedController.selectAction = new InputActionProperty(selectAction);
                }
            }
        }
        
        Log($"{handName}コントローラーの設定が完了しました");
    }
    
    void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[VRHeadTrackingFixed] {message}");
        }
    }
    
    void OnEnable()
    {
        if (inputActionAsset != null)
        {
            inputActionAsset.Enable();
        }
    }
    
    void OnDisable()
    {
        if (inputActionAsset != null)
        {
            inputActionAsset.Disable();
        }
    }
}