using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankData
{
    public int score;
    public int dishesCount;
    public string time;
    public int mode;
   
    private RankData() { }
    // 静态工厂
    public static RankData CreateCasualData(int dishesCount, string time)
    {
        return new RankData()
        {
            dishesCount = dishesCount,
            time = time,
            mode = (int)GameModeMgr.GameMode.Casual,
            score = 0
        };
    }

    // 静态工厂方法：创建挑战模式数据
    public static RankData CreateChallengingData(int score, string time)
    {
        return new RankData()
        {
            score = score,
            time = time,
            mode = (int)GameModeMgr.GameMode.Challenging,
            dishesCount = 0
        };
    }

    public GameModeMgr.GameMode GetGameMode()
    {
        return (GameModeMgr.GameMode)mode;
    }
}
// JSON包装类
[Serializable]
public class RankListWrapper
{
    public List<RankData> rankList = new List<RankData>();
}