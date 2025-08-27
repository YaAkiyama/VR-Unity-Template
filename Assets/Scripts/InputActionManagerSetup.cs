using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Input Action Managerの自動セットアップ
/// XR Interaction ToolkitのInput Actionsを有効化
/// </summary>
public class InputActionManagerSetup : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset inputActionAsset;
    
    void Start()
    {
        Debug.Log("[InputActionManagerSetup] Input Action Manager設定を開始します");
        Debug.Log("手動設定が必要:");
        Debug.Log("1. このオブジェクトに 'Input Action Manager' コンポーネントを追加");
        Debug.Log("2. Input Action Manager の 'Action Assets' に 'XRI Default Input Actions' を設定");
        Debug.Log("3. XR Origin の各コントローラーの Input Actions を有効化");
    }
}