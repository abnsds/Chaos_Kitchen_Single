using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MainCameraPlayerFollow : SingletonMono<MainCameraPlayerFollow>
{
    [Header("=== 核心控制 ===")]
    public bool followPlayer = true;
    public bool lookAtPlayer = true;

    [Header("=== 跟随参数 ===")]
    public Vector3 cameraOffset = new Vector3(0f, 2f, -3f); // 玩家偏移
    public Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);   // 看向偏移
    [Range(2f, 20f)] public float followSmoothness = 10f;

    [Header("=== 安全设置 ===")]
    public bool lockYHeight = true;
    public float fixedYHeight = 2f;
    public bool forceRootNode = true;

    // 核心变量
    private Transform playerTarget; // 绑定的玩家
    private Vector3 _fixedCameraPos; // 固定位置（不跟随时用）
    private bool _isPlayerBound = false; // 是否绑定玩家

    protected override void Awake()
    {
        base.Awake();


        // 3. 初始化固定位置
        _fixedCameraPos = transform.position;

        // 4. 【核心】监听生成器的camera事件（对应RectangleMapGenerator的camera常量）
        EventCenter.GetInstance().AddEventListener<Transform>("camera", OnPlayerSpawned);
        //Debug.Log("摄像机已监听camera事件，等待玩家生成...");
    }

    // 【核心】事件回调：收到玩家数据后绑定
    private void OnPlayerSpawned(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogError("摄像机收到的玩家Transform为空！");
            return;
        }

        // 绑定玩家
        playerTarget = playerTransform;
        _isPlayerBound = true;

        // 立即重置摄像机到玩家旁
        ResetCameraToPlayer();
        //Debug.Log($"摄像机绑定玩家成功：{playerTarget.name}，位置：{playerTarget.position}");
    }

    void LateUpdate()
    {
        // 未绑定玩家则直接返回
        if (!_isPlayerBound || playerTarget == null) return;

        // 跟随逻辑
        if (followPlayer)
        {
            UpdateCameraPosition();
        }
        else
        {
            transform.position = _fixedCameraPos;
        }

        // 看向玩家逻辑
        if (lookAtPlayer)
        {
            UpdateCameraLookAt();
        }
    }

    // 更新摄像机位置（平滑跟随）
    private void UpdateCameraPosition()
    {
        Vector3 targetPos = playerTarget.position + cameraOffset;
        // 锁定Y轴高度（可选）
        if (lockYHeight) targetPos.y = fixedYHeight;
        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothness);
    }

    // 更新摄像机朝向（看向玩家）
    private void UpdateCameraLookAt()
    {
        Vector3 lookAtPos = playerTarget.position + lookAtOffset;
        transform.LookAt(lookAtPos);
    }

    // 重置摄像机到玩家旁（立即定位）
    public void ResetCameraToPlayer()
    {
        if (playerTarget == null) return;

        Vector3 targetPos = playerTarget.position + cameraOffset;
        if (lockYHeight) targetPos.y = fixedYHeight;
        transform.position = targetPos;

        // 同步固定位置
        _fixedCameraPos = transform.position;
    }

    // 重要：销毁时取消事件监听（防止内存泄漏）
    private void OnDestroy()
    {
        EventCenter.GetInstance().RemoveEventListener<Transform>("camera", OnPlayerSpawned);
        
    }

    // 容错：如果事件没触发，Update中自动检测玩家
    void Update()
    {
        if (!_isPlayerBound)
        {
            AutoDetectPlayer();
        }
    }

    // 自动检测玩家（兜底逻辑）
    private void AutoDetectPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            OnPlayerSpawned(player.transform);
        }
    }

    // Gizmos调试（可选）
    void OnDrawGizmos()
    {
        if (playerTarget != null)
        {
            // 绘制玩家位置
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(playerTarget.position, 0.3f);

            // 绘制摄像机目标位置
            Gizmos.color = Color.blue;
            Vector3 targetPos = playerTarget.position + cameraOffset;
            if (lockYHeight) targetPos.y = fixedYHeight;
            Gizmos.DrawSphere(targetPos, 0.2f);
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }
}