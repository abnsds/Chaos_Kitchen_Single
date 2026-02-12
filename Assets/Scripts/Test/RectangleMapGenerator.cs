using System;
using System.Collections.Generic;
using UnityEngine;

public class RectangleMapGenerator : SingletonMono<RectangleMapGenerator>
{
    [Header("方块预制体配置")]
    public GameObject[] allBlockPrefabs; // 所有方块预制体（每种只出现一次）
    public GameObject nonInteractiveBlockPrefab; // 单独绑定的不可交互方块预制体

    [Header("玩家配置")]
    public GameObject playerPrefab; // 玩家预制体

    [Header("矩形关卡设置")]
    public int minWidth = 10; // 最小宽度（格子数）
    public int maxWidth = 20; // 最大宽度（格子数）
    public int minHeight = 10; // 最小高度（格子数）
    public int maxHeight = 20; // 最大高度（格子数）
    public float blockSize = 1f; // 方块大小

    [Header("地图大小限制")]
    public Vector2Int gridSize = new Vector2Int(40, 40); // 地图网格大小

    [Header("不可交互方块配置")]
    [Range(1, 10)] public int minNonInteractiveBlocks = 6; // 最少不可交互方块数量（至少4个顶点）
    [Range(1, 20)] public int maxNonInteractiveBlocks = 10; // 最多不可交互方块数量

    [Header("生成控制")]
    public bool generateOnStart = true; // 启动时自动生成
    public KeyCode regenerateKey = KeyCode.R; // 重新生成按键

    [Header("调试显示")]
    public bool showDebug = true; // 显示调试信息

    
    [Header("通关分数配置")]
    public int scorePerBlock = 2; // 每个方块对应的基础分数
    public float randomFactorMin = 0.8f; // 随机因子最小值（0.8=80%）
    public float randomFactorMax = 1.2f; // 随机因子最大值（1.2=120%）
    public int scoreBaseOffset = 10; // 基准校正值（固定加分，平衡难度）
    public event EventHandler<int> OnTargetScoreGenerated;

    private int currentTargetScore; // 当前关卡的目标分数

    new private const string camera = "camera";

    
    private List<Vector2Int> wallPositions = new List<Vector2Int>();
    private List<Vector2Int> rectangleCorners = new List<Vector2Int>();
    private GameObject playerInstance;
    private Vector3 playerSpawnPosition;
    private Vector3 mapCenter = Vector3.zero;
    private List<int> shuffledBlockIndices = new List<int>(); // 洗牌后的方块索引
    private int currentBlockIndex = 0; // 当前使用的方块索引
    private RectangleInfo rectangleInfo; // 矩形信息
    private Dictionary<Vector2Int, bool> cornerPositions = new Dictionary<Vector2Int, bool>(); // 记录顶点位置



    // 矩形信息结构
    private struct RectangleInfo
    {
        public Vector2Int position; // 左下角位置
        public int width;           // 宽度（格子数）
        public int height;          // 高度（格子数）
    }

    void Start()
    {
        if (generateOnStart)
        {
            GenerateCompleteSystem();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(regenerateKey))
        {
            ClearExistingSystem();
            GenerateCompleteSystem();
        }
    }

    /// <summary>
    /// 生成完整的矩形关卡系统
    /// </summary>
    public void GenerateCompleteSystem()
    {
        ClearExistingSystem();

        // 1. 生成随机矩形
        GenerateRandomRectangle();

        // 2. 填充矩形边界
        FillRectangleEdges();

        // 3. 计算地图中心
        CalculateMapCenter();

        // 4. 计算玩家生成位置（矩形中心）
        CalculatePlayerSpawnPosition();

        // 5. 准备方块索引（每种方块只出现一次）
        PrepareBlockIndices();

        // 6. 实例化所有方块（四个顶点必须是不可交互方块，所有方块面朝内）
        InstantiateAllBlocks();

        // 7. 生成玩家
        SpawnPlayer();

        //Debug.Log($"矩形关卡生成完成！尺寸: {rectangleInfo.width}x{rectangleInfo.height}, 位置: {rectangleInfo.position}");
        //Debug.Log($"玩家位置: {playerSpawnPosition}");
        CalculateTargetScore();
        // 通知外部（UI/DeliveryMgr）目标分数已更新
        OnTargetScoreGenerated?.Invoke(this, currentTargetScore);
        Debug.Log($"当前关卡总方块数：{GetTotalBlocks()}，目标分数：{currentTargetScore}");
    }
    /// <summary>
    /// 根据总方块数计算动态目标分数
    /// 公式：目标分数 = (总方块数 × 每方块基础分 + 基准校正值) × 随机因子
    /// </summary>
    private void CalculateTargetScore()
    {
        int totalBlocks = GetTotalBlocks();
        if (totalBlocks <= 0)
        {
            currentTargetScore = scoreBaseOffset;
            return;
        }
        int lastLevelScore = GameMgr.GetInstance().GetLastLevelScore();
        
        int baseScore = (totalBlocks * scorePerBlock) + scoreBaseOffset;
        // 生成随机增量（10~30分，可调整）
        int randomIncrement = UnityEngine.Random.Range(10, 31);
        // 最终目标分数 = 上一关分数 + 本关基础分 × 随机因子 + 随机增量
        float randomFactor = UnityEngine.Random.Range(randomFactorMin, randomFactorMax);
        currentTargetScore = Mathf.RoundToInt(lastLevelScore + (baseScore * randomFactor) + randomIncrement);
        Debug.Log($"第{GameMgr.GetInstance().GetCurrentLevel()}关目标分数计算完成：");
        Debug.Log($"上一关分数：{lastLevelScore}，本关基础分：{baseScore}，随机增量：{randomIncrement}，最终目标：{currentTargetScore}");
    }

    #region 矩形生成部分
    /// <summary>
    /// 生成随机矩形
    /// </summary>
    void GenerateRandomRectangle()
    {
        // 随机确定矩形尺寸
        int width = UnityEngine.Random.Range(minWidth, maxWidth + 1);
        int height = UnityEngine.Random.Range(minHeight, maxHeight + 1);

        // 确保矩形不会超出地图边界
        int maxX = gridSize.x - width - 1;
        int maxY = gridSize.y - height - 1;

        if (maxX < 1 || maxY < 1)
        {
            Debug.LogWarning("矩形尺寸过大，自动调整...");
            width = Mathf.Min(width, gridSize.x - 2);
            height = Mathf.Min(height, gridSize.y - 2);
            maxX = Mathf.Max(1, gridSize.x - width - 1);
            maxY = Mathf.Max(1, gridSize.y - height - 1);
        }

        // 随机确定矩形左下角位置
        int x = UnityEngine.Random.Range(1, maxX);
        int y = UnityEngine.Random.Range(1, maxY);

        rectangleInfo = new RectangleInfo
        {
            position = new Vector2Int(x, y),
            width = width,
            height = height
        };

        // 计算四个角点
        rectangleCorners.Clear();
        cornerPositions.Clear();

        Vector2Int[] corners = new Vector2Int[4]
        {
            new Vector2Int(x, y),                     // 左下角
            new Vector2Int(x + width - 1, y),         // 右下角
            new Vector2Int(x + width - 1, y + height - 1), // 右上角
            new Vector2Int(x, y + height - 1)         // 左上角
        };

        foreach (var corner in corners)
        {
            rectangleCorners.Add(corner);
            cornerPositions[corner] = true;
        }
    }

    /// <summary>
    /// 填充矩形边界
    /// </summary>
    void FillRectangleEdges()
    {
        wallPositions.Clear();

        int x = rectangleInfo.position.x;
        int y = rectangleInfo.position.y;
        int width = rectangleInfo.width;
        int height = rectangleInfo.height;

        // 填充下边
        for (int i = 0; i < width; i++)
        {
            AddWallPosition(new Vector2Int(x + i, y));
        }

        // 填充上边
        for (int i = 0; i < width; i++)
        {
            AddWallPosition(new Vector2Int(x + i, y + height - 1));
        }

        // 填充左边（排除角落已添加的点）
        for (int j = 1; j < height - 1; j++)
        {
            AddWallPosition(new Vector2Int(x, y + j));
        }

        // 填充右边（排除角落已添加的点）
        for (int j = 1; j < height - 1; j++)
        {
            AddWallPosition(new Vector2Int(x + width - 1, y + j));
        }

        // 移除重复的位置
        RemoveDuplicatePositions();
    }

    /// <summary>
    /// 添加墙体位置
    /// </summary>
    void AddWallPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
        {
            Debug.LogWarning($"位置 {pos} 超出地图范围");
            return;
        }

        wallPositions.Add(pos);
    }

    /// <summary>
    /// 移除重复位置
    /// </summary>
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

    /// <summary>
    /// 计算地图中心
    /// </summary>
    void CalculateMapCenter()
    {
        // 计算矩形的中心
        float centerX = rectangleInfo.position.x + rectangleInfo.width / 2f - 0.5f;
        float centerY = rectangleInfo.position.y + rectangleInfo.height / 2f - 0.5f;

        mapCenter = new Vector3(
            centerX * blockSize,
            0,
            centerY * blockSize
        );
    }
    #endregion

    #region 玩家生成部分
    /// <summary>
    /// 计算玩家生成位置
    /// </summary>
    void CalculatePlayerSpawnPosition()
    {
        // 玩家生成在矩形中心
        float centerX = rectangleInfo.position.x + rectangleInfo.width / 2f - 0.5f;
        float centerY = rectangleInfo.position.y + rectangleInfo.height / 2f - 0.5f;

        playerSpawnPosition = new Vector3(
            centerX * blockSize,
            0f, // 稍微高于地面
            centerY * blockSize
        );
    }

    /// <summary>
    /// 生成玩家
    /// </summary>
    void SpawnPlayer()
    {

        if (playerPrefab == null)
        {
            Debug.LogWarning("未分配玩家预制体，使用默认胶囊体");
            return;
        }
        
        
        playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity, transform);
        playerInstance.name = "Player";
        EventCenter.GetInstance().MarkEventReady("PlayerCreated");
        EventCenter.GetInstance().EventTrigger(camera, playerInstance.transform);

        //Debug.Log($"玩家生成在位置: {playerSpawnPosition}");
    }
    #endregion

    #region 方块实例化部分（所有方块面朝内，四个角朝向一个方向）
    /// <summary>
    /// 准备方块索引
    /// </summary>
    void PrepareBlockIndices()
    {
        shuffledBlockIndices.Clear();
        currentBlockIndex = 0;

        if (allBlockPrefabs == null || allBlockPrefabs.Length == 0)
        {
            Debug.LogError("没有分配方块预制体！");
            return;
        }

        // 创建方块索引列表
        for (int i = 0; i < allBlockPrefabs.Length; i++)
        {
            shuffledBlockIndices.Add(i);
        }

        // 随机打乱索引
        ShuffleList(shuffledBlockIndices);

        //Debug.Log($"准备了 {shuffledBlockIndices.Count} 种方块，每种将出现一次");
    }

    /// <summary>
    /// 获取下一个方块索引
    /// </summary>
    int GetNextBlockIndex()
    {
        if (shuffledBlockIndices.Count == 0)
        {
            Debug.LogWarning("方块索引不足，返回随机索引");
            return UnityEngine.Random.Range(0, allBlockPrefabs.Length);
        }

        // 如果已经用完所有独特方块，重新打乱并重新开始
        if (currentBlockIndex >= shuffledBlockIndices.Count)
        {
            ShuffleList(shuffledBlockIndices);
            currentBlockIndex = 0;
        }

        int index = shuffledBlockIndices[currentBlockIndex];
        currentBlockIndex++;
        return index;
    }

    /// <summary>
    /// 洗牌算法
    /// </summary>
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 实例化所有方块（四个顶点必须是不可交互方块，所有方块面朝内）
    /// </summary>
    void InstantiateAllBlocks()
    {
        if (wallPositions.Count == 0) return;

        GameObject blocksParent = new GameObject("RectangleBlocks");
        blocksParent.transform.SetParent(transform);

        // 确保有可用的方块预制体
        if (allBlockPrefabs == null || allBlockPrefabs.Length == 0)
        {
            Debug.LogError("没有分配方块预制体！");
            return;
        }

        if (nonInteractiveBlockPrefab == null)
        {
            Debug.LogError("没有分配不可交互方块预制体！");
            return;
        }

        // 确定哪些位置使用不可交互方块（确保四个顶点包含在内）
        HashSet<int> nonInteractiveIndices = GetNonInteractiveBlockPositions();

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
            string blockName;

            // 检查是否是顶点位置
            bool isCorner = IsCornerPosition(gridPos);

            // 计算朝向内部的旋转角度
            Quaternion rotation = CalculateInwardRotation(gridPos);

            if (isCorner || nonInteractiveIndices.Contains(i))
            {
                // 顶点位置强制使用不可交互方块
                // 其他位置如果被选中也使用不可交互方块
                blockInstance = Instantiate(nonInteractiveBlockPrefab, worldPos, rotation, blocksParent.transform);

                if (isCorner)
                {
                    blockName = $"Corner_NonInteractive_{gridPos.x}_{gridPos.y}";
                    // 为顶点方块添加特殊标记
                    MarkCornerBlock(blockInstance, gridPos);
                }
                else
                {
                    blockName = $"NonInteractive_{gridPos.x}_{gridPos.y}";
                    MarkNonInteractiveBlock(blockInstance);
                }
            }
            else
            {
                // 使用普通可交互方块（每种只出现一次）
                int blockIndex = GetNextBlockIndex();
                GameObject blockPrefab = allBlockPrefabs[blockIndex];
                blockInstance = Instantiate(blockPrefab, worldPos, rotation, blocksParent.transform);

                // 更新计数
                blockTypeCount[blockIndex]++;

                // 记录方块类型
                blockName = $"Interactive_{blockIndex}_{gridPos.x}_{gridPos.y}";
            }

            blockInstance.name = blockName;
        }

        // 输出方块类型分布统计
        OutputBlockStatistics(nonInteractiveIndices.Count, blockTypeCount);
    }

    /// <summary>
    /// 计算方块朝向内部的旋转角度
    /// </summary>
    Quaternion CalculateInwardRotation(Vector2Int gridPos)
    {
        int x = rectangleInfo.position.x;
        int y = rectangleInfo.position.y;
        int width = rectangleInfo.width;
        int height = rectangleInfo.height;

        // 检查方块在哪条边上，并设置朝向
        if (gridPos.y == y) // 下边
        {
            // 朝向北（朝向矩形内部）
            return Quaternion.Euler(0, 0, 0);
        }
        else if (gridPos.y == y + height - 1) // 上边
        {
            // 朝向南
            return Quaternion.Euler(0, 180, 0);
        }
        else if (gridPos.x == x) // 左边
        {
            // 朝向东
            return Quaternion.Euler(0, 90, 0);
        }
        else if (gridPos.x == x + width - 1) // 右边
        {
            // 朝向西
            return Quaternion.Euler(0, 270, 0);
        }

        // 如果是顶点，使用默认朝向（北方向）
        if (IsCornerPosition(gridPos))
        {
            // 四个角的方块都朝向北方向（0度）
            return Quaternion.Euler(0, 0, 0);
        }

        // 默认朝向北
        return Quaternion.Euler(0, 0, 0);
    }

    /// <summary>
    /// 标记顶点方块
    /// </summary>
    void MarkCornerBlock(GameObject block, Vector2Int position)
    {
        var renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 顶点方块使用特殊的颜色标记
            renderer.material.color = new Color(0.5f, 0.2f, 0.2f); // 深红色
        }

        // 可以在这里添加其他特殊组件或效果
        block.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f); // 稍微放大

        // 添加朝向指示器（调试用）
        if (showDebug)
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "DirectionArrow";
            arrow.transform.SetParent(block.transform);
            arrow.transform.localPosition = new Vector3(0, 0.6f, 0.3f);
            arrow.transform.localScale = new Vector3(0.1f, 0.1f, 0.3f);
            arrow.GetComponent<Renderer>().material.color = Color.yellow;
        }
    }

    /// <summary>
    /// 标记不可交互方块
    /// </summary>
    void MarkNonInteractiveBlock(GameObject block)
    {
        var renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 不可交互方块使用灰色标记
            renderer.material.color = Color.gray * 0.7f;
        }
    }

    /// <summary>
    /// 获取不可交互方块位置（确保四个顶点包含在内）
    /// </summary>
    HashSet<int> GetNonInteractiveBlockPositions()
    {
        HashSet<int> nonInteractiveIndices = new HashSet<int>();

        // 首先确保四个顶点被包含
        for (int i = 0; i < wallPositions.Count; i++)
        {
            if (IsCornerPosition(wallPositions[i]))
            {
                nonInteractiveIndices.Add(i);
                //Debug.Log($"顶点 {wallPositions[i]} 被标记为不可交互方块");
            }
        }

        // 随机确定额外不可交互方块数量
        int nonInteractiveCount = UnityEngine.Random.Range(minNonInteractiveBlocks, maxNonInteractiveBlocks + 1);

        // 确保至少包含四个顶点
        nonInteractiveCount = Mathf.Max(nonInteractiveCount, 4);

        // 限制不可交互方块数量不能超过总方块数的一半
        nonInteractiveCount = Mathf.Min(nonInteractiveCount, wallPositions.Count / 2);

        // 如果矩形太小，调整数量
        if (wallPositions.Count < 10)
        {
            nonInteractiveCount = Mathf.Min(nonInteractiveCount, wallPositions.Count);
        }

        // 添加额外的不可交互方块位置（避开已经添加的顶点）
        while (nonInteractiveIndices.Count < nonInteractiveCount)
        {
            int randomIndex = UnityEngine.Random.Range(0, wallPositions.Count);

            // 确保不是已经包含的顶点位置
            if (!nonInteractiveIndices.Contains(randomIndex))
            {
                nonInteractiveIndices.Add(randomIndex);
            }
        }

        //Debug.Log($"不可交互方块总数: {nonInteractiveIndices.Count} (包含{rectangleCorners.Count}个顶点)");
        return nonInteractiveIndices;
    }

    /// <summary>
    /// 检查是否是顶点位置
    /// </summary>
    bool IsCornerPosition(Vector2Int position)
    {
        return cornerPositions.ContainsKey(position);
    }

    /// <summary>
    /// 输出方块统计信息
    /// </summary>
    void OutputBlockStatistics(int nonInteractiveCount, Dictionary<int, int> blockTypeCount)
    {
        int interactiveCount = wallPositions.Count - nonInteractiveCount;
        int cornerCount = rectangleCorners.Count;

        //Debug.Log("=== 方块统计 ===");
        //Debug.Log($"总方块数: {wallPositions.Count}");
        //Debug.Log($"可交互方块: {interactiveCount} 个");
        //Debug.Log($"不可交互方块: {nonInteractiveCount} 个");
        //Debug.Log($"  - 其中顶点方块: {cornerCount} 个");
        //Debug.Log($"  - 其他不可交互方块: {nonInteractiveCount - cornerCount} 个");

        //Debug.Log("可交互方块类型分布：");
        bool hasUniqueBlocks = false;
        foreach (var kvp in blockTypeCount)
        {
            if (kvp.Value > 0)
            {
                hasUniqueBlocks = true;
                //Debug.Log($"  类型 {kvp.Key}: {kvp.Value} 个");
            }
        }

        if (!hasUniqueBlocks && interactiveCount > 0)
        {
            Debug.LogWarning($"有{interactiveCount}个可交互方块，但类型分布统计为空！");
        }

        // 验证四个顶点是否都是不可交互方块
        bool allCornersAreNonInteractive = true;
        foreach (var corner in rectangleCorners)
        {
            // 查找场景中对应位置的方块
            string expectedName = $"Corner_NonInteractive_{corner.x}_{corner.y}";
            GameObject cornerBlock = GameObject.Find(expectedName);

            if (cornerBlock == null)
            {
                Debug.LogError($"顶点 {corner} 的方块没有找到或命名不正确！");
                allCornersAreNonInteractive = false;
            }
            else
            {
                //Debug.Log($"确认顶点 {corner} 为不可交互方块: {cornerBlock.name}");
                //Debug.Log($"  朝向: {cornerBlock.transform.rotation.eulerAngles.y:F0}度");
            }
        }

        if (allCornersAreNonInteractive)
        {
            //Debug.Log("✓ 所有四个顶点都是不可交互方块");
        }
        else
        {
            Debug.LogError("✗ 存在顶点不是不可交互方块！");
        }

        // 验证所有方块是否面朝内
        //Debug.Log("验证方块朝向...");
        int inwardFacingCount = 0;
        int totalFacingCount = 0;

        foreach (Transform child in GameObject.Find("RectangleBlocks").transform)
        {
            totalFacingCount++;

            // 获取方块的前方向量
            Vector3 blockForward = child.forward;

            // 检查方块的位置，判断它应该朝向哪个方向
            Vector3 blockPos = child.position;
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(blockPos.x / blockSize),
                Mathf.RoundToInt(blockPos.z / blockSize)
            );

            // 检查方块应该的朝向
            Quaternion expectedRotation = CalculateInwardRotation(gridPos);
            Vector3 expectedForward = expectedRotation * Vector3.forward;

            // 计算实际朝向和期望朝向的角度差
            float angleDiff = Vector3.Angle(blockForward, expectedForward);

            if (angleDiff < 10f) // 如果角度差小于10度，认为朝向正确
            {
                inwardFacingCount++;
            }
            else
            {
                Debug.LogWarning($"方块 {child.name} 朝向不正确，角度差: {angleDiff:F1}度");
            }
        }

        //Debug.Log($"方块朝向统计: {inwardFacingCount}/{totalFacingCount} 个方块朝向正确");

        if (inwardFacingCount == totalFacingCount)
        {
            //Debug.Log("✓ 所有方块都正确朝向");
        }
        else
        {
            Debug.LogWarning($"有 {totalFacingCount - inwardFacingCount} 个方块朝向不正确");
        }
    }
    #endregion

    #region 辅助函数
    /// <summary>
    /// 清空现有系统
    /// </summary>
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
        rectangleCorners.Clear();
        cornerPositions.Clear();
        shuffledBlockIndices.Clear();
        currentBlockIndex = 0;

        // 清除玩家
        if (playerInstance != null)
        {
            if (Application.isPlaying)
                Destroy(playerInstance);
            else
                DestroyImmediate(playerInstance);
        }
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
        }

        // 绘制矩形轮廓
        if (rectangleCorners.Count >= 4)
        {
            Gizmos.color = Color.yellow;

            // 绘制四条边
            for (int i = 0; i < 4; i++)
            {
                Vector2Int start = rectangleCorners[i];
                Vector2Int end = rectangleCorners[(i + 1) % 4];

                Vector3 startPos = new Vector3(start.x * blockSize, 0.1f, start.y * blockSize);
                Vector3 endPos = new Vector3(end.x * blockSize, 0.1f, end.y * blockSize);

                Gizmos.DrawLine(startPos, endPos);
            }

            // 绘制顶点（特殊标记）
            Gizmos.color = Color.red;
            foreach (var corner in rectangleCorners)
            {
                Vector3 cornerPos = new Vector3(corner.x * blockSize, 0.2f, corner.y * blockSize);
                Gizmos.DrawSphere(cornerPos, 0.4f);
                Gizmos.DrawWireSphere(cornerPos, 0.5f);
            }
        }

        // 绘制方块朝向指示
        if (Application.isPlaying)
        {
            GameObject blocksParent = GameObject.Find("RectangleBlocks");
            if (blocksParent != null)
            {
                foreach (Transform block in blocksParent.transform)
                {
                    Vector3 blockPos = block.position + Vector3.up * 0.3f;
                    Vector3 forward = block.forward * 0.5f;

                    // 用不同颜色表示不同类型的方块
                    if (block.name.Contains("Corner_NonInteractive"))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(blockPos, blockPos + forward);
                        Gizmos.DrawSphere(blockPos + forward * 0.8f, 0.1f);
                    }
                    else if (block.name.Contains("NonInteractive"))
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawLine(blockPos, blockPos + forward * 0.7f);
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(blockPos, blockPos + forward * 0.6f);
                        Gizmos.DrawWireSphere(blockPos + forward * 0.5f, 0.08f);
                    }
                }
            }
        }
    }

    //void OnGUI()
    //{
    //    if (!Application.isPlaying) return;

    //    GUIStyle style = new GUIStyle(GUI.skin.label);
    //    style.fontSize = 14;
    //    style.normal.textColor = Color.white;

    //    // 构建显示信息
    //    string info = "=== 矩形关卡 ===\n";
    //    info += $"尺寸: {rectangleInfo.width} x {rectangleInfo.height}\n";
    //    info += $"顶点位置: \n";
    //    for (int i = 0; i < rectangleCorners.Count; i++)
    //    {
    //        var corner = rectangleCorners[i];
    //        string cornerName = i switch
    //        {
    //            0 => "左下",
    //            1 => "右下",
    //            2 => "右上",
    //            3 => "左上",
    //            _ => "顶点"
    //        };
    //        info += $"  {cornerName}: ({corner.x}, {corner.y})\n";
    //    }
    //    info += $"总方块数: {wallPositions.Count}\n";
    //    info += $"玩家位置: {playerSpawnPosition:F1}\n";
    //    info += "边上方块面朝内，四角朝北\n";
    //    info += "按 R 重新生成";

    //    // 绘制背景框
    //    GUI.Box(new Rect(10, 10, 320, 160), "");

    //    // 绘制文本
    //    GUI.Label(new Rect(15, 15, 310, 150), info, style);
    //}
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取墙体位置列表
    /// </summary>
    public List<Vector2Int> GetWallPositions()
    {
        return new List<Vector2Int>(wallPositions);
    }

    /// <summary>
    /// 获取矩形角点
    /// </summary>
    public List<Vector2Int> GetRectangleCorners()
    {
        return new List<Vector2Int>(rectangleCorners);
    }

    /// <summary>
    /// 获取矩形信息
    /// </summary>
    //public RectangleInfo GetRectangleInfo()
    //{
    //    return rectangleInfo;
    //}

    /// <summary>
    /// 获取地图中心
    /// </summary>
    public Vector3 GetMapCenter()
    {
        return mapCenter;
    }

    /// <summary>
    /// 获取玩家生成位置
    /// </summary>
    public Vector3 GetPlayerSpawnPosition()
    {
        return playerSpawnPosition;
    }

    /// <summary>
    /// 获取总方块数
    /// </summary>
    public int GetTotalBlocks()
    {
        return wallPositions.Count;
    }

    /// <summary>
    /// 检查位置是否是顶点
    /// </summary>
    public bool IsPositionCorner(Vector2Int position)
    {
        return IsCornerPosition(position);
    }

    /// <summary>
    /// 计算位置的面朝内旋转
    /// </summary>
    public Quaternion GetInwardRotationForPosition(Vector2Int position)
    {
        return CalculateInwardRotation(position);
    }

    /// <summary>
    /// 手动生成地图（供其他脚本调用）
    /// </summary>
    public void GenerateMap()
    {
        GenerateCompleteSystem();
    }

    /// <summary>
    /// 手动清空地图（供其他脚本调用）
    /// </summary>
    public void ClearMap()
    {
        ClearExistingSystem();
    }

    
    /// <summary>
    /// 获取当前关卡的目标分数
    /// </summary>
    public int GetCurrentTargetScore()
    {
        return currentTargetScore;
    }
    #endregion
}