using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VRコントローラーのデバッグ情報を表示
/// コントローラーの状態と位置を確認
/// </summary>
public class VRControllerDebugger : MonoBehaviour
{
    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool isLeftController = false;
    
    [Header("強制可視化")]
    [SerializeField] private bool forceCreateVisual = true;
    [SerializeField] private float visualSize = 0.1f;
    
    private ActionBasedController actionController;
    private GameObject debugVisual;
    
    void Start()
    {
        actionController = GetComponent<ActionBasedController>();
        
        if (forceCreateVisual)
        {
            CreateForceVisual();
        }
        
        LogControllerInfo();
    }
    
    void CreateForceVisual()
    {
        // 強制的にコントローラーを可視化
        debugVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugVisual.name = $"DEBUG_Controller_{(isLeftController ? "Left" : "Right")}";
        debugVisual.transform.SetParent(transform);
        debugVisual.transform.localPosition = Vector3.zero;
        debugVisual.transform.localScale = Vector3.one * visualSize;
        
        // コライダーを削除
        Collider col = debugVisual.GetComponent<Collider>();
        if (col) DestroyImmediate(col);
        
        // 目立つ色に設定
        Renderer renderer = debugVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = isLeftController ? Color.magenta : Color.cyan;
            renderer.material = mat;
        }
        
        Log("強制デバッグビジュアルを作成しました");
    }
    
    void LogControllerInfo()
    {
        Log("=== コントローラー情報 ===");
        Log($"GameObject名: {gameObject.name}");
        Log($"位置: {transform.position}");
        Log($"回転: {transform.rotation.eulerAngles}");
        Log($"スケール: {transform.localScale}");
        Log($"アクティブ: {gameObject.activeInHierarchy}");
        
        if (actionController != null)
        {
            Log($"ActionBasedController: 有り");
            Log($"Position Action: {(actionController.positionAction.action != null ? "設定済み" : "未設定")}");
            Log($"Rotation Action: {(actionController.rotationAction.action != null ? "設定済み" : "未設定")}");
            Log($"Select Action: {(actionController.selectAction.action != null ? "設定済み" : "未設定")}");
        }
        else
        {
            Log("ActionBasedController: 無し");
        }
        
        // 他のコンポーネントをチェック
        Component[] components = GetComponents<Component>();
        Log($"コンポーネント数: {components.Length}");
        foreach (Component comp in components)
        {
            Log($"- {comp.GetType().Name}");
        }
        
        Log("========================");
    }
    
    void Update()
    {
        if (debugVisual != null)
        {
            // デバッグビジュアルの位置を更新
            debugVisual.transform.position = transform.position;
            debugVisual.transform.rotation = transform.rotation;
            
            // トリガー状態に応じて色を変更
            if (actionController != null && actionController.selectAction.action != null)
            {
                bool isPressed = actionController.selectAction.action.IsPressed();
                Renderer renderer = debugVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = isPressed ? Color.yellow : 
                        (isLeftController ? Color.magenta : Color.cyan);
                }
            }
        }
        
        // 定期的に位置情報をログ出力（5秒ごと）
        if (enableDebugLog && Time.time % 5f < Time.deltaTime)
        {
            LogPositionUpdate();
        }
    }
    
    void LogPositionUpdate()
    {
        Log($"[Update] 位置: {transform.position:F3}, 回転: {transform.rotation.eulerAngles:F1}");
        
        if (actionController != null)
        {
            var state = actionController.currentControllerState;
            Log($"[Controller] Tracked: {state.isTracked}, Position: {state.position:F3}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // コントローラーの位置と向きを表示
        Gizmos.color = isLeftController ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        
        // 前方向を表示
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
        
        // 上方向を表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.up * 0.1f);
    }
    
    void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[VRControllerDebugger:{(isLeftController ? "L" : "R")}] {message}");
        }
    }
    
    void OnDestroy()
    {
        if (debugVisual != null)
        {
            DestroyImmediate(debugVisual);
        }
    }
    
    // インスペクターから呼び出し可能
    [ContextMenu("Log Controller Info")]
    public void ManualLogInfo()
    {
        LogControllerInfo();
    }
    
    [ContextMenu("Recreate Debug Visual")]
    public void RecreateDebugVisual()
    {
        if (debugVisual != null)
        {
            DestroyImmediate(debugVisual);
        }
        CreateForceVisual();
    }
}