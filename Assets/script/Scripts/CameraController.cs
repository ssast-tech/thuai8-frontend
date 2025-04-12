using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("平移设置")]
    public float panSpeed = 20f;        // 平移速度
    public float panSmoothing = 5f;     // 平移平滑度

    [Header("旋转设置")]
    public float rotationSpeed = 3f;    // 旋转速度
    public bool invertY = false;        // 是否反转Y轴
    
    [Header("缩放设置")]
    public float zoomSpeed = 5f;        // 缩放速度
    public float minZoom = 5f;          // 最小缩放值(最近距离)
    public float maxZoom = 50f;         // 最大缩放值(最远距离)
    
    [Header("边界设置")]
    public bool limitMovement = false;  // 是否限制移动范围
    public float xMin = -50f;
    public float xMax = 50f;
    public float zMin = -50f;
    public float zMax = 50f;
    
    private Vector3 targetPosition;     // 目标位置
    private float currentZoom;          // 当前缩放值
    private Vector3 rotationAngles;     // 旋转角度
    
    void Start()
    {
        targetPosition = transform.position;
        currentZoom = transform.position.y;
        rotationAngles = transform.eulerAngles;
    }
    
    void Update()
    {
        // 处理缩放
        HandleZooming();
        
        // 处理旋转(右键)
        if (Input.GetMouseButton(1))
        {
            HandleRotation();
        }
        
        // 处理平移(左键)
        if (Input.GetMouseButton(0))
        {
            HandlePanning();
        }
        
        // 平滑移动到目标位置
        UpdateCameraPosition();
    }
    
    void HandlePanning()
    {
        // 基于当前相机视角计算移动方向
        float xInput = -Input.GetAxis("Mouse X") * panSpeed;
        float zInput = -Input.GetAxis("Mouse Y") * panSpeed;
        
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        forward.y = 0; // 保持在水平面移动
        forward.Normalize();
        
        Vector3 moveDirection = right * xInput + forward * zInput;
        targetPosition += moveDirection * Time.deltaTime;
        
        // 如果启用了边界限制，确保在边界内
        if (limitMovement)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, xMin, xMax);
            targetPosition.z = Mathf.Clamp(targetPosition.z, zMin, zMax);
        }
    }
    
    void HandleRotation()
    {
        float xRotation = Input.GetAxis("Mouse X") * rotationSpeed;
        float yRotation = Input.GetAxis("Mouse Y") * rotationSpeed * (invertY ? 1 : -1);
        
        rotationAngles.y += xRotation;
        rotationAngles.x = Mathf.Clamp(rotationAngles.x + yRotation, 0f, 89f); // 限制垂直旋转角度
        
        transform.rotation = Quaternion.Euler(rotationAngles);
    }
    
    void HandleZooming()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // 直接修改缩放值，更加线性和可预测
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            
            // 直接设置目标位置的高度，而不依赖射线检测
            targetPosition.y = currentZoom;
        }
    }
    
    void UpdateCameraPosition()
    {
        // 仅处理水平方向(X,Z)的平滑移动，垂直方向(Y)单独处理
        Vector3 horizontalTargetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 currentPosition = transform.position;
        currentPosition = Vector3.Lerp(currentPosition, horizontalTargetPosition, Time.deltaTime * panSmoothing);
        
        // 单独平滑处理高度，使用更小的平滑因子避免抖动
        float smoothHeight = Mathf.Lerp(currentPosition.y, currentZoom, Time.deltaTime * (panSmoothing * 0.5f));
        currentPosition.y = smoothHeight;
        
        // 应用最终位置
        transform.position = currentPosition;
    }
    
    // 添加防抖动功能 - 可以从脚本的其他地方调用这个方法来立即稳定摄像机
    public void StabilizeCamera()
    {
        // 简化稳定逻辑，直接设置高度为当前缩放值
        Vector3 pos = transform.position;
        pos.y = currentZoom;
        transform.position = pos;
        
        // 确保目标位置也被更新
        targetPosition = new Vector3(targetPosition.x, currentZoom, targetPosition.z);
    }
}
