using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// VRコントローラー用レーザーポインター
/// 両手のコントローラーからレーザーを発射し、UIパネルとの交差点にドットを表示
/// コントローラーの可視化も含む
/// </summary>
public class VRLaserPointerSimple : MonoBehaviour
{
    [Header("コントローラー可視化")]
    [SerializeField] private bool showController = true;
    [SerializeField] private bool isLeftController = false;
    
    [Header("レーザー設定")]
    [SerializeField] private float laserMaxLength = 10f; // レイキャスト検出用の最大距離
    [SerializeField] private float laserVisualLength = 1.5f; // レーザー表示用の長さ
    [SerializeField] private float laserStartWidth = 0.008f; // レーザー起点の太さ
    [SerializeField] private float laserEndWidth = 0.002f; // レーザー終点の太さ
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject dotPrefab;
    
    [Header("レーザーの色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.cyan;
    
    [Header("ドット設定")]
    [SerializeField] private float dotSize = 0.02f; // 2cm（より小さく、精密に）
    
    // レーザーとドットのインスタンス
    private LineRenderer laserLine;
    private GameObject dotObject;
    private GameObject controllerVisual;
    private bool isHovering = false;
    
    // Input System
    public InputActionProperty triggerAction;
    public InputActionProperty gripAction;
    
    // レイキャスト用
    private RaycastHit hit;
    private GameObject currentHitObject;
    private bool triggerPressed = false;
    
    void Start()
    {
        Debug.Log($"[VRLaserPointer] Start() 開始 - Controller: {(isLeftController ? "Left" : "Right")}");
        
        // コントローラーの可視化作成
        CreateControllerVisual();
        
        // レーザーラインの作成
        CreateLaserLine();
        
        // ドットの作成
        CreateDot();
        
        // 初期状態では非表示
        if (laserLine != null)
            laserLine.enabled = false;
        if (dotObject != null)
            dotObject.SetActive(false);
            
        // InputAction設定をデバッグ
        DebugInputActionSettings();
        
        // デバッグ用：Scene内のすべてのColliderを列挙
        Invoke("DebugAllColliders", 3f); // 3秒後に実行
        
        Debug.Log($"[VRLaserPointer] Start() 完了 - Position: {transform.position}");
    }
    
    void DebugInputActionSettings()
    {
        Debug.Log($"[VRInputSetup] ==== InputAction設定チェック開始 ====");
        Debug.Log($"[VRInputSetup] コントローラー: {(isLeftController ? "左手" : "右手")}");
        
        if (triggerAction.action != null)
        {
            Debug.Log($"[VRInputSetup] TriggerAction設定済み: {triggerAction.action.name}");
            Debug.Log($"[VRInputSetup] TriggerAction有効: {triggerAction.action.enabled}");
            Debug.Log($"[VRInputSetup] TriggerActionバインディング: {triggerAction.action.bindings.Count}個");
            
            for (int i = 0; i < triggerAction.action.bindings.Count; i++)
            {
                var binding = triggerAction.action.bindings[i];
                Debug.Log($"[VRInputSetup] Trigger Binding[{i}]: Path={binding.path}");
            }
            
            // 初期値テスト
            float initialValue = triggerAction.action.ReadValue<float>();
            Debug.Log($"[VRInputSetup] Trigger初期値: {initialValue:F3}");
        }
        else
        {
            Debug.LogError("[VRInputSetup] TriggerActionが未設定です！");
        }
        
        if (gripAction.action != null)
        {
            Debug.Log($"[VRInputSetup] GripAction設定済み: {gripAction.action.name}");
            Debug.Log($"[VRInputSetup] GripAction有効: {gripAction.action.enabled}");
            Debug.Log($"[VRInputSetup] GripActionバインディング: {gripAction.action.bindings.Count}個");
            
            for (int i = 0; i < gripAction.action.bindings.Count; i++)
            {
                var binding = gripAction.action.bindings[i];
                Debug.Log($"[VRInputSetup] Grip Binding[{i}]: Path={binding.path}");
            }
            
            // 初期値テスト
            float initialValue = gripAction.action.ReadValue<float>();
            Debug.Log($"[VRInputSetup] Grip初期値: {initialValue:F3}");
        }
        else
        {
            Debug.LogWarning("[VRInputSetup] GripActionが未設定です");
        }
        
        Debug.Log($"[VRInputSetup] ==== InputAction設定チェック完了 ====");
    }
    
    void DebugAllColliders()
    {
        Collider[] allColliders = FindObjectsOfType<Collider>();
        Debug.Log($"[VRLaserPointer] Scene内のCollider数: {allColliders.Length}");
        
        // 背景パネルを探す
        GameObject backgroundPanel = GameObject.Find("BackgroundPanel");
        if (backgroundPanel != null)
        {
            BoxCollider bgCollider = backgroundPanel.GetComponent<BoxCollider>();
            if (bgCollider != null)
            {
                Debug.Log($"[VRLaserPointer] 背景パネル発見! Position: {backgroundPanel.transform.position}, " +
                         $"Bounds: {bgCollider.bounds}, Size: {bgCollider.size}, " +
                         $"Center: {bgCollider.center}, Enabled: {bgCollider.enabled}");
            }
            else
            {
                Debug.LogWarning("[VRLaserPointer] 背景パネルにコライダーがありません！");
            }
        }
        else
        {
            Debug.LogWarning("[VRLaserPointer] 背景パネルが見つかりません！");
        }
        
        // UIパネルのコライダー詳細
        foreach (Collider col in allColliders)
        {
            if (col.gameObject.name.Contains("Button") || col.gameObject.name.Contains("Background"))
            {
                Debug.Log($"[VRLaserPointer] UIコライダー: {col.gameObject.name}, " +
                         $"Position: {col.transform.position}, Bounds: {col.bounds}, " +
                         $"Size: {col.bounds.size}, Layer: {col.gameObject.layer}");
            }
        }
    }
    
    void CreateControllerVisual()
    {
        // VRControllerVisualizerが既に存在する場合はスキップ
        if (!showController) return;
        
        // 既にVRControllerVisualizerコンポーネントがある場合は何もしない
        VRControllerVisualizer existingVisualizer = GetComponent<VRControllerVisualizer>();
        if (existingVisualizer != null)
        {
            Debug.Log($"VRControllerVisualizerが既に存在するため、追加のビジュアル作成をスキップします");
            return;
        }
        
        // VRControllerVisualizerがない場合のみシンプルな球体を作成
        controllerVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        controllerVisual.name = $"SimpleController_{(isLeftController ? "Left" : "Right")}";
        controllerVisual.transform.SetParent(transform);
        controllerVisual.transform.localPosition = Vector3.zero;
        controllerVisual.transform.localScale = Vector3.one * 0.08f; // 8cm
        
        // コライダーを無効化
        Collider controllerCollider = controllerVisual.GetComponent<Collider>();
        if (controllerCollider != null)
            controllerCollider.enabled = false;
        
        // マテリアル設定
        Renderer controllerRenderer = controllerVisual.GetComponent<Renderer>();
        if (controllerRenderer != null)
        {
            Material controllerMat = new Material(Shader.Find("Standard"));
            controllerMat.color = isLeftController ? Color.blue : Color.red;
            // StandardシェーダーのプロパティをSetFloatで設定
            controllerMat.SetFloat("_Metallic", 0.3f);
            controllerMat.SetFloat("_Glossiness", 0.7f);
            controllerRenderer.material = controllerMat;
        }
        
        Debug.Log($"{(isLeftController ? "左手" : "右手")}シンプルコントローラーを可視化しました");
    }
    
    void CreateLaserLine()
    {
        // LineRendererコンポーネントを追加
        GameObject laserGO = new GameObject("LaserLine");
        laserGO.transform.SetParent(transform);
        laserGO.transform.localPosition = Vector3.zero;
        laserGO.transform.localRotation = Quaternion.identity;
        
        laserLine = laserGO.AddComponent<LineRenderer>();
        laserLine.startWidth = laserStartWidth; // 起点の太さ
        laserLine.endWidth = laserEndWidth;     // 終点の太さ - 先細り効果
        laserLine.positionCount = 2;
        
        // より滑らかな先細り効果のため、追加設定
        laserLine.useWorldSpace = true;
        laserLine.sortingOrder = 1000; // 他のオブジェクトより前面に表示
        
        // マテリアルの設定（発光マテリアル）
        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null)
        {
            // フォールバック：Standard Unlit
            unlitShader = Shader.Find("Standard");
            Debug.LogWarning("[VRLaserPointer] Unlit/Colorシェーダーが見つからないため、Standardを使用します");
        }
        
        if (unlitShader != null)
        {
            Material laserMat = new Material(unlitShader);
            laserMat.color = normalColor;
            
            // 先細りレーザーの視認性向上
            if (unlitShader.name == "Standard")
            {
                laserMat.EnableKeyword("_EMISSION");
                laserMat.SetColor("_EmissionColor", normalColor * 1.5f);
            }
            
            laserLine.material = laserMat;
        }
        else
        {
            Debug.LogError("[VRLaserPointer] 利用可能なシェーダーが見つかりません");
        }
        laserLine.enabled = false;
    }
    
    void CreateDot()
    {
        // ドットオブジェクトの作成（球体）
        dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotObject.name = "LaserDot";
        dotObject.transform.localScale = Vector3.one * dotSize;
        
        // コライダーを無効化（ドット自体はインタラクション不可）
        Collider dotCollider = dotObject.GetComponent<Collider>();
        if (dotCollider != null)
            dotCollider.enabled = false;
        
        // マテリアルの設定（明るい発光マテリアル）
        Renderer dotRenderer = dotObject.GetComponent<Renderer>();
        if (dotRenderer != null)
        {
            Material dotMat = new Material(Shader.Find("Standard"));
            dotMat.color = Color.white;
            dotMat.EnableKeyword("_EMISSION");
            dotMat.SetColor("_EmissionColor", Color.white * 2f); // より明るい発光
            dotMat.SetFloat("_Metallic", 0f);
            dotMat.SetFloat("_Glossiness", 1f);
            dotRenderer.material = dotMat;
        }
        
        dotObject.SetActive(false);
    }
    
    void Update()
    {
        // 5秒ごとにUpdate動作確認
        if (Time.time % 5f < Time.deltaTime)
        {
            Debug.Log($"[VRLaserPointer] Update動作中 - Controller: {(isLeftController ? "Left" : "Right")}, Position: {transform.position}");
        }
        
        UpdateLaser();
        HandleInput();
        
        // デバッグ：トリガーとグリップの値を監視（HandleInput内で実行するため不要）
        // DebugInputValues();
    }
    
    void DebugInputValues()
    {
        float triggerVal = 0f;
        float gripVal = 0f;
        
        if (triggerAction.action != null && triggerAction.action.enabled)
        {
            triggerVal = triggerAction.action.ReadValue<float>();
        }
        
        if (gripAction.action != null && gripAction.action.enabled)
        {
            gripVal = gripAction.action.ReadValue<float>();
        }
        
        if (triggerVal > 0.01f || gripVal > 0.01f)
        {
            Debug.Log($"[VRInput] Trigger={triggerVal:F3}, Grip={gripVal:F3}");
        }
    }
    
    void UpdateLaser()
    {
        // レーザーの原点
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        
        // デバッグ用：5秒ごとにレイキャスト情報をログ出力
        if (Time.time % 5f < Time.deltaTime)
        {
            Debug.Log($"[VRLaserPointer] Origin: {origin}, Direction: {direction}, MaxLength: {laserMaxLength}");
            
            // Unity Editor用：Scene Viewでレイを表示
            #if UNITY_EDITOR
            Debug.DrawRay(origin, direction * laserMaxLength, Color.red, 1f);
            #endif
        }
        
        // レイキャスト実行（複数のヒットを取得）
        int layerMask = ~0; // すべてのレイヤーをチェック
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, laserMaxLength, layerMask);
        
        bool hitSomething = false;
        RaycastHit? closestButtonHit = null;
        RaycastHit? closestBackgroundHit = null;
        float closestButtonDistance = float.MaxValue;
        float closestBackgroundDistance = float.MaxValue;
        
        // 全ヒット対象をチェック
        foreach (RaycastHit rayHit in hits)
        {
            GameObject hitObj = rayHit.collider.gameObject;
            
            // ボタンかどうかを判定（複数の方法で確認）
            bool isButton = hitObj.name.Contains("Button") || 
                           hitObj.GetComponent<UnityEngine.UI.Button>() != null ||
                           hitObj.GetComponentInParent<UnityEngine.UI.Button>() != null;
            
            if (isButton && rayHit.distance < closestButtonDistance)
            {
                closestButtonHit = rayHit;
                closestButtonDistance = rayHit.distance;
            }
            // 背景パネルかどうかを判定
            else if (hitObj.name == "BackgroundPanel" && rayHit.distance < closestBackgroundDistance)
            {
                closestBackgroundHit = rayHit;
                closestBackgroundDistance = rayHit.distance;
            }
        }
        
        // ボタンが検出された場合は必ず優先
        if (closestButtonHit.HasValue)
        {
            hit = closestButtonHit.Value;
            hitSomething = true;
            Debug.Log($"[VRLaserPointer] ボタン最優先選択: {hit.collider.gameObject.name}, 距離: {hit.distance:F3}");
        }
        // ボタンがない場合のみ背景パネルを選択
        else if (closestBackgroundHit.HasValue)
        {
            hit = closestBackgroundHit.Value;
            hitSomething = true;
            Debug.Log($"[VRLaserPointer] 背景パネル選択: 距離: {hit.distance:F3}");
        }
        // どちらでもない場合は最初のヒット
        else if (hits.Length > 0)
        {
            hit = hits[0];
            hitSomething = true;
            Debug.Log($"[VRLaserPointer] その他オブジェクト選択: {hit.collider.gameObject.name}");
        }
        
        // デバッグ用：ヒット結果をログ出力
        if (Time.time % 3f < Time.deltaTime) // 3秒ごとにログ出力
        {
            Debug.Log($"[VRLaserPointer] Hits found: {hits.Length}, Controller: {(isLeftController ? "Left" : "Right")}");
            if (hitSomething)
            {
                Debug.Log($"[VRLaserPointer] Selected HitObject: {hit.collider.gameObject.name}, Distance: {hit.distance:F2}m");
                if (closestButtonHit.HasValue) Debug.Log("[VRLaserPointer] ボタン優先選択");
                if (closestBackgroundHit.HasValue) Debug.Log("[VRLaserPointer] 背景パネル検出");
            }
            else
            {
                Debug.Log($"[VRLaserPointer] 何にもヒットしていません - Origin: {origin}, Direction: {direction}");
            }
        }
        
        // レーザー表示用の終点（常に固定長）
        Vector3 laserEndPoint = origin + direction * laserVisualLength;
        
        // レーザーラインの描画（常に固定長で表示）
        if (laserLine != null)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, origin);
            laserLine.SetPosition(1, laserEndPoint);
        }
        
        if (hitSomething)
        {
            // ヒットした場合 - ドットを表示
            Vector3 hitPoint = hit.point;
            
            // ドットの表示（ヒット地点に関係なく表示）
            if (dotObject != null)
            {
                dotObject.SetActive(true);
                dotObject.transform.position = hitPoint;
                
                // ヒット面の法線方向に少しオフセット（Z-fighting回避）
                dotObject.transform.position += hit.normal * 0.002f;
                
                // ドットをヒット面に向ける
                dotObject.transform.LookAt(hitPoint + hit.normal);
                
                Debug.Log($"[VRLaserPointer] ドット表示: Position={hitPoint}, Active={dotObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("[VRLaserPointer] dotObjectがnullです");
            }
            
            // ヒットしたオブジェクトをチェック
            GameObject hitObj = hit.collider.gameObject;
            
            // ボタンかどうかを判定（統一された方法で）
            bool isButton = hitObj.name.Contains("Button") || 
                           hitObj.GetComponent<UnityEngine.UI.Button>() != null ||
                           hitObj.GetComponentInParent<UnityEngine.UI.Button>() != null;
            
            // 背景パネルかどうかを判定
            bool isBackgroundPanel = hitObj.name == "BackgroundPanel";
            
            if (isButton)
            {
                // ボタンにヒット - 赤色表示
                SetHoverState(true, true); // hovering=true, isButton=true
                
                // ボタンコンポーネントを取得
                UnityEngine.UI.Button button = hitObj.GetComponent<UnityEngine.UI.Button>();
                if (button == null)
                {
                    button = hitObj.GetComponentInParent<UnityEngine.UI.Button>();
                }
                
                currentHitObject = button != null ? button.gameObject : hitObj;
                Debug.Log($"[VRLaserPointer] ボタンにヒット（赤色）: {hitObj.name} at {hitPoint}");
            }
            else if (isBackgroundPanel)
            {
                // 背景パネルにヒット - 白色表示
                SetHoverState(true, false); // hovering=true, isButton=false
                currentHitObject = hitObj;
                Debug.Log($"[VRLaserPointer] 背景パネルにヒット（白色）: {hitObj.name} at {hitPoint}");
            }
            else
            {
                // その他のオブジェクトにヒット
                SetHoverState(false, false);
                currentHitObject = hitObj;
                Debug.Log($"[VRLaserPointer] その他オブジェクトにヒット: {hitObj.name} at {hitPoint}");
            }
        }
        else
        {
            // 何もヒットしない場合、ドットは非表示
            if (dotObject != null)
                dotObject.SetActive(false);
            
            SetHoverState(false, false);
            currentHitObject = null;
        }
    }
    
    void SetHoverState(bool hovering, bool isButton = false)
    {
        isHovering = hovering;
        
        // レーザーの色設定：ボタン上では赤、パネル上ではシアン、通常時は白
        Color laserColor;
        if (isButton)
        {
            laserColor = Color.red; // ボタン上は赤色
        }
        else if (hovering)
        {
            laserColor = hoverColor; // パネル上はシアン
        }
        else
        {
            laserColor = normalColor; // 通常時は白
        }
        
        // ドットの色設定：ボタン上では赤、それ以外（パネル・通常）は白
        Color dotColor;
        if (isButton)
        {
            dotColor = Color.red; // ボタン上は赤色
        }
        else
        {
            dotColor = Color.white; // パネル・通常時は白色
        }
        
        if (laserLine != null && laserLine.material != null)
        {
            laserLine.material.color = laserColor;
        }
        
        if (dotObject != null)
        {
            Renderer dotRenderer = dotObject.GetComponent<Renderer>();
            if (dotRenderer != null && dotRenderer.material != null)
            {
                // 実機でのマテリアル変更を確実にするため、複数の方法で設定
                dotRenderer.material.color = dotColor; // 基本色をドット色に設定
                dotRenderer.material.SetColor("_EmissionColor", dotColor * 2f); // 発光色もドット色に設定
                
                // Standard シェーダーの場合の追加設定
                if (dotRenderer.material.HasProperty("_Color"))
                {
                    dotRenderer.material.SetColor("_Color", dotColor);
                }
                
                Debug.Log($"[VRLaserPointer] ドットマテリアル変更: Color={dotColor}, Emission={dotColor * 2f}");
            }
            else
            {
                Debug.LogWarning("[VRLaserPointer] ドットのRenderer又はMaterialが見つかりません");
            }
        }
        
        // デバッグログ
        string laserState = isButton ? "ボタン(レーザー赤)" : hovering ? "パネル(レーザーシアン)" : "通常(レーザー白)";
        string dotState = isButton ? "ドット赤" : "ドット白";
        Debug.Log($"[VRLaserPointer] ホバー状態変更: {laserState}, {dotState}");
    }
    
    void HandleInput()
    {
        // トリガー入力の処理（シンプルにトリガーのみ使用）
        if (triggerAction.action != null && triggerAction.action.enabled)
        {
            float triggerValue = triggerAction.action.ReadValue<float>();
            bool currentPressed = triggerValue > 0.5f;
            
            // デバッグ：トリガー値を出力
            if (triggerValue > 0.01f || currentPressed != triggerPressed)
            {
                Debug.Log($"[VRInput] Trigger値: {triggerValue:F3}, 押下状態: {currentPressed}");
            }
            
            // トリガー状態が変化した場合レーザーの色を更新
            if (currentPressed != triggerPressed)
            {
                Debug.Log($"[VRInput] トリガー状態変化: {triggerPressed} → {currentPressed}");
                UpdateLaserColorForTrigger(currentPressed);
            }
            
            // トリガーが押された瞬間（立ち上がりエッジ）を検出
            if (currentPressed && !triggerPressed && currentHitObject != null)
            {
                Debug.Log($"[VRButtonClick] 入力検出: isHovering={isHovering}, currentHitObject={currentHitObject.name}");
                
                // 赤いドット（ボタンホバー中）の場合のみボタンクリックを実行
                if (isHovering && currentHitObject != null)
                {
                    ProcessButtonClick();
                }
                else
                {
                    Debug.LogWarning($"[VRButtonClick] ホバー状態ではない: isHovering={isHovering}");
                }
            }
            
            triggerPressed = currentPressed;
        }
        else
        {
            Debug.LogError("[VRInput] TriggerActionが設定されていません");
        }
    }
    
    void ProcessButtonClick()
    {
        // UIボタンコンポーネントを複数の方法で検索
        UnityEngine.UI.Button button = currentHitObject.GetComponent<UnityEngine.UI.Button>();
        
        // 直接見つからない場合は親から検索
        if (button == null)
        {
            button = currentHitObject.GetComponentInParent<UnityEngine.UI.Button>();
        }
        
        // それでも見つからない場合は子から検索
        if (button == null)
        {
            button = currentHitObject.GetComponentInChildren<UnityEngine.UI.Button>();
        }
        
        // ボタン名でも確認（UISetup.csで作成したボタン）
        if (button == null && currentHitObject.name.Contains("Button"))
        {
            // ボタン名を含むオブジェクトから親のボタンを検索
            Transform parent = currentHitObject.transform.parent;
            while (parent != null && button == null)
            {
                button = parent.GetComponent<UnityEngine.UI.Button>();
                parent = parent.parent;
            }
        }
        
        if (button != null && button.interactable)
        {
            Debug.Log($"[VRButtonClick] ボタンクリック実行: {button.name}");
            
            // ボタンクリックの視覚フィードバック
            StartCoroutine(ButtonClickFeedback(button));
            
            // ボタンアクションを実行
            button.onClick.Invoke();
        }
        else if (button != null && !button.interactable)
        {
            Debug.LogWarning($"[VRButtonClick] ボタンは無効状態です: {button.name}");
        }
        else
        {
            Debug.LogWarning($"[VRButtonClick] ボタンコンポーネントが見つかりません: {currentHitObject.name}");
        }
    }
    
    /// <summary>
    /// トリガー押下時のレーザー色変更
    /// </summary>
    void UpdateLaserColorForTrigger(bool triggerPressed)
    {
        if (laserLine != null && laserLine.material != null)
        {
            Color targetColor;
            
            if (triggerPressed)
            {
                // トリガー押下中は黄色
                targetColor = Color.yellow;
                Debug.Log("[VRLaserColor] 押下: 黄色");
            }
            else
            {
                // トリガーが離されたら元の色に戻す
                if (isHovering)
                {
                    // ボタンホバー中の場合は赤
                    targetColor = hoverColor;
                    Debug.Log("[VRLaserColor] 離す: 赤（ホバー中）");
                }
                else
                {
                    // 通常状態は白
                    targetColor = normalColor;
                    Debug.Log("[VRLaserColor] 離す: 白（通常）");
                }
            }
            
            // レーザーの色を更新
            laserLine.material.color = targetColor;
            
            // エミッション効果も更新（可能な場合）
            if (laserLine.material.HasProperty("_EmissionColor"))
            {
                laserLine.material.SetColor("_EmissionColor", targetColor * 1.5f);
            }
        }
    }
    
    /// <summary>
    /// ボタンクリック時の視覚フィードバック
    /// </summary>
    IEnumerator ButtonClickFeedback(UnityEngine.UI.Button button)
    {
        // ドットを一瞬大きくして押下感を演出
        if (dotObject != null)
        {
            Vector3 originalScale = dotObject.transform.localScale;
            Vector3 pressedScale = originalScale * 1.5f;
            
            // 大きくする
            dotObject.transform.localScale = pressedScale;
            
            // 色を一瞬黄色に
            Renderer dotRenderer = dotObject.GetComponent<Renderer>();
            if (dotRenderer != null)
            {
                Color originalColor = dotRenderer.material.color;
                dotRenderer.material.color = Color.yellow;
                dotRenderer.material.SetColor("_EmissionColor", Color.yellow * 3f);
                
                yield return new WaitForSeconds(0.1f);
                
                // 元に戻す
                dotRenderer.material.color = originalColor;
                dotRenderer.material.SetColor("_EmissionColor", originalColor * 2f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // サイズを元に戻す
            dotObject.transform.localScale = originalScale;
        }
    }
    
    void OnDestroy()
    {
        // クリーンアップ
        if (laserLine != null)
            Destroy(laserLine.gameObject);
        if (dotObject != null)
            Destroy(dotObject);
        if (controllerVisual != null)
            Destroy(controllerVisual);
    }
    
    // インスペクターから呼び出し可能
    [ContextMenu("Setup Laser System")]
    public void SetupLaserSystem()
    {
        CreateControllerVisual();
        CreateLaserLine();
        CreateDot();
        Debug.Log("レーザーシステムのセットアップが完了しました");
    }
}