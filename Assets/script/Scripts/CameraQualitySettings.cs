using UnityEngine;

// 此脚本应挂载在场景中的主摄像机(Main Camera)上
[RequireComponent(typeof(Camera))]
public class CameraQualitySettings : MonoBehaviour
{
    [Header("抗锯齿设置")]
    public bool enableAntiAliasing = true;
    public int antiAliasingLevel = 8; // 2, 4 或 8

    [Header("边缘增强")]
    public bool enableEdgeEnhancement = true;
    
    private Camera mainCamera;
    
    void Start()
    {
        // 获取摄像机引用
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("无法找到Camera组件！");
            return;
        }
        
        ApplyQualitySettings();
    }
    
    void ApplyQualitySettings()
    {
        // 设置全局抗锯齿
        if (enableAntiAliasing)
        {
            QualitySettings.antiAliasing = antiAliasingLevel;
        }
        
        // 应用摄像机特定设置
        if (mainCamera != null)
        {
            // 启用HDR以改善图像质量
            mainCamera.allowHDR = true;
            
            // 如果支持后处理，可以添加后处理组件提升画质
            #if UNITY_POST_PROCESSING_STACK_V2
            if (enableEdgeEnhancement)
            {
                // 此处需要添加后处理相关代码，取决于您的项目是否使用后处理栈
                // 请安装Post Processing包后使用
            }
            #endif
        }
    }
}
