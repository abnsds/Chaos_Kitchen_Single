using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryScoreUI : MonoBehaviour
{
    [SerializeField] private Text nowScore;
    [SerializeField] private Text Targetscore;

    private void Start()
    {
        
        UpdateScoreText(0);
        UpdateTargetScoreText(DeliveryMgr.GetInstance().GetTargetScore());

        // 监听分数变更事件
        DeliveryMgr.GetInstance().OnScoreChanged += DeliveryMgr_OnScoreChanged;
        RectangleMapGenerator.GetInstance().OnTargetScoreGenerated += MapGenerator_OnTargetScoreGenerated;


        UpdateScoreText(DeliveryMgr.GetInstance().GetTotalScore());
    }

    private void DeliveryMgr_OnScoreChanged(object sender, int totalScore)
    {
        UpdateScoreText(totalScore);
    }
    private void MapGenerator_OnTargetScoreGenerated(object sender, int newTargetScore)
    {
        UpdateTargetScoreText(newTargetScore);
    }

    private void UpdateTargetScoreText(int targetScore)
    {
        if (Targetscore == null) return;
        Targetscore.text = $"目标分数：{targetScore}";
    }
    // 更新分数文本显示
    private void UpdateScoreText(int score)
    {
        if (nowScore == null)
        {
            Debug.LogError("未赋值ScoreText组件！");
            return;
        }
        nowScore.text = $"当前分数：{score}";
    }

    // 防止内存泄漏，移除事件监听
    private void OnDestroy()
    {
        DeliveryMgr.GetInstance().OnScoreChanged -= DeliveryMgr_OnScoreChanged;
        RectangleMapGenerator.GetInstance().OnTargetScoreGenerated -= MapGenerator_OnTargetScoreGenerated;
        
    }
}
