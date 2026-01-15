using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image progressBarImage;
    [SerializeField] private TextMeshProUGUI progressText;
    //[SerializeField] private Text hintText;
    //[SerializeField] private GameObject loadingTipsPanel;

   
    //[SerializeField] private string[] loadingTips;

    private string targetSceneName;

    private void Start()
    {
        // 初始化UI
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "0%";
        }

        // 显示随机提示
        //ShowRandomTip();

        // 订阅事件
        EventCenter.GetInstance().AddEventListener<string>("StartLoadingTargetScene", OnStartLoadingTargetScene);
        EventCenter.GetInstance().AddEventListener<float>("UpdateLoadingProgress", OnUpdateLoadingProgress);
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        EventCenter.GetInstance().RemoveEventListener<string>("StartLoadingTargetScene", OnStartLoadingTargetScene);
        EventCenter.GetInstance().RemoveEventListener<float>("UpdateLoadingProgress", OnUpdateLoadingProgress);
    }

    /// <summary>
    /// 开始加载目标场景事件
    /// </summary>
    private void OnStartLoadingTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        //Debug.Log($"开始加载目标场景: {sceneName}");

        // 开始加载目标场景
        ScenesMgr.GetInstance().LoadTargetSceneFromLoading(sceneName);
    }

    /// <summary>
    /// 更新加载进度事件
    /// </summary>
    private void OnUpdateLoadingProgress(float progress)
    {
        // 确保progress在0-1之间
        float clampedProgress = Mathf.Clamp01(progress);

        // 更新进度条
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = clampedProgress;
        }

        // 更新进度文本
        if (progressText != null)
        {
            progressText.text = $"{(clampedProgress * 100):F0}%";
        }

        // 可以在这里添加进度到达某个点时的额外逻辑
        if (clampedProgress >= 0.3f && clampedProgress < 0.4f)
        {
            // 可以切换提示等
        }

        // 进度达到100%时的处理
        if (clampedProgress >= 0.99f)
        {
            OnLoadingComplete();
        }
    }

    ///// <summary>
    ///// 显示随机加载提示
    ///// </summary>
    //private void ShowRandomTip()
    //{
    //    if (loadingTips != null && loadingTips.Length > 0 && hintText != null)
    //    {
    //        int randomIndex = Random.Range(0, loadingTips.Length);
    //        hintText.text = loadingTips[randomIndex];
    //    }
    //}

    /// <summary>
    /// 加载完成时的处理
    /// </summary>
    private void OnLoadingComplete()
    {
        // 可以在这里显示"加载完成，即将进入游戏"等提示
        if (progressText != null)
        {
            progressText.text = "100%";
        }

        // 可以添加一个短暂的延迟，让玩家看到100%
        // 场景会在场景管理器中自动切换
    }
}
