using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VRコントローラーを可視化するシンプルなスクリプト
/// コントローラーの位置に3Dモデルを表示
/// </summary>
public class VRControllerVisualizer : MonoBehaviour
{
    [Header("コントローラー設定")]
    [SerializeField] private bool isLeftController = false;
    [SerializeField] private bool showDebugVisual = true;
    
    [Header("ビジュアル設定")]
    [SerializeField] private GameObject controllerModelPrefab;
    [SerializeField] private float visualScale = 1f;
    
    private GameObject visualObject;
    private ActionBasedController actionController;
    
    void Start()
    {
        // ActionBasedControllerコンポーネントを取得
        actionController = GetComponent<ActionBasedController>();
        
        // コントローラーの可視化を作成
        CreateControllerVisual();
    }
    
    void CreateControllerVisual()
    {
        if (!showDebugVisual) return;
        
        // カスタムモデルがある場合はそれを使用
        if (controllerModelPrefab != null)
        {
            visualObject = Instantiate(controllerModelPrefab, transform);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one * visualScale;
        }
        else
        {
            // デフォルトの可視化（シンプルなプリミティブ形状）
            CreateDefaultVisual();
        }
        
        Debug.Log($"[VRControllerVisualizer] {(isLeftController ? "左手" : "右手")}コントローラーの可視化を作成しました");
    }
    
    void CreateDefaultVisual()
    {
        // Meta Quest 3 風のコントローラー作成
        GameObject controllerRoot = new GameObject($"MetaQuest_Controller_{(isLeftController ? "Left" : "Right")}");
        controllerRoot.transform.SetParent(transform);
        controllerRoot.transform.localPosition = Vector3.zero;
        controllerRoot.transform.localRotation = Quaternion.identity;
        
        // メイン本体部分（楕円形）
        GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mainBody.name = "MainBody";
        mainBody.transform.SetParent(controllerRoot.transform);
        mainBody.transform.localPosition = new Vector3(0, 0, 0.02f);
        mainBody.transform.localRotation = Quaternion.Euler(90, 0, 0);
        mainBody.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);
        DestroyImmediate(mainBody.GetComponent<Collider>());
        
        // トラッキングリング（Meta Quest特有の円形）
        GameObject trackingRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trackingRing.name = "TrackingRing";
        trackingRing.transform.SetParent(controllerRoot.transform);
        trackingRing.transform.localPosition = new Vector3(0, 0.08f, 0);
        trackingRing.transform.localScale = new Vector3(0.08f, 0.005f, 0.08f);
        DestroyImmediate(trackingRing.GetComponent<Collider>());
        
        // ボタン群部分
        GameObject buttonArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonArea.name = "ButtonArea";
        buttonArea.transform.SetParent(controllerRoot.transform);
        buttonArea.transform.localPosition = new Vector3(0, 0.03f, 0.02f);
        buttonArea.transform.localScale = new Vector3(0.025f, 0.015f, 0.03f);
        DestroyImmediate(buttonArea.GetComponent<Collider>());
        
        // Aボタン（右手）またはXボタン（左手）
        GameObject primaryButton = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        primaryButton.name = isLeftController ? "XButton" : "AButton";
        primaryButton.transform.SetParent(controllerRoot.transform);
        primaryButton.transform.localPosition = new Vector3(isLeftController ? -0.012f : 0.012f, 0.04f, 0.025f);
        primaryButton.transform.localScale = new Vector3(0.008f, 0.002f, 0.008f);
        DestroyImmediate(primaryButton.GetComponent<Collider>());
        
        // Bボタン（右手）またはYボタン（左手）
        GameObject secondaryButton = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        secondaryButton.name = isLeftController ? "YButton" : "BButton";
        secondaryButton.transform.SetParent(controllerRoot.transform);
        secondaryButton.transform.localPosition = new Vector3(isLeftController ? 0.012f : -0.012f, 0.04f, 0.015f);
        secondaryButton.transform.localScale = new Vector3(0.008f, 0.002f, 0.008f);
        DestroyImmediate(secondaryButton.GetComponent<Collider>());
        
        // サムスティック
        GameObject thumbstick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        thumbstick.name = "Thumbstick";
        thumbstick.transform.SetParent(controllerRoot.transform);
        thumbstick.transform.localPosition = new Vector3(0, 0.04f, -0.005f);
        thumbstick.transform.localScale = new Vector3(0.012f, 0.005f, 0.012f);
        DestroyImmediate(thumbstick.GetComponent<Collider>());
        
        // グリップボタン
        GameObject gripButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gripButton.name = "GripButton";
        gripButton.transform.SetParent(controllerRoot.transform);
        gripButton.transform.localPosition = new Vector3(isLeftController ? -0.02f : 0.02f, -0.01f, 0);
        gripButton.transform.localScale = new Vector3(0.015f, 0.03f, 0.01f);
        DestroyImmediate(gripButton.GetComponent<Collider>());
        
        // トリガー
        GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trigger.name = "Trigger";
        trigger.transform.SetParent(controllerRoot.transform);
        trigger.transform.localPosition = new Vector3(0, -0.01f, 0.035f);
        trigger.transform.localRotation = Quaternion.Euler(15, 0, 0);
        trigger.transform.localScale = new Vector3(0.015f, 0.025f, 0.008f);
        DestroyImmediate(trigger.GetComponent<Collider>());
        
        // マテリアル適用
        ApplyMetaQuestMaterials(controllerRoot);
        
        visualObject = controllerRoot;
    }
    
    void ApplyMetaQuestMaterials(GameObject root)
    {
        // Meta Quest風のマテリアル
        Material blackPlastic = new Material(Shader.Find("Standard"));
        blackPlastic.color = new Color(0.15f, 0.15f, 0.15f);
        blackPlastic.SetFloat("_Metallic", 0.1f);
        blackPlastic.SetFloat("_Glossiness", 0.4f);
        
        Material whitePlastic = new Material(Shader.Find("Standard"));
        whitePlastic.color = new Color(0.9f, 0.9f, 0.9f);
        whitePlastic.SetFloat("_Metallic", 0.1f);
        whitePlastic.SetFloat("_Glossiness", 0.6f);
        
        Material buttonMaterial = new Material(Shader.Find("Standard"));
        buttonMaterial.color = Color.black;
        buttonMaterial.SetFloat("_Metallic", 0.2f);
        buttonMaterial.SetFloat("_Glossiness", 0.3f);
        
        Material accentColor = new Material(Shader.Find("Standard"));
        accentColor.color = isLeftController ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.4f, 0.2f);
        accentColor.SetFloat("_Metallic", 0.3f);
        accentColor.SetFloat("_Glossiness", 0.7f);
        
        // 各パーツにマテリアルを適用
        Transform mainBody = root.transform.Find("MainBody");
        if (mainBody) mainBody.GetComponent<Renderer>().material = whitePlastic;
        
        Transform trackingRing = root.transform.Find("TrackingRing");
        if (trackingRing) trackingRing.GetComponent<Renderer>().material = blackPlastic;
        
        Transform buttonArea = root.transform.Find("ButtonArea");
        if (buttonArea) buttonArea.GetComponent<Renderer>().material = blackPlastic;
        
        Transform primaryButton = root.transform.Find(isLeftController ? "XButton" : "AButton");
        if (primaryButton) primaryButton.GetComponent<Renderer>().material = accentColor;
        
        Transform secondaryButton = root.transform.Find(isLeftController ? "YButton" : "BButton");
        if (secondaryButton) secondaryButton.GetComponent<Renderer>().material = accentColor;
        
        Transform thumbstick = root.transform.Find("Thumbstick");
        if (thumbstick) thumbstick.GetComponent<Renderer>().material = blackPlastic;
        
        Transform gripButton = root.transform.Find("GripButton");
        if (gripButton) gripButton.GetComponent<Renderer>().material = buttonMaterial;
        
        Transform trigger = root.transform.Find("Trigger");
        if (trigger) trigger.GetComponent<Renderer>().material = buttonMaterial;
    }
    
    void Update()
    {
        // 必要に応じてビジュアルを更新
        if (visualObject != null && actionController != null)
        {
            // トリガー押下時のビジュアルフィードバック
            if (actionController.selectAction.action != null && actionController.selectAction.action.IsPressed())
            {
                // トリガーが押されている時の処理（例：色を変える）
                UpdateTriggerVisual(true);
            }
            else
            {
                UpdateTriggerVisual(false);
            }
        }
    }
    
    void UpdateTriggerVisual(bool pressed)
    {
        if (visualObject == null) return;
        
        // トリガーオブジェクトを探す
        Transform trigger = visualObject.transform.Find("Trigger");
        if (trigger != null)
        {
            // トリガーの回転を変更（押下時）
            trigger.localRotation = pressed ? 
                Quaternion.Euler(30, 0, 0) : 
                Quaternion.Euler(15, 0, 0);
            
            Renderer triggerRenderer = trigger.GetComponent<Renderer>();
            if (triggerRenderer != null)
            {
                // 押されている時は色を変える
                triggerRenderer.material.color = pressed ? 
                    new Color(1f, 0.8f, 0.2f) : 
                    Color.black;
            }
        }
        
        // ボタンの色も変更
        Transform primaryButton = visualObject.transform.Find(isLeftController ? "XButton" : "AButton");
        if (primaryButton != null && pressed)
        {
            Renderer buttonRenderer = primaryButton.GetComponent<Renderer>();
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = Color.white;
            }
        }
    }
    
    void OnDestroy()
    {
        if (visualObject != null)
        {
            DestroyImmediate(visualObject);
        }
    }
    
    // エディタから呼び出し可能
    [ContextMenu("Recreate Visual")]
    public void RecreateVisual()
    {
        // 既存のビジュアルを削除
        if (visualObject != null)
        {
            DestroyImmediate(visualObject);
        }
        
        // 新しいビジュアルを作成
        CreateControllerVisual();
    }
}