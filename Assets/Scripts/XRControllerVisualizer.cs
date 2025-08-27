using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// XRコントローラーの可視化とモデル設定
/// </summary>
[RequireComponent(typeof(ActionBasedController))]
public class XRControllerVisualizer : MonoBehaviour
{
    [Header("コントローラー設定")]
    [SerializeField] private bool isLeftHand = true;
    [SerializeField] private bool showDebugModel = true;
    [SerializeField] private Color debugModelColor = Color.white;
    
    private ActionBasedController controller;
    private GameObject debugModel;
    
    void Start()
    {
        SetupController();
        if (showDebugModel)
        {
            CreateDebugModel();
        }
    }
    
    void SetupController()
    {
        // ActionBasedControllerコンポーネントを取得または追加
        controller = GetComponent<ActionBasedController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<ActionBasedController>();
            Debug.Log($"{gameObject.name}にActionBasedControllerを追加しました");
        }
        
        // コントローラーの種類を自動判定
        if (gameObject.name.ToLower().Contains("left"))
        {
            isLeftHand = true;
        }
        else if (gameObject.name.ToLower().Contains("right"))
        {
            isLeftHand = false;
        }
        
        Debug.Log($"{gameObject.name}を{(isLeftHand ? "左手" : "右手")}コントローラーとして設定しました");
    }
    
    void CreateDebugModel()
    {
        // デバッグ用の簡易モデルを作成
        debugModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugModel.name = "ControllerDebugModel";
        debugModel.transform.SetParent(transform);
        debugModel.transform.localPosition = Vector3.zero;
        debugModel.transform.localRotation = Quaternion.identity;
        
        // サイズ調整（コントローラーサイズに近い大きさ）
        debugModel.transform.localScale = new Vector3(0.08f, 0.04f, 0.12f);
        
        // マテリアル設定
        Renderer renderer = debugModel.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = isLeftHand ? new Color(1f, 0.5f, 0.5f, 0.8f) : new Color(0.5f, 0.5f, 1f, 0.8f);
            renderer.material = mat;
        }
        
        // コライダーを無効化（レーザーポインターの邪魔にならないように）
        Collider collider = debugModel.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // ポインター方向を示す小さなシリンダーを追加
        GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pointer.name = "PointerDirection";
        pointer.transform.SetParent(debugModel.transform);
        pointer.transform.localPosition = new Vector3(0, 0, 0.8f);
        pointer.transform.localRotation = Quaternion.Euler(90, 0, 0);
        pointer.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        
        Renderer pointerRenderer = pointer.GetComponent<Renderer>();
        if (pointerRenderer != null)
        {
            Material pointerMat = new Material(Shader.Find("Sprites/Default"));
            pointerMat.color = new Color(1f, 1f, 0f, 0.5f);
            pointerRenderer.material = pointerMat;
        }
        
        Collider pointerCollider = pointer.GetComponent<Collider>();
        if (pointerCollider != null)
        {
            pointerCollider.enabled = false;
        }
        
        Debug.Log($"{gameObject.name}にデバッグモデルを作成しました");
    }
    
    void Update()
    {
        // コントローラーの位置と回転が正しくトラッキングされているかデバッグ表示
        if (Time.frameCount % 60 == 0) // 1秒ごとに更新
        {
            if (transform.position != Vector3.zero || transform.rotation != Quaternion.identity)
            {
                Debug.Log($"{gameObject.name} - 位置: {transform.position}, 回転: {transform.rotation.eulerAngles}");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // コントローラーの位置を表示
            Gizmos.color = isLeftHand ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.02f);
            
            // 前方向を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
        }
    }
}