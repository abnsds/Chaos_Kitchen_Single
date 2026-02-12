using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryMgr : SingletonMono<DeliveryMgr> 
{
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;

    public event EventHandler OnRecipeFailed;
    public event EventHandler OnRecipeSuccess;

    public event EventHandler<int> OnScoreChanged;

    public event EventHandler OnGameWin;

    [SerializeField] private RecipeListSO recipeListSO;
    private List<RecipeSO> waitingRecipeSOList;

    private float spawnRecipeTimer;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipeMax = 4;
    private int successfulRecipesAmount;

    private int targetScore;

    private int totalScore; 
    private bool isGameWon = false;
    protected override void Awake()
    {
        base.Awake();
        waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Start()
    {
        if(GameModeMgr.GetInstance().GetGameMode() == GameModeMgr.GameMode.Challenging)
        {
            RectangleMapGenerator.GetInstance().OnTargetScoreGenerated += MapGenerator_OnTargetScoreGenerated;

            // 初始化目标分数（如果地图已生成）
            targetScore = RectangleMapGenerator.GetInstance().GetCurrentTargetScore();
            totalScore = GameMgr.GetInstance().GetLastLevelScore();

            OnScoreChanged?.Invoke(this, totalScore);
        }
        
    }

    private void MapGenerator_OnTargetScoreGenerated(object sender, int newTargetScore)
    {
        targetScore = newTargetScore;
        isGameWon = false; // 重置通关状态
        //totalScore = 0; // 重置分数
        OnScoreChanged?.Invoke(this, totalScore); // 通知UI分数重置
        //Debug.Log($"DeliveryMgr已同步新目标分数：{targetScore}");
    }
    private void Update()
    {
        spawnRecipeTimer -= Time.deltaTime;
        if(spawnRecipeTimer < 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if(GameMgr.GetInstance().IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipeMax)
            {
                RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count)];
                //Debug.Log(waitingRecipeSO.recipeName);
                waitingRecipeSOList.Add(waitingRecipeSO);

                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
            }
            
        }
    }

    public void DeliverRecipe(Plate plate)
    {
        for(int i = 0; i < waitingRecipeSOList.Count; ++i)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];
            if(waitingRecipeSO.kitchenObjectSOList.Count == plate.GetKitchenObjectSOList().Count)
            {
                bool plateContentsMatchesRecipe = true;
                foreach(var recipekitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    bool ingredientFound = false;
                    foreach(var plateKitchenObjectSO in plate.GetKitchenObjectSOList())
                    {
                        if(plateKitchenObjectSO == recipekitchenObjectSO)
                        {
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound)
                    {
                        plateContentsMatchesRecipe = false;
                    }
                }

                if(plateContentsMatchesRecipe)
                {
                    //Debug.Log("有匹配");
                    successfulRecipesAmount++;
                    waitingRecipeSOList.RemoveAt(i);

                    
                    OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                    switch (GameModeMgr.GetInstance().GetGameMode())
                    {
                        case GameModeMgr.GameMode.Casual:
                            GameMgr.GetInstance().AddRandomGameTime();
                            break;
                        case GameModeMgr.GameMode.Challenging:
                            GameMgr.GetInstance().AddRandomGameTime();
                            int addedScore = waitingRecipeSO.score; // 获取当前菜品的分数
                            totalScore += addedScore; // 累加到总分数
                            OnScoreChanged?.Invoke(this, totalScore); // 触发分数变更事件，传递总分数
                            CheckIfGameWin();

                            break;
                    }
                    
                    
                    OnRecipeSuccess?.Invoke(this, EventArgs.Empty);

                    return;
                }
            }
        }
        //EventCenter.GetInstance().EventTrigger("DeliveryMgr_OnrecipeFailed");
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
        //Debug.Log("无匹配");
        
    }
    private void CheckIfGameWin()
    {
        if (isGameWon || totalScore < targetScore) return;

        isGameWon = true;
        GameMgr.GetInstance().PauseGame();
        OnGameWin?.Invoke(this, EventArgs.Empty);
        Debug.Log("恭喜通关！总分数：" + totalScore + " / 目标分数：" + targetScore);
    }
    public List<RecipeSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount()
    {
        return successfulRecipesAmount;
    }

    
    public int GetTotalScore()
    {
        return totalScore;
    }


    public void ResetScore()
    {
        totalScore = 0;
        isGameWon = false;
        OnScoreChanged?.Invoke(this, totalScore);
    }

    public int GetTargetScore()
    {
        return targetScore;
    }

    public bool IsGameWon() { return isGameWon; }

    
    private void OnDestroy()
    {
        if (GameModeMgr.GetInstance().GetGameMode() == GameModeMgr.GameMode.Challenging)
        {
            RectangleMapGenerator.GetInstance().OnTargetScoreGenerated -= MapGenerator_OnTargetScoreGenerated;
        }
           
        
    }
}
    

