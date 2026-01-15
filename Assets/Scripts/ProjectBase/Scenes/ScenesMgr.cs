using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换模块
/// 知识点
/// 1.场景异步加载
/// 2.协程
/// 3.委托
/// </summary>
public class ScenesMgr : BaseManager<ScenesMgr>
{
    private const string LOADING_SCENE_NAME = "LoadScene";
    /// <summary>
    /// 切换场景 同步
    /// </summary>
    /// <param name="name"></param>
    public void LoadScene(string name, UnityAction fun)
    {
        //场景同步加载
        SceneManager.LoadScene(name);
        //加载完成过后 才会去执行fun
        fun();
    }

    /// <summary>
    /// 提供给外部的 异步加载的接口方法
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fun"></param>
    public void LoadSceneAsyn(string name, UnityAction fun)
    {
        MonoMgr.GetInstance().StartCoroutine(ReallyLoadSceneAsyn(name, fun));
    }

    /// <summary>
    /// 协程异步加载场景
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fun"></param>
    /// <returns></returns>
    private IEnumerator ReallyLoadSceneAsyn(string name, UnityAction fun)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        //可以得到场景加载的一个进度
        while(!ao.isDone)
        {
            //事件中心 向外分发 进度情况  外面想用就用
            EventCenter.GetInstance().EventTrigger("进度条更新", ao.progress);
            //这里面去更新进度条
            yield return ao.progress;
        }
        //加载完成过后 才会去执行fun
        fun();
    }

    /// <summary>
    /// 重新加载当前场景并显示加载场景
    /// </summary>
    public void ReloadCurrentWithLoadingScene(UnityAction onComplete = null)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneWithLoadingScene(currentScene, onComplete);
    }

    /// <summary>
    /// 加载指定场景并显示加载场景作为过渡
    /// </summary>
    public void LoadSceneWithLoadingScene(string targetSceneName, UnityAction onComplete = null)
    {
        MonoMgr.GetInstance().StartCoroutine(
            LoadWithLoadingSceneCoroutine(targetSceneName, onComplete)
        );
    }

    /// <summary>
    /// 协程：先加载加载场景，然后异步加载目标场景
    /// </summary>
    private IEnumerator LoadWithLoadingSceneCoroutine(string targetSceneName, UnityAction onComplete)
    {
        // 异步加载加载场景
        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Single);
        loadLoadingScene.allowSceneActivation = false;

        while (loadLoadingScene.progress < 0.9f)
        {
            yield return null;
        }
        loadLoadingScene.allowSceneActivation = true;

        // 等待加载场景完全加载
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        // 在加载场景中开始异步加载目标场景
        yield return new WaitForSeconds(0.5f); // 给加载场景一点显示时间

        // 触发事件告诉加载场景开始加载目标场景
        EventCenter.GetInstance().EventTrigger("StartLoadingTargetScene", targetSceneName);

        // 等待加载场景处理完成（它应该调用下面的LoadTargetSceneFromLoading）
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// 从加载场景中调用，开始加载目标场景
    /// </summary>
    public void LoadTargetSceneFromLoading(string targetSceneName)
    {
        MonoMgr.GetInstance().StartCoroutine(
            LoadTargetSceneCoroutine(targetSceneName)
        );
    }

    /// <summary>
    /// 从加载场景异步加载目标场景
    /// </summary>
    private IEnumerator LoadTargetSceneCoroutine(string targetSceneName)
    {
        // 开始异步加载目标场景
        AsyncOperation ao = SceneManager.LoadSceneAsync(targetSceneName);
        ao.allowSceneActivation = false; // 先不激活

        float timer = 0f;
        float minLoadingTime = 2f; // 最小加载时间，确保玩家能看到加载界面

        while (!ao.isDone)
        {
            timer += Time.deltaTime;

            // Unity的加载进度只到0.9
            float realProgress = ao.progress / 0.9f;

            // 考虑最小加载时间，防止进度条瞬间完成
            float displayProgress = Mathf.Min(realProgress, timer / minLoadingTime);

            // 更新进度（0-1范围）
            EventCenter.GetInstance().EventTrigger("UpdateLoadingProgress", displayProgress);

            // 当实际加载完成且达到最小加载时间时
            if (ao.progress >= 0.9f && timer >= minLoadingTime)
            {
                // 额外等待一下，让进度条显示100%
                yield return new WaitForSeconds(0.3f);

                // 允许激活场景
                ao.allowSceneActivation = true;
            }

            yield return null;
        }

        // 加载完成，可以触发一些事件
        EventCenter.GetInstance().EventTrigger("OnTargetSceneLoaded", targetSceneName);
    }

}
