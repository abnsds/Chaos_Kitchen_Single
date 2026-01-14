// PlayerAwareEnclosureGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public class PlayerAwareEnclosureGenerator : MonoBehaviour
{
    [Header("方块预制体配置")]
    public GameObject[] allBlockPrefabs; // 所有方块预制体
    public GameObject nonInteractiveBlockPrefab; // 单独绑定的不可交互方块预制体

    [Header("玩家配置")]
    public GameObject playerPrefab; // 玩家预制体
    public Camera playerCamera; // 玩家相机（可选）

    [Header("生成设置")]
    public Vector2Int gridSize = new Vector2Int(40, 40);
    public float blockSize = 1f;

    [Header("图形参数")]
    [Range(4, 8)] public int minSides = 4;
    [Range(5, 12)] public int maxSides = 8;
    [Range(5, 15)] public int minSideLength = 5;
    [Range(8, 20)] public int maxSideLength = 12;

    [Header("不可交互方块配置")]
    [Range(0f, 1f)] public float nonInteractiveChance = 0.2f; // 不可交互方块出现概率
    public int minNonInteractiveBlocks = 1; // 最少不可交互方块数量

    [Header("相机配置")]
    public float cameraHeight = 20f; // 相机高度
    public float cameraDistance = 25f; // 相机距离
    public float cameraAngle = 45f; // 相机俯角（度）
    public bool autoAdjustCamera = true; // 自动调整相机视角

    [Header("调试")]
    public bool showDebug = true;
    public KeyCode regenerateKey = KeyCode.R;

    // 私有变量
    private List<Vector2Int> wallPositions = new List<Vector2Int>();
    private List<Vector2Int> polygonVertices = new List<Vector2Int>();
    private GameObject playerInstance;
    private Vector3 playerSpawnPosition;
    private Vector3 mapCenter = Vector3.zero;
    private List<int> availableBlockIndices = new List<int>(); // 用于均衡分布的方块索引池

    void Start()
    {
        GenerateCompleteSystem();
    }

    void Update()
    {
        if (Input.GetKeyDown(regenerateKey))
        {
            ClearExistingSystem();
            GenerateCompleteSystem();
        }
    }

    void GenerateCompleteSystem()
    {
        ClearExistingSystem();

        // 1. 生成封闭图形
        GenerateRandomConvexPolygon();
        FillAllEdgesWithBlocks();

        // 2. 计算地图中心
        CalculateMapCenter();

        // 3. 计算玩家生成位置（封闭图形内部中心）
        CalculatePlayerSpawnPosition();

        // 4. 实例化所有方块（包括不可交互方块）
        InstantiateAllBlocks();

        // 5. 生成玩家
        SpawnPlayer();

        // 6. 设置相机
        SetupCamera();

        Debug.Log("系统生成完成！");
    }

    #region 图形生成部分
    void GenerateRandomConvexPolygon()
    {
        polygonVertices.Clear();

        int centerX = Random.Range(gridSize.x / 4, gridSize.x * 3 / 4);
        int centerY = Random.Range(gridSize.y / 4, gridSize.y * 3 / 4);

        int sides = Random.Range(minSides, maxSides + 1);
        List<float> angles = new List<float>();

        for (int i = 0; i < sides; i++)
        {
            angles.Add(Random.Range(0f, 360f));
        }
        angles.Sort();

        float radius = Random.Range(minSideLength * 0.8f, maxSideLength * 1.2f);

        for (int i = 0; i < sides; i++)
        {
            float angle = angles[i] * Mathf.Deg2Rad;
            float r = radius * Random.Range(0.8f, 1.2f);

            int x = Mathf.RoundToInt(centerX + r * Mathf.Cos(angle));
            int y = Mathf.RoundToInt(centerY + r * Mathf.Sin(angle));

            x = Mathf.Clamp(x, 2, gridSize.x - 3);
            y = Mathf.Clamp(y, 2, gridSize.y - 3);

            polygonVertices.Add(new Vector2Int(x, y));
        }

        SortVerticesClockwise();
    }

    void SortVerticesClockwise()
    {
        if (polygonVertices.Count < 3) return;

        Vector2 center = Vector2.zero;
        foreach (var vertex in polygonVertices)
        {
            center += new Vector2(vertex.x, vertex.y);
        }
        center /= polygonVertices.Count;

        polygonVertices.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
            float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
            return angleA.CompareTo(angleB);
        });
    }

    void FillAllEdgesWithBlocks()
    {
        wallPositions.Clear();

        if (polygonVertices.Count < 3) return;

        for (int i = 0; i < polygonVertices.Count; i++)
        {
            Vector2Int start = polygonVertices[i];
            Vector2Int end = polygonVertices[(i + 1) % polygonVertices.Count];
            FillLineWithBlocks(start, end);
        }

        RemoveDuplicatePositions();
    }

    void FillLineWithBlocks(Vector2Int start, Vector2Int end)
    {
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0)
        {
            AddWallPosition(start);
            return;
        }

        float xIncrement = dx / (float)steps;
        float yIncrement = dy / (float)steps;

        float x = start.x;
        float y = start.y;

        for (int i = 0; i <= steps; i++)
        {
            int gridX = Mathf.RoundToInt(x);
            int gridY = Mathf.RoundToInt(y);

            AddWallPosition(new Vector2Int(gridX, gridY));

            x += xIncrement;
            y += yIncrement;
        }
    }

    void AddWallPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
            return;

        wallPositions.Add(pos);
    }

    void RemoveDuplicatePositions()
    {
        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>();
        List<Vector2Int> cleanedPositions = new List<Vector2Int>();

        foreach (var pos in wallPositions)
        {
            if (uniquePositions.Add(pos))
            {
                cleanedPositions.Add(pos);
            }
        }

        wallPositions = cleanedPositions;
    }

    void CalculateMapCenter()
    {
        if (wallPositions.Count == 0)
        {
            mapCenter = Vector3.zero;
            return;
        }

        Vector3 sum = Vector3.zero;
        foreach (var pos in wallPositions)
        {
            sum += new Vector3(pos.x * blockSize, 0, pos.y * blockSize);
        }
        mapCenter = sum / wallPositions.Count;
    }
    #endregion

    #region 玩家生成部分
    void CalculatePlayerSpawnPosition()
    {
        if (polygonVertices.Count < 3)
        {
            playerSpawnPosition = Vector3.zero;
            return;
        }

        // 计算多边形中心点（平均值）
        Vector2 center = Vector2.zero;
        foreach (var vertex in polygonVertices)
        {
            center += new Vector2(vertex.x, vertex.y);
        }
        center /= polygonVertices.Count;

        // 转换为世界坐标
        playerSpawnPosition = new Vector3(
            center.x * blockSize,
            1f, // 稍微高于地面
            center.y * blockSize
        );

        // 确保生成点在多边形内部（使用射线法验证）
        if (!IsPointInPolygon(new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y)), polygonVertices))
        {
            // 如果中心点不在内部，使用多边形重心
            playerSpawnPosition = CalculatePolygonCentroid();
        }
    }

    Vector3 CalculatePolygonCentroid()
    {
        if (polygonVertices.Count < 3) return Vector3.zero;

        float area = 0f;
        float centroidX = 0f;
        float centroidY = 0f;

        for (int i = 0; i < polygonVertices.Count; i++)
        {
            Vector2Int p1 = polygonVertices[i];
            Vector2Int p2 = polygonVertices[(i + 1) % polygonVertices.Count];

            float cross = p1.x * p2.y - p2.x * p1.y;
            area += cross;
            centroidX += (p1.x + p2.x) * cross;
            centroidY += (p1.y + p2.y) * cross;
        }

        area *= 0.5f;
        float factor = 1f / (6f * area);

        centroidX *= factor;
        centroidY *= factor;

        return new Vector3(centroidX * blockSize, 1f, centroidY * blockSize);
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("未分配玩家预制体，使用默认胶囊体");
            playerInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerInstance.name = "Player";
            playerInstance.transform.position = playerSpawnPosition;

            // 添加玩家控制器组件
            var controller = playerInstance.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1f, 0);
        }
        else
        {
            playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity, transform);
            playerInstance.name = "Player";
        }

        Debug.Log($"玩家生成在位置: {playerSpawnPosition}");
    }
    #endregion

    #region 方块实例化部分（确保均衡分布）
    void InstantiateAllBlocks()
    {
        if (wallPositions.Count == 0) return;

        GameObject blocksParent = new GameObject("EnclosureBlocks");
        blocksParent.transform.SetParent(transform);

        // 确保有可用的方块预制体
        if (allBlockPrefabs == null || allBlockPrefabs.Length == 0)
        {
            Debug.LogError("没有分配方块预制体！");
            return;
        }

        // 准备均衡分布的方块索引池
        PrepareBalancedBlockIndices();

        // 确定哪些位置使用不可交互方块
        HashSet<int> nonInteractiveIndices = new HashSet<int>();
        int targetNonInteractiveCount = Mathf.Max(
            minNonInteractiveBlocks,
            Mathf.RoundToInt(wallPositions.Count * nonInteractiveChance)
        );

        while (nonInteractiveIndices.Count < targetNonInteractiveCount)
        {
            int randomIndex = Random.Range(0, wallPositions.Count);
            nonInteractiveIndices.Add(randomIndex);
        }

        // 统计方块类型使用情况
        Dictionary<int, int> blockTypeCount = new Dictionary<int, int>();
        for (int i = 0; i < allBlockPrefabs.Length; i++)
        {
            blockTypeCount[i] = 0;
        }

        // 实例化所有方块
        for (int i = 0; i < wallPositions.Count; i++)
        {
            Vector2Int gridPos = wallPositions[i];
            Vector3 worldPos = new Vector3(gridPos.x * blockSize, 0, gridPos.y * blockSize);

            GameObject blockInstance;

            if (nonInteractiveIndices.Contains(i) && nonInteractiveBlockPrefab != null)
            {
                // 使用不可交互方块
                blockInstance = Instantiate(nonInteractiveBlockPrefab, worldPos, Quaternion.identity, blocksParent.transform);
            }
            else
            {
                // 使用普通可交互方块（从均衡池中获取）
                int blockIndex = GetBalancedBlockIndex();
                GameObject blockPrefab = allBlockPrefabs[blockIndex];
                blockInstance = Instantiate(blockPrefab, worldPos, Quaternion.identity, blocksParent.transform);

                // 更新计数
                blockTypeCount[blockIndex]++;
            }

            blockInstance.name = $"Block_{gridPos.x}_{gridPos.y}";
        }

        // 输出方块类型分布统计
        Debug.Log($"生成了 {wallPositions.Count} 个方块，其中 {nonInteractiveIndices.Count} 个不可交互");
        foreach (var kvp in blockTypeCount)
        {
            if (kvp.Value > 0)
            {
                Debug.Log($"方块类型 {kvp.Key}: {kvp.Value} 个");
            }
        }
    }

    void PrepareBalancedBlockIndices()
    {
        availableBlockIndices.Clear();

        if (allBlockPrefabs.Length == 0) return;

        // 创建一个均衡的索引池，确保每种方块数量大致相等
        int blocksPerType = Mathf.CeilToInt((float)wallPositions.Count / allBlockPrefabs.Length);

        for (int blockType = 0; blockType < allBlockPrefabs.Length; blockType++)
        {
            for (int i = 0; i < blocksPerType; i++)
            {
                availableBlockIndices.Add(blockType);
            }
        }

        // 随机打乱索引池
        ShuffleList(availableBlockIndices);
    }

    int GetBalancedBlockIndex()
    {
        if (availableBlockIndices.Count == 0)
        {
            PrepareBalancedBlockIndices();
        }

        if (availableBlockIndices.Count == 0)
        {
            return Random.Range(0, allBlockPrefabs.Length);
        }

        // 从列表末尾取一个索引
        int index = availableBlockIndices[availableBlockIndices.Count - 1];
        availableBlockIndices.RemoveAt(availableBlockIndices.Count - 1);

        return index;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    #endregion

    #region 相机设置部分
    void SetupCamera()
    {
        if (playerCamera == null)
        {
            // 查找主相机或创建新相机
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam;
            }
            else
            {
                GameObject camObj = new GameObject("PlayerCamera");
                playerCamera = camObj.AddComponent<Camera>();
                playerCamera.tag = "MainCamera";
            }
        }

        // 计算相机位置和角度
        Vector3 cameraPosition = CalculateCameraPosition();
        Quaternion cameraRotation = CalculateCameraRotation();

        // 设置相机
        playerCamera.transform.position = cameraPosition;
        playerCamera.transform.rotation = cameraRotation;

        // 如果自动调整，确保能看到整个场景
        if (autoAdjustCamera)
        {
            AdjustCameraToViewAll();
        }
    }

    Vector3 CalculateCameraPosition()
    {
        // 使用地图中心作为焦点
        Vector3 focusPoint = mapCenter;

        // 计算相机位置（在焦点上方和后方）
        Vector3 direction = Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back;
        return focusPoint + direction * cameraDistance + Vector3.up * cameraHeight;
    }

    Quaternion CalculateCameraRotation()
    {
        // 相机看向地图中心，带有一定的俯角
        Vector3 directionToCenter = (mapCenter - playerCamera.transform.position).normalized;
        return Quaternion.LookRotation(directionToCenter);
    }

    void AdjustCameraToViewAll()
    {
        if (wallPositions.Count == 0) return;

        // 计算地图的边界框
        Bounds bounds = CalculateMapBounds();

        // 计算所需相机距离
        float requiredDistance = CalculateRequiredCameraDistance(bounds);

        // 调整相机位置
        Vector3 focusPoint = bounds.center;
        Vector3 direction = Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back;
        playerCamera.transform.position = focusPoint + direction * requiredDistance + Vector3.up * cameraHeight;
        playerCamera.transform.LookAt(focusPoint);

        Debug.Log($"相机调整完成：高度={cameraHeight}，距离={requiredDistance:F1}");
    }

    Bounds CalculateMapBounds()
    {
        Bounds bounds = new Bounds();

        foreach (var pos in wallPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * blockSize, 0, pos.y * blockSize);
            bounds.Encapsulate(worldPos);
        }

        // 包括玩家位置
        if (playerInstance != null)
        {
            bounds.Encapsulate(playerInstance.transform.position);
        }

        return bounds;
    }

    float CalculateRequiredCameraDistance(Bounds bounds)
    {
        // 计算相机视锥体能够容纳整个边界框所需的最小距离
        float objectSize = Mathf.Max(bounds.size.x, bounds.size.z);
        float cameraFOV = playerCamera.fieldOfView * Mathf.Deg2Rad;

        // 考虑相机角度
        float requiredDistance = (objectSize * 0.5f) / Mathf.Tan(cameraFOV * 0.5f);

        // 添加一些余量
        requiredDistance *= 1.2f;

        return Mathf.Max(requiredDistance, cameraDistance);
    }
    #endregion

    #region 辅助函数
    bool IsPointInPolygon(Vector2Int point, List<Vector2Int> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    void ClearExistingSystem()
    {
        // 清除所有方块
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        wallPositions.Clear();
        polygonVertices.Clear();
        playerInstance = null;
    }
    #endregion

    #region 调试和Gizmos
    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // 绘制玩家生成位置
        if (playerSpawnPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(playerSpawnPosition, 0.5f);
            Gizmos.DrawWireCube(playerSpawnPosition, Vector3.one * 0.3f);
        }

        // 绘制地图中心
        if (mapCenter != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(mapCenter, 0.3f);
            Gizmos.DrawWireSphere(mapCenter, 2f);
        }

        // 绘制相机视野指示
        if (playerCamera != null)
        {
            DrawCameraFrustum();
        }
    }

    void DrawCameraFrustum()
    {
        if (playerCamera == null) return;

        Gizmos.color = Color.yellow * 0.3f;

        // 计算视锥体角点
        float aspect = playerCamera.aspect;
        float fov = playerCamera.fieldOfView * Mathf.Deg2Rad;
        float near = playerCamera.nearClipPlane;
        float far = playerCamera.farClipPlane;

        Transform camTransform = playerCamera.transform;

        // 绘制视锥体
        Vector3[] nearCorners = GetFrustumCorners(camTransform, near, aspect, fov);
        Vector3[] farCorners = GetFrustumCorners(camTransform, far, aspect, fov);

        // 绘制近平面
        Gizmos.DrawLine(nearCorners[0], nearCorners[1]);
        Gizmos.DrawLine(nearCorners[1], nearCorners[2]);
        Gizmos.DrawLine(nearCorners[2], nearCorners[3]);
        Gizmos.DrawLine(nearCorners[3], nearCorners[0]);

        // 绘制远平面
        Gizmos.DrawLine(farCorners[0], farCorners[1]);
        Gizmos.DrawLine(farCorners[1], farCorners[2]);
        Gizmos.DrawLine(farCorners[2], farCorners[3]);
        Gizmos.DrawLine(farCorners[3], farCorners[0]);

        // 连接远近平面
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(nearCorners[i], farCorners[i]);
        }
    }

    Vector3[] GetFrustumCorners(Transform camTransform, float distance, float aspect, float fov)
    {
        Vector3[] corners = new Vector3[4];

        float halfHeight = distance * Mathf.Tan(fov * 0.5f);
        float halfWidth = halfHeight * aspect;

        corners[0] = camTransform.position + camTransform.forward * distance
            + camTransform.up * halfHeight - camTransform.right * halfWidth;
        corners[1] = camTransform.position + camTransform.forward * distance
            + camTransform.up * halfHeight + camTransform.right * halfWidth;
        corners[2] = camTransform.position + camTransform.forward * distance
            - camTransform.up * halfHeight + camTransform.right * halfWidth;
        corners[3] = camTransform.position + camTransform.forward * distance
            - camTransform.up * halfHeight - camTransform.right * halfWidth;

        return corners;
    }
    #endregion

    #region 公共方法
    public List<Vector2Int> GetWallPositions()
    {
        return new List<Vector2Int>(wallPositions);
    }

    public List<Vector2Int> GetPolygonVertices()
    {
        return new List<Vector2Int>(polygonVertices);
    }

    public Vector3 GetMapCenter()
    {
        return mapCenter;
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        return playerSpawnPosition;
    }
    #endregion
}