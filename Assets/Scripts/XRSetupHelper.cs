using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

/// <summary>
/// XR Originの設定を補助するスクリプト
/// 実行時に必要なコンポーネントをチェックし、設定を修正
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class XRSetupHelper : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Transform cameraOffset;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;
    
    private XROrigin xrOrigin;
    
    void Awake()
    {
        SetupXROrigin();
        ValidateReferences();
        SetupCamera();
    }
    
    void SetupXROrigin()
    {
        // XROriginコンポーネントを取得または追加
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            xrOrigin = gameObject.AddComponent<XROrigin>();
            Debug.Log("XROriginコンポーネントを追加しました");
        }
        
        // 参照を自動検出
        if (cameraOffset == null)
        {
            Transform offset = transform.Find("Camera Offset");
            if (offset != null)
            {
                cameraOffset = offset;
                Debug.Log("Camera Offsetを自動検出しました");
            }
        }
        
        if (xrCamera == null)
        {
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.gameObject.activeInHierarchy && cam.tag == "MainCamera")
                {
                    xrCamera = cam;
                    Debug.Log("Main Cameraを自動検出しました");
                    break;
                }
            }
        }
        
        // XROriginの設定
        if (xrOrigin != null)
        {
            if (xrCamera != null)
            {
                xrOrigin.Camera = xrCamera;
            }
            
            if (cameraOffset != null)
            {
                xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
            }
            
            // トラッキング原点モードを設定（床基準）
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            
            // カメラの高さオフセット（Meta Quest 3のデフォルト）
            xrOrigin.CameraYOffset = 0.0f; // Floor modeでは0
            
            Debug.Log("XROriginの設定が完了しました");
        }
    }
    
    void ValidateReferences()
    {
        // コントローラーの参照を自動検出
        if (leftController == null && cameraOffset != null)
        {
            Transform left = cameraOffset.Find("LeftHand Controller");
            if (left != null)
            {
                leftController = left;
                Debug.Log("LeftHand Controllerを自動検出しました");
            }
        }
        
        if (rightController == null && cameraOffset != null)
        {
            Transform right = cameraOffset.Find("RightHand Controller");
            if (right != null)
            {
                rightController = right;
                Debug.Log("RightHand Controllerを自動検出しました");
            }
        }
    }
    
    void SetupCamera()
    {
        if (xrCamera == null) return;
        
        // TrackedPoseDriverを確認・追加
        var trackedPoseDriver = xrCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = xrCamera.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            Debug.Log("TrackedPoseDriverを追加しました");
            
            // デフォルト設定
            trackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }
        
        // カメラの基本設定を確認
        xrCamera.nearClipPlane = 0.01f; // VR用に近いクリップ面を設定
        xrCamera.farClipPlane = 1000f;
        
        Debug.Log("カメラの設定が完了しました");
    }
    
    // エディタでのデバッグ用
    void OnDrawGizmos()
    {
        if (Application.isPlaying && xrOrigin != null)
        {
            // XR原点を表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // カメラ方向を表示
            if (xrCamera != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(xrCamera.transform.position, xrCamera.transform.forward * 2f);
            }
        }
    }
}