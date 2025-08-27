using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VRコントローラーの可視化とレーザーポインター機能
/// VR_Viewerプロジェクトベースの実装
/// </summary>
public class VRControllerLaser : MonoBehaviour
{
    [Header("コントローラー表示")]
    [SerializeField] private bool showController = true;
    [SerializeField] private GameObject controllerModel;
    [SerializeField] private Transform controllerVisual;
    
    [Header("レーザー設定")]
    [SerializeField] private bool enableLaser = true;
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float laserMaxDistance = 10f;
    [SerializeField] private float laserWidth = 0.002f;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private Material laserMaterial;
    
    [Header("ポインター")]
    [SerializeField] private GameObject pointerDot;
    [SerializeField] private float pointerDotSize = 0.05f;
    [SerializeField] private Color pointerColor = Color.red;
    
    [Header("入力設定")]
    [SerializeField] private InputActionProperty selectAction;
    [SerializeField] private bool isLeftController = false;
    
    [Header("デバッグ")]
    [SerializeField] private bool debugMode = true;
    
    private ActionBasedController actionController;
    private Camera vrCamera;
    private GraphicRaycaster graphicRaycaster;
    private Canvas targetCanvas;
    private bool isPointing = false;
    private GameObject lastHitObject;
    private Button currentButton;
    
    void Start()
    {
        SetupController();
        SetupLaser();
        SetupPointerDot();
        FindVRCamera();
    }
    
    void SetupController()
    {
        // ActionBasedControllerを取得
        actionController = GetComponent<ActionBasedController>();
        if (actionController == null)
        {
            actionController = gameObject.AddComponent<ActionBasedController>();
        }
        
        // コントローラーの可視化
        if (showController && controllerVisual == null)
        {
            // シンプルな球体でコントローラーを表現
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.1f;
            
            // マテリアル設定
            Renderer renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = isLeftController ? Color.blue : Color.red;
            
            controllerVisual = sphere.transform;
            
            Log($"{(isLeftController ? "左手" : "右手")}コントローラーの可視化を作成しました");
        }
    }
    
    void SetupLaser()
    {
        if (!enableLaser) return;
        
        if (laserLine == null)
        {
            // LineRendererを作成
            GameObject laserGO = new GameObject("LaserLine");
            laserGO.transform.SetParent(transform);
            laserGO.transform.localPosition = Vector3.zero;
            
            laserLine = laserGO.AddComponent<LineRenderer>();
        }
        
        // LineRendererの設定
        laserLine.material = laserMaterial != null ? laserMaterial : CreateDefaultLaserMaterial();
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth * 0.5f;
        laserLine.positionCount = 2;
        laserLine.useWorldSpace = true;
        laserLine.sortingOrder = 100;
        
        // 最初は非表示
        laserLine.enabled = false;
        
        Log("レーザーポインターを設定しました");
    }
    
    void SetupPointerDot()
    {
        if (pointerDot == null)
        {
            // ポインタードット作成
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerDot.name = "PointerDot";
            pointerDot.transform.localScale = Vector3.one * pointerDotSize;
            
            // Colliderを削除（不要）
            Collider dotCollider = pointerDot.GetComponent<Collider>();
            if (dotCollider) DestroyImmediate(dotCollider);
            
            // マテリアル設定
            Renderer dotRenderer = pointerDot.GetComponent<Renderer>();
            dotRenderer.material.color = pointerColor;
            dotRenderer.material.shader = Shader.Find("Unlit/Color");
            
            // 最初は非表示
            pointerDot.SetActive(false);
            
            Log("ポインタードットを作成しました");
        }
    }
    
    void FindVRCamera()
    {
        // VRカメラを検索
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            vrCamera = FindObjectOfType<Camera>();
        }
        
        // UIキャンバスを検索
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                targetCanvas = canvas;
                graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                {
                    graphicRaycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                break;
            }
        }
        
        Log($"VRカメラ: {(vrCamera ? "発見" : "未発見")}, UIキャンバス: {(targetCanvas ? "発見" : "未発見")}");
    }
    
    Material CreateDefaultLaserMaterial()
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = laserColor;
        return mat;
    }
    
    void Update()
    {
        if (enableLaser)
        {
            UpdateLaser();
        }
        
        HandleInput();
    }
    
    void UpdateLaser()
    {
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;
        
        // UIとの交差判定
        bool hitUI = CheckUIIntersection(rayOrigin, rayDirection, out Vector3 hitPoint, out GameObject hitObject);
        
        if (!hitUI)
        {
            // 物理オブジェクトとの交差判定
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, laserMaxDistance))
            {
                hitPoint = hit.point;
                hitObject = hit.collider.gameObject;
                hitUI = true;
            }
            else
            {
                // 何もヒットしない場合
                hitPoint = rayOrigin + rayDirection * laserMaxDistance;
                hitObject = null;
            }
        }
        
        // レーザー表示
        if (laserLine != null)
        {
            laserLine.enabled = hitUI || isPointing;
            laserLine.SetPosition(0, rayOrigin);
            laserLine.SetPosition(1, hitPoint);
        }
        
        // ポインタードット表示
        if (pointerDot != null)
        {
            pointerDot.SetActive(hitUI);
            if (hitUI)
            {
                pointerDot.transform.position = hitPoint;
            }
        }
        
        // UI要素のハイライト
        HandleUIHighlight(hitObject);
    }
    
    bool CheckUIIntersection(Vector3 rayOrigin, Vector3 rayDirection, out Vector3 hitPoint, out GameObject hitObject)
    {
        hitPoint = Vector3.zero;
        hitObject = null;
        
        if (targetCanvas == null || graphicRaycaster == null) return false;
        
        // スクリーン座標への変換
        Vector3 screenPoint = vrCamera.WorldToScreenPoint(rayOrigin + rayDirection * 5f);
        
        // GraphicRaycasterでUIとの交差判定
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(screenPoint.x, screenPoint.y),
            delta = Vector2.zero,
            scrollDelta = Vector2.zero,
            pointerId = -1,
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, raycastResults);
        
        if (raycastResults.Count > 0)
        {
            RaycastResult result = raycastResults[0];
            hitObject = result.gameObject;
            
            // ワールド座標での交差点を計算
            RectTransform rectTransform = result.gameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] worldCorners = new Vector3[4];
                rectTransform.GetWorldCorners(worldCorners);
                
                // 簡単な平面交差計算
                Vector3 planeNormal = targetCanvas.transform.forward;
                Vector3 planePoint = worldCorners[0];
                
                if (IntersectRayPlane(rayOrigin, rayDirection, planePoint, planeNormal, out float distance))
                {
                    hitPoint = rayOrigin + rayDirection * distance;
                    return true;
                }
            }
        }
        
        return false;
    }
    
    bool IntersectRayPlane(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal, out float distance)
    {
        distance = 0f;
        float denominator = Vector3.Dot(planeNormal, rayDirection);
        
        if (Mathf.Abs(denominator) < 0.0001f)
            return false; // 平行
        
        Vector3 diff = planePoint - rayOrigin;
        distance = Vector3.Dot(diff, planeNormal) / denominator;
        
        return distance >= 0;
    }
    
    void HandleUIHighlight(GameObject hitObject)
    {
        // 前回のボタンのハイライト解除
        if (currentButton != null && currentButton.gameObject != hitObject)
        {
            // ボタンの通常状態に戻す処理
            currentButton = null;
        }
        
        // 新しいボタンのハイライト
        if (hitObject != null)
        {
            Button button = hitObject.GetComponent<Button>();
            if (button != null && button != currentButton)
            {
                currentButton = button;
                // ボタンのハイライト処理
            }
        }
        
        lastHitObject = hitObject;
    }
    
    void HandleInput()
    {
        // トリガー入力の処理
        bool triggerPressed = false;
        
        if (selectAction.action != null)
        {
            triggerPressed = selectAction.action.IsPressed();
        }
        
        if (triggerPressed && !isPointing)
        {
            isPointing = true;
            OnTriggerPress();
        }
        else if (!triggerPressed && isPointing)
        {
            isPointing = false;
            OnTriggerRelease();
        }
    }
    
    void OnTriggerPress()
    {
        if (currentButton != null)
        {
            Log($"ボタンを押下: {currentButton.name}");
            currentButton.onClick.Invoke();
        }
    }
    
    void OnTriggerRelease()
    {
        // トリガーリリース処理
    }
    
    void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[VRControllerLaser:{(isLeftController ? "L" : "R")}] {message}");
        }
    }
    
    void OnDestroy()
    {
        // クリーンアップ
        if (pointerDot != null)
        {
            DestroyImmediate(pointerDot);
        }
    }
    
    // インスペクターから呼び出し可能
    [ContextMenu("Setup Laser System")]
    public void SetupLaserSystem()
    {
        SetupController();
        SetupLaser();
        SetupPointerDot();
        FindVRCamera();
        Log("レーザーシステムのセットアップが完了しました");
    }
}