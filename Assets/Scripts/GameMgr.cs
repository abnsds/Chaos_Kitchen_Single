using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : SingletonMono<GameMgr>
{

    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    public event EventHandler OnGameWin;



    private enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
        GameWin,
    }
    

    private State state;
    
    private float countdownToStartTimer = 3f;
    private float gamePlayingTimer;
    private float gamePlayingTimeMax = 10f;

    private bool isGamePaused = false;


    private float lastAddedRandomTime;
    public float LastAddedRandomTime => lastAddedRandomTime;

    private const string PREFS_CURRENT_SCORE = "CurrentLevelScore";
    private const string PREFS_HIGHEST_SCORE = "HighestScore";
    private const string PREFS_CURRENT_LEVEL = "CurrentLevel";

    protected override void Awake()
    {
        base.Awake();
        state = State.WaitingToStart;
        InitPlayerPrefs();
    }

    private void InitPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey(PREFS_CURRENT_SCORE)) PlayerPrefs.SetInt(PREFS_CURRENT_SCORE, 0);
        if (!PlayerPrefs.HasKey(PREFS_HIGHEST_SCORE)) PlayerPrefs.SetInt(PREFS_HIGHEST_SCORE, 0);
        if (!PlayerPrefs.HasKey(PREFS_CURRENT_LEVEL)) PlayerPrefs.SetInt(PREFS_CURRENT_LEVEL, 1);
        PlayerPrefs.Save();
    }

    private void DeliveryMgr_OnGameWin(object sender, EventArgs e)
    {
        if (GameModeMgr.GetInstance().GetGameMode() != GameModeMgr.GameMode.Challenging) return;
        TriggerGameWin();
    }

    private void TriggerGameWin()
    {
        if (state != State.GamePlaying) return;

        state = State.GameWin;
        PauseGame();
        
        int currentScore = DeliveryMgr.GetInstance().GetTotalScore();
        RankMgr.GetInstance().AddChallengingRank(currentScore);
        PlayerPrefs.SetInt(PREFS_CURRENT_SCORE, currentScore);
        int currentLevel = PlayerPrefs.GetInt(PREFS_CURRENT_LEVEL);
        PlayerPrefs.SetInt(PREFS_CURRENT_LEVEL, currentLevel + 1);
        PlayerPrefs.Save();

        OnGameWin?.Invoke(this, EventArgs.Empty);
        //OnStateChanged?.Invoke(this, EventArgs.Empty);
        
    }
    private void Start()
    {
        GameInputMgr.GetInstance().OnPauseAction += GameInput_OnPauseAction;
        GameInputMgr.GetInstance().OnInteractAction += GameInput_OnInteractAction;
        if (GameModeMgr.GetInstance().GetGameMode() == GameModeMgr.GameMode.Challenging)
        {
            DeliveryMgr.GetInstance().OnGameWin += DeliveryMgr_OnGameWin;
        }
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if(state == State.WaitingToStart)
        {
            state = State.CountdownToStart;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        PauseGame();
    }

    

    private void Update()
    {
        switch (state)
        {
            case State.WaitingToStart:
               
                break;
            case State.CountdownToStart:
                countdownToStartTimer -= Time.deltaTime;
                if(countdownToStartTimer < 0f)
                {
                    state = State.GamePlaying;
                    gamePlayingTimer = gamePlayingTimeMax;
                    OnStateChanged?.Invoke(this, EventArgs.Empty);

                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if(gamePlayingTimer < 0f)
                {
                    state = State.GameOver;
                    OnStateChanged?.Invoke(this, EventArgs.Empty);
                    Time.timeScale = 0f;

                    if (GameModeMgr.GetInstance().GetGameMode() == GameModeMgr.GameMode.Challenging)
                    {
                        // 挑战模式失败：记录分数+失败
                        int currentScore = DeliveryMgr.GetInstance().GetTotalScore();
                        RankMgr.GetInstance().AddChallengingRank(currentScore);

                        // 原有存储逻辑
                        int highestScore = PlayerPrefs.GetInt(PREFS_HIGHEST_SCORE);
                        if (currentScore > highestScore) PlayerPrefs.SetInt(PREFS_HIGHEST_SCORE, currentScore);
                        PlayerPrefs.SetInt(PREFS_CURRENT_SCORE, 0);
                        PlayerPrefs.SetInt(PREFS_CURRENT_LEVEL, 1);
                        PlayerPrefs.Save();

                    }
                    else
                    {
                        // 休闲模式失败（时间到）：记录菜品数+失败
                        int dishesCount = DeliveryMgr.GetInstance().GetSuccessfulRecipesAmount();
                        RankMgr.GetInstance().AddCasualRank(dishesCount);

                    }
                }
                break;
            case State.GameOver:
                
                break;
        }
        //Debug.Log(state);
    }

    public bool IsGamePlaying()
    {
        return state == State.GamePlaying;
    }
    public bool IsGameOver()
    {
        return state == State.GameOver;
    }
    public bool IsGameWin()
    {
        return state == State.GameWin;
    }

    public bool IsCountdownToStartActive()
    {
        return state == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer()
    {
        return countdownToStartTimer;
    }

   

    public float GetPlayingTimerNormalized()
    {
        return 1 - (gamePlayingTimer / gamePlayingTimeMax);
    }
    public int GetCurrentLevel()
    {
        return PlayerPrefs.GetInt(PREFS_CURRENT_LEVEL);
    }
    public int GetLastLevelScore()
    {
        return PlayerPrefs.GetInt(PREFS_CURRENT_SCORE);
    }
    public int GetHighestScore()
    {
        return PlayerPrefs.GetInt(PREFS_HIGHEST_SCORE);
    }



    public void PauseGame()
    {
        isGamePaused = !isGamePaused;
        if (isGamePaused)
        {
            Time.timeScale = 0f;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);

        }

    }

    public void AddRandomGameTime()
    {
        if (state != State.GamePlaying)
        {
            Debug.LogWarning("无法增加游戏时间：当前游戏状态不是进行中");
            lastAddedRandomTime = 0; 
            return;
        }

        float randomAddTime = UnityEngine.Random.Range(5f, 10f);

        lastAddedRandomTime = randomAddTime;

        gamePlayingTimer += randomAddTime;
        // gamePlayingTimer = Mathf.Min(gamePlayingTimer, gamePlayingTimeMax * 2);
        
    }
    private void OnDestroy()
    {
        
        DeliveryMgr.GetInstance().OnGameWin -= DeliveryMgr_OnGameWin;
        
    }

}
