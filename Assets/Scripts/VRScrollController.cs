using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

/// <summary>
/// VRコントローラーのスティック入力でScrollRectをスクロールするコンポーネント
/// </summary>
public class VRScrollController : MonoBehaviour
{
    [Header("スクロール設定")]
    [SerializeField] private float scrollSpeed = 2f; // スクロール速度
    [SerializeField] private float scrollDeadZone = 0.1f; // スティック入力のデッドゾーン
    
    [Header("コントローラー設定")]
    [SerializeField] private bool useLeftController = false; // 左コントローラーを使用
    [SerializeField] private bool useRightController = true; // 右コントローラーを使用
    
    private ScrollRect targetScrollRect;
    private XRNode leftControllerNode = XRNode.LeftHand;
    private XRNode rightControllerNode = XRNode.RightHand;
    
    void Start()
    {
        // 同じGameObjectのScrollRectを取得
        targetScrollRect = GetComponent<ScrollRect>();
        
        if (targetScrollRect == null)
        {
            // 子オブジェクトから探す
            targetScrollRect = GetComponentInChildren<ScrollRect>();
        }
        
        if (targetScrollRect == null)
        {
            Debug.LogWarning("[VRScrollController] ScrollRectが見つかりません");
            enabled = false;
            return;
        }
        
        Debug.Log($"[VRScrollController] ScrollRectを検出: {targetScrollRect.name}");
    }
    
    void Update()
    {
        if (targetScrollRect == null) return;
        
        float scrollInput = 0f;
        
        // 左コントローラーのスティック入力
        if (useLeftController)
        {
            Vector2 leftStickInput = GetStickInput(leftControllerNode);
            if (Mathf.Abs(leftStickInput.y) > scrollDeadZone)
            {
                scrollInput = leftStickInput.y;
            }
        }
        
        // 右コントローラーのスティック入力
        if (useRightController)
        {
            Vector2 rightStickInput = GetStickInput(rightControllerNode);
            if (Mathf.Abs(rightStickInput.y) > scrollDeadZone)
            {
                scrollInput = rightStickInput.y;
            }
        }
        
        // スクロール処理
        if (Mathf.Abs(scrollInput) > 0f)
        {
            // 現在のスクロール位置を取得
            float currentPos = targetScrollRect.verticalNormalizedPosition;
            
            // スクロール速度を計算（Time.deltaTimeで速度を調整）
            float scrollDelta = scrollInput * scrollSpeed * Time.deltaTime;
            
            // 新しいスクロール位置を設定（0-1の範囲にクランプ）
            float newPos = Mathf.Clamp01(currentPos + scrollDelta);
            
            // スクロール位置を更新
            targetScrollRect.verticalNormalizedPosition = newPos;
            
            // デバッグ出力（頻度を下げる）
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[VRScrollController] スティック入力: {scrollInput:F2}, スクロール位置: {newPos:F2}");
            }
        }
    }
    
    /// <summary>
    /// 指定されたコントローラーのスティック入力を取得
    /// </summary>
    private Vector2 GetStickInput(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        
        if (device.isValid)
        {
            Vector2 stickValue;
            
            // プライマリ2D軸（通常はサムスティック）の値を取得
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out stickValue))
            {
                return stickValue;
            }
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// スクロール速度を設定
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = Mathf.Max(0.1f, speed);
    }
    
    /// <summary>
    /// デッドゾーンを設定
    /// </summary>
    public void SetDeadZone(float deadZone)
    {
        scrollDeadZone = Mathf.Clamp(deadZone, 0f, 0.9f);
    }
    
    /// <summary>
    /// 使用するコントローラーを設定
    /// </summary>
    public void SetControllerUsage(bool useLeft, bool useRight)
    {
        useLeftController = useLeft;
        useRightController = useRight;
    }
}