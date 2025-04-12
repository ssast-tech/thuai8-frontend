using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; // 用于文件操作

// 地图生成器组件 - 应挂载在场景中的空GameObject上（如MapManager）
public class MapGeneration : MonoBehaviour
{

    private int mapWidth;
    public int[,] mapArray;
    
    // 新增立方体生成相关属性
    public GameObject cubePrefab; // 方块预制件，如果为空则使用Unity自带的Cube
    public Material cubeMaterial; // 方块材质，如果使用预制件可以不设置
    public float cubeSize = 1.0f; // 每个方块的大小
    public Transform mapParent; // 所有生成方块的父对象
    
    // 添加自动生成的控制参数
    [Header("自动生成设置")]
    public bool generateOnStart = false; // 是否在开始时自动生成地图
    public int defaultMapWidth = 10; // 默认地图宽度
    public bool enableDebugKeys = false; // 是否启用调试按键
    
    // 添加JSON相关设置
    [Header("JSON地图设置")]
    public bool loadFromJSON = false; // 是否从JSON文件加载
    public string jsonFilePath = "Assets/Maps/map1.json"; // JSON文件路径
    public string mapName = ""; // 地图名称（从JSON读取）
    public string mapDescription = ""; // 地图描述（从JSON读取）

    // 修改JSON格式的地图数据类
   
  
    // Start is called before the first frame update
    void Start()
    {
        // 启用全局抗锯齿设置
        QualitySettings.antiAliasing = 4; // 可以设为2、4或8，值越高质量越好但性能消耗越大
        
        // 如果设置了从JSON加载，则尝试加载JSON文件
        if (loadFromJSON)
        {
            if (LoadMapFromJSON(jsonFilePath))
            {
                GenerateMap();
                return;
            }
            else
            {
                Debug.LogWarning("无法从JSON加载地图，将使用默认随机地图");
            }
        }
        
        // 如果设置了自动生成地图或者JSON加载失败
        if (generateOnStart)
        {
            // 初始化地图大小
            if (mapArray == null)
            {
                setMapWidth(defaultMapWidth);
                
                // 生成示例地图数据
                for (int i = 0; i < defaultMapWidth; i++)
                {
                    for (int j = 0; j < defaultMapWidth; j++)
                    {
                        // 创建一些随机高度的方块，可以根据需要修改
                        mapArray[i, j] = Random.Range(1, 4);
                    }
                }
            }
            
            // 生成地图
            GenerateMap();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 只有在启用调试按键时才检测输入
        if (enableDebugKeys)
        {
            // 按空格键重新生成地图
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("重新生成地图...");
                GenerateRandomMap();
            }
            
            // 按C键清除地图
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("清除地图...");
                ClearMap();
            }
            
            // 按L键加载JSON地图
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("尝试从JSON加载地图...");
                if (LoadMapFromJSON(jsonFilePath))
                {
                    GenerateMap();
                }
            }
        }
    }

    // 从JSON文件加载地图
    public bool LoadMapFromJSON(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("JSON文件路径为空");
                return false;
            }
            
            string jsonText = null;
            
            // 根据路径类型读取文件
            if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            {
                // 从网络加载需要使用WWW或UnityWebRequest，这里略过
                Debug.LogError("当前不支持从URL加载JSON");
                return false;
            }
            else
            {
                Debug.Log($"尝试从路径加载地图文件: {filePath}");
                
                // 从本地文件读取
                if (filePath.StartsWith("Assets/") || filePath.StartsWith("assets/"))
                {
                    // 转换为StreamingAssets路径
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, filePath.Substring(7));
                    Debug.Log($"尝试从StreamingAssets加载: {streamingPath}");
                    
                    if (File.Exists(streamingPath))
                    {
                        jsonText = File.ReadAllText(streamingPath);
                        Debug.Log("从StreamingAssets成功读取文件");
                    }
                    else if (File.Exists(filePath))
                    {
                        jsonText = File.ReadAllText(filePath);
                        Debug.Log("从原始路径成功读取文件");
                    }
                    else
                    {
                        Debug.LogError($"找不到地图文件: {filePath} 或 {streamingPath}");
                        return false;
                    }
                }
                else
                {
                    // 尝试作为完整路径或相对于StreamingAssets的路径
                    if (File.Exists(filePath))
                    {
                        jsonText = File.ReadAllText(filePath);
                        Debug.Log($"从完整路径成功读取文件: {filePath}");
                    }
                    else
                    {
                        string streamingPath = Path.Combine(Application.streamingAssetsPath, filePath);
                        Debug.Log($"尝试从StreamingAssets加载: {streamingPath}");
                        
                        if (File.Exists(streamingPath))
                        {
                            jsonText = File.ReadAllText(streamingPath);
                            Debug.Log("从StreamingAssets成功读取文件");
                        }
                        else
                        {
                            Debug.LogError($"找不到地图文件: {filePath} 或 {streamingPath}");
                            return false;
                        }
                    }
                }
            }
            
            // 检查读取的JSON文本是否为空
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("读取的JSON文本为空");
                return false;
            }
            
            Debug.Log($"读取到的JSON内容: {jsonText.Substring(0, Mathf.Min(100, jsonText.Length))}...");
            
            // 解析JSON
            MapData mapData = JsonUtility.FromJson<MapData>(jsonText);
            
            // 检查解析后的对象是否为空
            if (mapData == null)
            {
                Debug.LogError("JSON解析失败，mapData为null");
                return false;
            }
            
            // 确保rows不为null
            if (mapData.rows == null)
            {
                Debug.LogError("JSON格式错误: mapData.rows为null");
                return false; 
            }
            
            // 更新地图属性
            mapName = mapData.mapName ?? "未命名地图";
            mapDescription = mapData.mapDescription ?? "无描述";
            
            // 确保有效的地图尺寸
            if (mapData.mapWidth <= 0)
            {
                Debug.LogWarning("无效的地图宽度，使用默认值");
                mapData.mapWidth = defaultMapWidth;
            }
            
            if (mapData.cubeSize <= 0)
            {
                Debug.LogWarning("无效的方块大小，使用默认值");
            }
            else
            {
                cubeSize = mapData.cubeSize;
            }
            
            // 设置地图大小
            setMapWidth(mapData.mapWidth);
            
            // 将行列数据转换为二维数组
            Debug.Log($"地图行数: {mapData.rows.Count}");
            
            for (int x = 0; x < mapData.mapWidth; x++)
            {
                if (x >= mapData.rows.Count)
                {
                    Debug.LogWarning($"地图数据行数 {mapData.rows.Count} 小于mapWidth {mapData.mapWidth}");
                    // 对于超出rows边界的数据，填充0
                    for (int z = 0; z < mapData.mapWidth; z++)
                    {
                        mapArray[x, z] = 0;
                    }
                    continue;
                }
                
                // 确保row不为null
                if (mapData.rows[x].row == null)
                {
                    Debug.LogWarning($"第{x}行数据为null，填充0");
                    for (int z = 0; z < mapData.mapWidth; z++)
                    {
                        mapArray[x, z] = 0;
                    }
                    continue;
                }
                
                for (int z = 0; z < mapData.mapWidth; z++)
                {
                    // 确保不越界
                    if (z < mapData.rows[x].row.Count)
                    {
                        mapArray[x, z] = mapData.rows[x].row[z];
                    }
                    else
                    {
                        mapArray[x, z] = 0; // 默认值
                    }
                }
            }
            
            Debug.Log($"成功加载地图 '{mapName}': {mapDescription}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载JSON地图时出错: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }
    
    // 将当前地图保存为JSON
    public bool SaveMapToJSON(string filePath)
    {
        try
        {
            // 创建MapData对象
            MapData mapData = new MapData
            {
                mapName = this.mapName,
                mapDescription = this.mapDescription,
                mapWidth = this.mapWidth,
                cubeSize = this.cubeSize,
                rows = new List<MapRow>()
            };
            
            // 将二维数组转换为行列结构
            for (int x = 0; x < mapWidth; x++)
            {
                MapRow newRow = new MapRow { row = new List<int>() };
                
                for (int z = 0; z < mapWidth; z++)
                {
                    newRow.row.Add(mapArray[x, z]);
                }
                
                mapData.rows.Add(newRow);
            }
            
            // 转换为JSON
            string jsonText = JsonUtility.ToJson(mapData, true); // true表示格式化JSON
            
            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 写入文件
            File.WriteAllText(filePath, jsonText);
            
            Debug.Log($"地图已保存到: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存地图为JSON时出错: {e.Message}");
            return false;
        }
    }

    public void setMapWidth(int width)
    {
        mapWidth = width;
        mapArray = new int[mapWidth, mapWidth];
    }
    
    public void setMapArray(int[,] map)
    {
        mapArray = map;
    }

    public void GenerateMap()
    {
        if (mapArray == null)
        {
            Debug.LogError("地图数组未初始化！");
            return;
        }

        // 清除之前的地图
        ClearMap();

        // 如果没有设置父对象，则创建一个
        if (mapParent == null)
        {
            GameObject parent = new GameObject("MapParent");
            mapParent = parent.transform;
            mapParent.SetParent(transform);
            mapParent.localPosition = Vector3.zero;
        }

        // 遍历地图数组生成方块
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                int height = mapArray[i, j];
                
                // 跳过高度为0的位置
                if (height <= 0) continue;
                
                // 垂直堆叠方块
                for (int k = 0; k < height; k++)
                {
                    CreateCube(i, j, k);
                }
            }
        }
    }
    
    // 创建单个立方体方块
    private void CreateCube(int x, int z, int y)
    {
        GameObject cube;
        
        // 使用预制件或者创建原始立方体
        if (cubePrefab != null)
        {
            cube = Instantiate(cubePrefab);
        }
        else
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // 如果提供了材质，则应用
            if (cubeMaterial != null)
            {
                var renderer = cube.GetComponent<Renderer>();
                renderer.material = cubeMaterial;
                
                // 优化材质设置，减少锯齿
                renderer.material.enableInstancing = true; // 启用GPU实例化
                
                // 设置材质平滑度和反射，减少尖锐边缘
                renderer.material.SetFloat("_Smoothness", 0.5f);
                renderer.material.SetFloat("_Glossiness", 0.5f);
            }
            else
            {
                // 如果没有自定义材质，对默认材质进行优化
                var renderer = cube.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                
                // 创建新材质以优化默认立方体外观
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = new Color(0.7f, 0.7f, 0.7f);
                defaultMat.enableInstancing = true;
                defaultMat.SetFloat("_Smoothness", 0.5f);
                defaultMat.SetFloat("_Glossiness", 0.5f);
                renderer.material = defaultMat;
            }
        }

        // 设置立方体的位置和大小 - 稍微缩小一点防止相邻方块间的Z-fighting
        cube.transform.SetParent(mapParent);
        cube.transform.localPosition = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);
        cube.transform.localScale = new Vector3(cubeSize * 0.95f, cubeSize * 0.95f, cubeSize * 0.95f);
        
        // 命名立方体以便于识别
        cube.name = $"Cube_{x}_{z}_{y}";
        
        // 为方块添加组件处理边缘平滑
        AddEdgeSmoothing(cube);
    }
    
    // 添加边缘平滑处理
    private void AddEdgeSmoothing(GameObject cube)
    {
        // 获取或添加MeshFilter组件
        MeshFilter meshFilter = cube.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // 创建网格的副本，以便于修改
            Mesh mesh = Instantiate(meshFilter.sharedMesh);
            
            // 重新计算法线，使其更平滑
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            // 将修改后的网格应用到方块
            meshFilter.mesh = mesh;
        }
    }
    
    // 清除已生成的地图
    private void ClearMap()
    {
        if (mapParent != null)
        {
            // 在编辑器中
            #if UNITY_EDITOR
            // 清除所有子对象
            while (mapParent.childCount > 0)
            {
                DestroyImmediate(mapParent.GetChild(0).gameObject);
            }
            #else
            // 运行时
            foreach (Transform child in mapParent)
            {
                Destroy(child.gameObject);
            }
            #endif
        }
    }
    
    // 生成随机地图的辅助方法
    private void GenerateRandomMap()
    {
        if (mapArray == null || mapWidth == 0)
        {
            setMapWidth(defaultMapWidth);
        }
        
        // 随机高度
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                mapArray[i, j] = Random.Range(0, 4);
            }
        }
        
        // 生成地图
        GenerateMap();
    }
}
