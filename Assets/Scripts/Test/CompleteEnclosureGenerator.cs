// CompleteEnclosureGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public class CompleteEnclosureGenerator : MonoBehaviour
{
    [Header("方块预制体")]
    public GameObject[] blockPrefabs; // 12种方块预制体

    [Header("生成设置")]
    public Vector2Int gridSize = new Vector2Int(40, 40); // 网格大小
    public float blockSize = 1f; // 方块大小

    [Header("图形参数")]
    [Range(4, 8)] public int minSides = 4; // 最小边数
    [Range(5, 12)] public int maxSides = 8; // 最大边数
    [Range(5, 15)] public int minSideLength = 5; // 最小边长（格子数）
    [Range(8, 20)] public int maxSideLength = 12; // 最大边长（格子数）

    [Header("生成控制")]
    public bool generateOnStart = true;
    public KeyCode regenerateKey = KeyCode.R;
    public bool showDebugGizmos = true;

    private List<Vector2Int> wallPositions = new List<Vector2Int>();
    private List<Vector2Int> polygonVertices = new List<Vector2Int>();

    void Start()
    {
        if (generateOnStart)
        {
            GenerateCompleteEnclosure();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(regenerateKey))
        {
            ClearExistingBlocks();
            GenerateCompleteEnclosure();
        }
    }

    public void GenerateCompleteEnclosure()
    {
        ClearExistingBlocks();
        GenerateRandomConvexPolygon();
        FillAllEdgesWithBlocks();
        InstantiateAllBlocks();
    }

    void GenerateRandomConvexPolygon()
    {
        polygonVertices.Clear();

        // 随机选择中心点
        int centerX = Random.Range(gridSize.x / 4, gridSize.x * 3 / 4);
        int centerY = Random.Range(gridSize.y / 4, gridSize.y * 3 / 4);

        // 随机边数
        int sides = Random.Range(minSides, maxSides + 1);

        // 生成凸多边形的顶点（使用极坐标）
        List<Vector2> tempVertices = new List<Vector2>();
        float radius = Random.Range(minSideLength * 0.8f, maxSideLength * 1.2f);

        // 生成随机角度并排序，确保凸多边形
        List<float> angles = new List<float>();
        for (int i = 0; i < sides; i++)
        {
            angles.Add(Random.Range(0f, 360f));
        }
        angles.Sort();

        // 计算顶点位置
        for (int i = 0; i < sides; i++)
        {
            float angle = angles[i] * Mathf.Deg2Rad;
            float r = radius * Random.Range(0.8f, 1.2f); // 添加一些随机性

            int x = Mathf.RoundToInt(centerX + r * Mathf.Cos(angle));
            int y = Mathf.RoundToInt(centerY + r * Mathf.Sin(angle));

            // 确保在网格范围内
            x = Mathf.Clamp(x, 2, gridSize.x - 3);
            y = Mathf.Clamp(y, 2, gridSize.y - 3);

            polygonVertices.Add(new Vector2Int(x, y));
        }

        // 按顺时针或逆时针排序顶点
        SortVerticesClockwise();
    }

    void SortVerticesClockwise()
    {
        if (polygonVertices.Count < 3) return;

        // 计算中心点
        Vector2 center = Vector2.zero;
        foreach (var vertex in polygonVertices)
        {
            center += new Vector2(vertex.x, vertex.y);
        }
        center /= polygonVertices.Count;

        // 按角度排序
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

        // 为每条边填充方块
        for (int i = 0; i < polygonVertices.Count; i++)
        {
            Vector2Int start = polygonVertices[i];
            Vector2Int end = polygonVertices[(i + 1) % polygonVertices.Count];

            // 使用DDA算法确保边完全填满
            FillLineWithBlocks(start, end);
        }

        // 移除重复的方块位置
        RemoveDuplicatePositions();
    }

    void FillLineWithBlocks(Vector2Int start, Vector2Int end)
    {
        int dx = end.x - start.x;
        int dy = end.y - start.y;

        // 计算步数（取绝对值较大者）
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        // 如果步数为0，只需要一个方块
        if (steps == 0)
        {
            AddWallPosition(start);
            return;
        }

        // 计算增量
        float xIncrement = dx / (float)steps;
        float yIncrement = dy / (float)steps;

        float x = start.x;
        float y = start.y;

        // 添加所有方块
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
        // 检查边界
        if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
        {
            Debug.LogWarning($"位置 {pos} 超出网格范围");
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
            if (uniquePositions.Add(pos))
            {
                cleanedPositions.Add(pos);
            }
        }

        wallPositions = cleanedPositions;
    }

    void InstantiateAllBlocks()
    {
        if (wallPositions.Count == 0)
        {
            Debug.LogWarning("没有要生成的方块位置");
            return;
        }

        // 创建父对象
        GameObject enclosureParent = new GameObject("CompleteEnclosure");
        enclosureParent.transform.SetParent(transform);
        enclosureParent.transform.localPosition = Vector3.zero;

        // 统计每种方块的使用次数
        int[] blockUsage = new int[blockPrefabs.Length];

        // 实例化所有方块
        for (int i = 0; i < wallPositions.Count; i++)
        {
            Vector2Int gridPos = wallPositions[i];

            // 计算世界位置（y轴为0）
            Vector3 worldPos = new Vector3(
                gridPos.x * blockSize,
                0,
                gridPos.y * blockSize
            );

            // 选择方块类型（可以基于位置或其他规则）
            int blockIndex = SelectBlockType(gridPos, i);
            GameObject blockPrefab = blockPrefabs[blockIndex];
            blockUsage[blockIndex]++;

            // 实例化方块
            if (blockPrefab != null)
            {
                GameObject block = Instantiate(blockPrefab, worldPos, Quaternion.identity, enclosureParent.transform);
                block.name = $"Block_{gridPos.x}_{gridPos.y}";

                // 可选：随机旋转以增加变化
                if (Random.value > 0.7f)
                {
                    block.transform.Rotate(0, Random.Range(0, 4) * 90, 0);
                }
            }
            else
            {
                Debug.LogError($"方块预制体 {blockIndex} 未分配");
            }
        }

        // 输出统计信息
        Debug.Log($"生成完成！共生成 {wallPositions.Count} 个方块");
        for (int i = 0; i < blockUsage.Length; i++)
        {
            if (blockUsage[i] > 0)
            {
                Debug.Log($"方块类型 {i}: {blockUsage[i]} 个");
            }
        }
    }

    int SelectBlockType(Vector2Int position, int index)
    {
        // 方法1：完全随机
        // return Random.Range(0, blockPrefabs.Length);

        // 方法2：基于索引的循环使用
        // return index % blockPrefabs.Length;

        // 方法3：基于位置的哈希值
        int hash = (position.x * 73856093) ^ (position.y * 19349663);
        return Mathf.Abs(hash) % blockPrefabs.Length;

        // 方法4：根据是否是顶点选择特殊方块
        /*if (IsVertexPosition(position))
        {
            // 顶点使用前几种方块
            return Mathf.Min(blockPrefabs.Length - 1, 2);
        }
        else
        {
            // 边上的方块使用其他类型
            return Random.Range(3, blockPrefabs.Length);
        }*/
    }

    bool IsVertexPosition(Vector2Int position)
    {
        foreach (var vertex in polygonVertices)
        {
            if (vertex == position)
                return true;
        }
        return false;
    }

    void ClearExistingBlocks()
    {
        // 使用延迟销毁，避免编辑器模式下立即销毁的问题
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }

    #region 调试和可视化
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 绘制网格
        Gizmos.color = Color.gray * 0.3f;
        for (int x = 0; x <= gridSize.x; x++)
        {
            Vector3 start = new Vector3(x * blockSize, 0, 0);
            Vector3 end = new Vector3(x * blockSize, 0, gridSize.y * blockSize);
            Gizmos.DrawLine(start, end);
        }
        for (int y = 0; y <= gridSize.y; y++)
        {
            Vector3 start = new Vector3(0, 0, y * blockSize);
            Vector3 end = new Vector3(gridSize.x * blockSize, 0, y * blockSize);
            Gizmos.DrawLine(start, end);
        }

        // 绘制多边形顶点
        Gizmos.color = Color.red;
        foreach (var vertex in polygonVertices)
        {
            Vector3 pos = new Vector3(vertex.x * blockSize, 0.5f, vertex.y * blockSize);
            Gizmos.DrawSphere(pos, 0.3f);
        }

        // 绘制多边形边
        Gizmos.color = Color.yellow;
        for (int i = 0; i < polygonVertices.Count; i++)
        {
            Vector2Int start = polygonVertices[i];
            Vector2Int end = polygonVertices[(i + 1) % polygonVertices.Count];

            Vector3 startPos = new Vector3(start.x * blockSize, 0.2f, start.y * blockSize);
            Vector3 endPos = new Vector3(end.x * blockSize, 0.2f, end.y * blockSize);

            Gizmos.DrawLine(startPos, endPos);
        }

        // 绘制方块位置
        Gizmos.color = Color.green;
        foreach (var pos in wallPositions)
        {
            Vector3 center = new Vector3(pos.x * blockSize, 0, pos.y * blockSize);
            Gizmos.DrawWireCube(center, new Vector3(blockSize, 0.1f, blockSize));
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制边界框
        Gizmos.color = Color.blue;
        Vector3 center = new Vector3(gridSize.x * blockSize / 2f, 0, gridSize.y * blockSize / 2f);
        Vector3 size = new Vector3(gridSize.x * blockSize, 0.1f, gridSize.y * blockSize);
        Gizmos.DrawWireCube(center, size);
    }
    #endregion

    #region 编辑器扩展方法
#if UNITY_EDITOR
    [UnityEditor.MenuItem("GameObject/生成封闭图形", false, 10)]
    static void CreateEnclosureGenerator()
    {
        GameObject generator = new GameObject("EnclosureGenerator");
        generator.AddComponent<CompleteEnclosureGenerator>();

        // 添加示例预制体（需要手动替换）
        var comp = generator.GetComponent<CompleteEnclosureGenerator>();
        comp.blockPrefabs = new GameObject[12];

        UnityEditor.Selection.activeGameObject = generator;
    }
#endif
    #endregion
}