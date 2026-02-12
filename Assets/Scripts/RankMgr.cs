using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RankMgr : SingletonAutoMono<RankMgr>
{
    private const string RANK_FILE_NAME = "rank_data.json";
    private const int RANK_MAX_COUNT = 10; 

    // 获取持久化文件路径
    private string GetRankFilePath()
    {
        return Path.Combine(Application.persistentDataPath, RANK_FILE_NAME);
    }

    /// <summary>
    /// 添加休闲模式记录
    /// </summary>
    public void AddCasualRank(int dishesCount)
    {
        RankListWrapper wrapper = LoadRankData();
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        wrapper.rankList.Add(RankData.CreateCasualData(dishesCount, currentTime));
        SortCasualRank(wrapper.rankList);
        // 截断超出数量的记录（仅保留前10条休闲模式数据）
        TruncateCasualRank(wrapper.rankList);
        SaveRankData(wrapper);
    }

    /// <summary>
    /// 添加挑战模式记录（分数+是否通关）
    /// </summary>
    public void AddChallengingRank(int score)
    {
        RankListWrapper wrapper = LoadRankData();
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        wrapper.rankList.Add(RankData.CreateChallengingData(score, currentTime));
        SortChallengingRank(wrapper.rankList);
        TruncateChallengingRank(wrapper.rankList);
        SaveRankData(wrapper);
    }

    /// <summary>
    /// 获取休闲模式排行榜
    /// </summary>
    public List<RankData> GetCasualRankList()
    {
        List<RankData> allRank = LoadRankData().rankList;
        List<RankData> casualRank = allRank.FindAll(d => d.GetGameMode() == GameModeMgr.GameMode.Casual);
        SortCasualRank(casualRank); // 确保排序正确
        return casualRank;
    }

    /// <summary>
    /// 获取挑战模式排行榜
    /// </summary>
    public List<RankData> GetChallengingRankList()
    {
        List<RankData> allRank = LoadRankData().rankList;
        List<RankData> challengingRank = allRank.FindAll(d => d.GetGameMode() == GameModeMgr.GameMode.Challenging);
        SortChallengingRank(challengingRank); // 确保排序正确
        return challengingRank;
    }

    // 休闲模式排序：按菜品数降序
    private void SortCasualRank(List<RankData> list)
    {
        list.Sort((a, b) =>
        {
            if (a.GetGameMode() != GameModeMgr.GameMode.Casual) return 1;
            if (b.GetGameMode() != GameModeMgr.GameMode.Casual) return -1;
            return b.dishesCount.CompareTo(a.dishesCount);
        });
    }

    
    private void SortChallengingRank(List<RankData> list)
    {
        list.Sort((a, b) =>
        {
            if (a.GetGameMode() != GameModeMgr.GameMode.Challenging) return 1;
            if (b.GetGameMode() != GameModeMgr.GameMode.Challenging) return -1;
            return b.score.CompareTo(a.score);
        });
    }

    // 截断休闲模式记录
    private void TruncateCasualRank(List<RankData> list)
    {
        List<RankData> casualRank = list.FindAll(d => d.GetGameMode() == GameModeMgr.GameMode.Casual);
        if (casualRank.Count > RANK_MAX_COUNT)
        {
            // 找到需要删除的休闲模式记录（超出10条的部分）
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].GetGameMode() == GameModeMgr.GameMode.Casual && casualRank.Count > RANK_MAX_COUNT)
                {
                    list.RemoveAt(i);
                    casualRank.RemoveAt(casualRank.Count - 1);
                }
            }
        }
    }

    // 截断挑战模式记录
    private void TruncateChallengingRank(List<RankData> list)
    {
        List<RankData> challengingRank = list.FindAll(d => d.GetGameMode() == GameModeMgr.GameMode.Challenging);
        if (challengingRank.Count > RANK_MAX_COUNT)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].GetGameMode() == GameModeMgr.GameMode.Challenging && challengingRank.Count > RANK_MAX_COUNT)
                {
                    list.RemoveAt(i);
                    challengingRank.RemoveAt(challengingRank.Count - 1);
                }
            }
        }
    }

    // 加载JSON数据
    private RankListWrapper LoadRankData()
    {
        string path = GetRankFilePath();
        if (!File.Exists(path)) return new RankListWrapper();

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<RankListWrapper>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"加载排行榜失败：{e.Message}");
            return new RankListWrapper();
        }
    }

    // 保存JSON数据
    private void SaveRankData(RankListWrapper wrapper)
    {
        try
        {
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(GetRankFilePath(), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存排行榜失败：{e.Message}");
        }
    }

    // 清空排行榜
    public void ClearAllRank()
    {
        SaveRankData(new RankListWrapper());
    }
}