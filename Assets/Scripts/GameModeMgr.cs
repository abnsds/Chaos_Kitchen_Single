using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeMgr : SingletonAutoMono<GameModeMgr>
{
    public enum GameMode
    {
        Casual,
        Challenging
    }
    private GameMode mode = GameMode.Casual;
    public GameMode GetGameMode()
    {
        return mode;
    }

    public void SetGameMode(GameMode mode)
    {
        this.mode = mode;
    }
}
