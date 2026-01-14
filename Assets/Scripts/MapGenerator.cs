using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode] // 关键特性：让脚本在编辑模式下执行
public class MapGenerator : MonoBehaviour
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

    [Header("调试显示")]
    public bool showDebug = true; // 显示调试信息

    new private const string camera = "camera";

    // 私有变量
    private List<Vector2Int> wallPositions = new List<Vector2Int>();
    private List<Vector2Int> rectangleCorners = new List<Vector2Int>();
    private GameObject playerInstance;
    private Vector3 playerSpawnPosition;
    private Vector3 mapCenter = Vector3.zero;
    private List<int> shuffledBlockIndices = new List<int>();
    private int currentBlockIndex = 0;
    private RectangleInfo rectangleInfo;
    private Dictionary<Vector2Int, bool> cornerPositions = new Dictionary<Vector2Int, bool>();

    // 矩形信息结构
    private struct RectangleInfo
    {
        public Vector2Int position; // 左下角位置
        public int width;           // 宽度（格子数）
        public int height;          // 高度（格子数）
    }

    // ===================== 核心新增：编辑器右键菜单 =====================
    [ContextMenu("生成矩形地图【编辑模式生效】")]
    public void GenerateCompleteSystem()
    {
        ClearExistingSystem();

        GenerateRandomRectangle();
        FillRectangleEdges();
        CalculateMapCenter();
        CalculatePlayerSpawnPosition();
        PrepareBlockIndices();
        InstantiateAllBlocks();
        SpawnPlayer();
    }

    [ContextMenu("清空场景中生成的地图")]
    public void ClearMapManually()
    {
        ClearExistingSystem();
    }

    #region 矩形生成部分
    void GenerateRandomRectangle()
    {
        int width = Random.Range(minWidth, maxWidth + 1);
        int height = Random.Range(minHeight, maxHeight + 1);

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

        int x = Random.Range(1, maxX);
        int y = Random.Range(1, maxY);

        rectangleInfo = new RectangleInfo
        {
            position = new Vector2Int(x, y),
            width = width,
            height = height
        };

        rectangleCorners.Clear();
        cornerPositions.Clear();

        Vector2Int[] corners = new Vector2Int[4]
        {
            new Vector2Int(x, y),
            new Vector2Int(x + width - 1, y),
            new Vector2Int(x + width - 1, y + height - 1),
            new Vector2Int(x, y + height - 1)
        };

        foreach (var corner in corners)
        {
            rectangleCorners.Add(corner);
            cornerPositions[corner] = true;
        }
    }

    void FillRectangleEdges()
    {
        wallPositions.Clear();

        int x = rectangleInfo.position.x;
        int y = rectangleInfo.position.y;
        int width = rectangleInfo.width;
        int height = rectangleInfo.height;

        for (int i = 0; i < width; i++) AddWallPosition(new Vector2Int(x + i, y));
        for (int i = 0; i < width; i++) AddWallPosition(new Vector2Int(x + i, y + height - 1));
        for (int j = 1; j < height - 1; j++) AddWallPosition(new Vector2Int(x, y + j));
        for (int j = 1; j < height - 1; j++) AddWallPosition(new Vector2Int(x + width - 1, y + j));

        RemoveDuplicatePositions();
    }

    void AddWallPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
        {
            Debug.LogWarning($"位置 {pos} 超出地图范围");
            return;
        }
        wallPositions.Add(pos);
    }

    void RemoveDuplicatePositions()
    {
        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>();
        List<Vector2Int> cleanedPositions = new List<Vector2Int>();

        foreach (var pos in wallPositions)
        {
            if (uniquePositions.Add(pos)) cleanedPositions.Add(pos);
        }
        wallPositions = cleanedPositions;
    }

    void CalculateMapCenter()
    {
        float centerX = rectangleInfo.position.x + rectangleInfo.width / 2f - 0.5f;
        float centerY = rectangleInfo.position.y + rectangleInfo.height / 2f - 0.5f;

        mapCenter = new Vector3(centerX * blockSize, 0, centerY * blockSize);
    }
    #endregion

    #region 玩家生成部分
    void CalculatePlayerSpawnPosition()
    {
        float centerX = rectangleInfo.position.x + rectangleInfo.width / 2f - 0.5f;
        float centerY = rectangleInfo.position.y + rectangleInfo.height / 2f - 0.5f;

        playerSpawnPosition = new Vector3(centerX * blockSize, 0f, centerY * blockSize);
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("未分配玩家预制体，跳过生成玩家");
            return;
        }

        playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity, transform);
        playerInstance.name = "Player";
        playerInstance.hideFlags = HideFlags.None; // 标记为场景物体，可编辑

        
        if (EventCenter.GetInstance() != null)
        {
            EventCenter.GetInstance().EventTrigger(camera, playerInstance.transform);
        }
    }
    #endregion

    #region 方块实例化部分（核心规则完全保留）
    void PrepareBlockIndices()
    {
        shuffledBlockIndices.Clear();
        currentBlockIndex = 0;

        if (allBlockPrefabs == null || allBlockPrefabs.Length == 0)
        {
            Debug.LogError("没有分配方块预制体！");
            return;
        }

        for (int i = 0; i < allBlockPrefabs.Length; i++) shuffledBlockIndices.Add(i);
        ShuffleList(shuffledBlockIndices);
    }

    int GetNextBlockIndex()
    {
        if (shuffledBlockIndices.Count == 0) return Random.Range(0, allBlockPrefabs.Length);

        if (currentBlockIndex >= shuffledBlockIndices.Count)
        {
            ShuffleList(shuffledBlockIndices);
            currentBlockIndex = 0;
        }

        int index = shuffledBlockIndices[currentBlockIndex];
        currentBlockIndex++;
        return index;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    void InstantiateAllBlocks()
    {
        if (wallPositions.Count == 0) return;

        GameObject blocksParent = GameObject.Find("RectangleBlocks");
        if (blocksParent == null)
        {
            blocksParent = new GameObject("RectangleBlocks");
            blocksParent.transform.SetParent(transform);
        }
        blocksParent.hideFlags = HideFlags.None;

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

        HashSet<int> nonInteractiveIndices = GetNonInteractiveBlockPositions();
        Dictionary<int, int> blockTypeCount = new Dictionary<int, int>();
        for (int i = 0; i < allBlockPrefabs.Length; i++) blockTypeCount[i] = 0;

        for (int i = 0; i < wallPositions.Count; i++)
        {
            Vector2Int gridPos = wallPositions[i];
            Vector3 worldPos = new Vector3(gridPos.x * blockSize, 0, gridPos.y * blockSize);

            GameObject blockInstance;
            string blockName;
            bool isCorner = IsCornerPosition(gridPos);
            Quaternion rotation = CalculateInwardRotation(gridPos);

            if (isCorner || nonInteractiveIndices.Contains(i))
            {
                blockInstance = Instantiate(nonInteractiveBlockPrefab, worldPos, rotation, blocksParent.transform);
                blockInstance.hideFlags = HideFlags.None;

                if (isCorner)
                {
                    blockName = $"Corner_NonInteractive_{gridPos.x}_{gridPos.y}";
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
                int blockIndex = GetNextBlockIndex();
                GameObject blockPrefab = allBlockPrefabs[blockIndex];
                blockInstance = Instantiate(blockPrefab, worldPos, rotation, blocksParent.transform);
                blockInstance.hideFlags = HideFlags.None;

                blockTypeCount[blockIndex]++;
                blockName = $"Interactive_{blockIndex}_{gridPos.x}_{gridPos.y}";
            }
            blockInstance.name = blockName;
        }
    }

    Quaternion CalculateInwardRotation(Vector2Int gridPos)
    {
        int x = rectangleInfo.position.x;
        int y = rectangleInfo.position.y;
        int width = rectangleInfo.width;
        int height = rectangleInfo.height;

        if (gridPos.y == y) return Quaternion.Euler(0, 0, 0);
        else if (gridPos.y == y + height - 1) return Quaternion.Euler(0, 180, 0);
        else if (gridPos.x == x) return Quaternion.Euler(0, 90, 0);
        else if (gridPos.x == x + width - 1) return Quaternion.Euler(0, 270, 0);
        else if (IsCornerPosition(gridPos)) return Quaternion.Euler(0, 0, 0);

        return Quaternion.Euler(0, 0, 0);
    }

    void MarkCornerBlock(GameObject block, Vector2Int position)
    {
        var renderer = block.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = new Color(0.5f, 0.2f, 0.2f);
        block.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

    void MarkNonInteractiveBlock(GameObject block)
    {
        var renderer = block.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = Color.gray * 0.7f;
    }

    HashSet<int> GetNonInteractiveBlockPositions()
    {
        HashSet<int> nonInteractiveIndices = new HashSet<int>();

        for (int i = 0; i < wallPositions.Count; i++)
        {
            if (IsCornerPosition(wallPositions[i])) nonInteractiveIndices.Add(i);
        }

        int nonInteractiveCount = Random.Range(minNonInteractiveBlocks, maxNonInteractiveBlocks + 1);
        nonInteractiveCount = Mathf.Max(nonInteractiveCount, 4);
        nonInteractiveCount = Mathf.Min(nonInteractiveCount, wallPositions.Count / 2);

        while (nonInteractiveIndices.Count < nonInteractiveCount)
        {
            int randomIndex = Random.Range(0, wallPositions.Count);
            if (!nonInteractiveIndices.Contains(randomIndex)) nonInteractiveIndices.Add(randomIndex);
        }
        return nonInteractiveIndices;
    }

    bool IsCornerPosition(Vector2Int position)
    {
        return cornerPositions.ContainsKey(position);
    }
    #endregion

    #region 核心修改：清空逻辑（编辑模式兼容）
    void ClearExistingSystem()
    {
        // 遍历子物体删除，兼容编辑模式和运行模式
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "RectangleBlocks" || child.name == "Player")
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        wallPositions.Clear();
        rectangleCorners.Clear();
        cornerPositions.Clear();
        shuffledBlockIndices.Clear();
        currentBlockIndex = 0;
        playerInstance = null;
    }
    #endregion

    

    #region 公共方法（保留原接口）
    public List<Vector2Int> GetWallPositions() => new List<Vector2Int>(wallPositions);
    public List<Vector2Int> GetRectangleCorners() => new List<Vector2Int>(rectangleCorners);
    public Vector3 GetMapCenter() => mapCenter;
    public Vector3 GetPlayerSpawnPosition() => playerSpawnPosition;
    public int GetTotalBlocks() => wallPositions.Count;
    public bool IsPositionCorner(Vector2Int position) => IsCornerPosition(position);
    public Quaternion GetInwardRotationForPosition(Vector2Int position) => CalculateInwardRotation(position);
    #endregion
}