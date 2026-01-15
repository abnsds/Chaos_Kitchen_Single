using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button restartBtn;
    [SerializeField] private Button quitBtn;

    private void Start()
    {
        if (restartBtn != null)
        {
            restartBtn.onClick.AddListener(OnRestartBtnClick);
        }

        if (quitBtn != null)
        {
            quitBtn.onClick.AddListener(OnQuitBtnClick);
        }
        GameMgr.GetInstance().OnStateChanged += GameMgr_OnStateChanged;

        Hide();
    }

    private void OnQuitBtnClick()
    {
        ScenesMgr.GetInstance().LoadScene("MainMenuScene", () =>
        {

        });
    }

    private void OnRestartBtnClick()
    {
      
        // 使用场景管理器重新加载游戏场景（会先显示加载场景）
        ScenesMgr.GetInstance().ReloadCurrentWithLoadingScene(() => {
            Debug.Log("游戏场景重新加载完成");
        });
    }

    private void Update()
    {
    }

    private void GameMgr_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameMgr.GetInstance().IsGameOver())
        {
            Show();
            recipesDeliveredText.text = DeliveryMgr.GetInstance().GetSuccessfulRecipesAmount().ToString();

        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);

    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
